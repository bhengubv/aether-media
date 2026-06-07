// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using AetherNet.Dtn;
using AetherNet.Models;

namespace AetherMedia.LocalLibrary.Audio.Scrobble;

/// <summary>
/// Mesh-aware scrobbler. Wraps an <see cref="IScrobbler"/> (the real online
/// implementation) and falls back to an <see cref="IDtnService"/> bundle
/// whenever the inner scrobble fails. Replaces the in-memory queue in
/// <see cref="LastFmScrobbler"/> with a real DTN custody flow:
/// <list type="number">
///   <item><description>Caller's local network is offline — inner scrobble throws.</description></item>
///   <item><description>We serialise the <see cref="ScrobbleEvent"/> into a <see cref="ScrobblePayload"/>, AES-GCM-encrypt with the user's own per-device key, and call <see cref="IDtnService.CreateBundleAsync"/> with the user's UHID as recipient.</description></item>
///   <item><description>The protocol stack carries the bundle opportunistically — over Wi-Fi, BLE, NearLink, or HTTP relay — to any of the user's other devices that ARE online.</description></item>
///   <item><description>That receiving device decrypts and posts to Last.fm.</description></item>
/// </list>
///
/// <para>
/// The Petri net <c>formal/dtn-custody</c> proves every bundle either reaches
/// <see cref="BundleStatus.Delivered"/> or expires — it never gets stuck in
/// <see cref="BundleStatus.Pending"/>. <see cref="PendingDtnBundles"/> + the
/// xUnit invariants in <c>DtnCustodyInvariants</c> verify the same property
/// at runtime.
/// </para>
/// </summary>
public sealed class DtnAwareScrobbler : IScrobbler
{
    private readonly IScrobbler _inner;
    private readonly IDtnService _dtn;
    private readonly string _selfUhid;
    private readonly byte[] _aesKey; // 32-byte key derived from user identity

    /// <summary>
    /// Construct a DTN-aware scrobbler.
    /// </summary>
    /// <param name="inner">The real HTTP-based scrobbler used when the network is up.</param>
    /// <param name="dtn">DTN service used to relay scrobbles when the inner call fails.</param>
    /// <param name="selfUhid">The user's mesh UHID — the bundle recipient for cross-device delivery.</param>
    /// <param name="payloadEncryptionKey">32-byte symmetric key for AES-GCM payload encryption.</param>
    public DtnAwareScrobbler(IScrobbler inner, IDtnService dtn, string selfUhid, byte[] payloadEncryptionKey)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _dtn = dtn ?? throw new ArgumentNullException(nameof(dtn));
        ArgumentException.ThrowIfNullOrEmpty(selfUhid);
        ArgumentNullException.ThrowIfNull(payloadEncryptionKey);
        if (payloadEncryptionKey.Length != 32)
            throw new ArgumentException("Encryption key must be 32 bytes (AES-256).", nameof(payloadEncryptionKey));
        _selfUhid = selfUhid;
        _aesKey = payloadEncryptionKey;
    }

    /// <inheritdoc/>
    public bool IsAuthenticated => _inner.IsAuthenticated;

    /// <summary>Active DTN bundles still in custody (not yet delivered or expired).</summary>
    public async Task<int> PendingDtnBundlesAsync(CancellationToken ct = default)
    {
        var bundles = await _dtn.GetActiveBundlesAsync(ct).ConfigureAwait(false);
        return bundles.Count(b => b.RecipientUhid == _selfUhid &&
                                  b.Status is BundleStatus.Pending or BundleStatus.InCustody);
    }

    /// <inheritdoc/>
    public async Task UpdateNowPlayingAsync(ScrobbleEvent ev, CancellationToken ct = default)
    {
        // Now-playing isn't worth buffering — drop on failure (matches Last.fm guidance).
        try { await _inner.UpdateNowPlayingAsync(ev, ct).ConfigureAwait(false); }
        catch (HttpRequestException) { /* drop */ }
    }

    /// <inheritdoc/>
    public async Task ScrobbleAsync(ScrobbleEvent ev, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ev);
        try
        {
            await _inner.ScrobbleAsync(ev, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            // Fall back to DTN custody to one of the user's other devices.
            var encrypted = EncryptPayload(ScrobblePayload.FromEvent(ev));
            await _dtn.CreateBundleAsync(
                recipientUhid: _selfUhid,
                encryptedPayload: encrypted,
                priority: BundlePriority.Normal,
                cancellationToken: ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task FlushAsync(CancellationToken ct = default)
    {
        // Try to drain any inner-buffered scrobbles first.
        await _inner.FlushAsync(ct).ConfigureAwait(false);

        // The DTN service runs its own delivery scan loop; trigger a single
        // pass to nudge our queued bundles through. Expiry sweep happens on
        // its own cadence — we don't drive that from the scrobbler.
        await _dtn.RunDeliveryScanAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Decrypt + re-deliver an incoming scrobble bundle. Called by the host
    /// shell when it sees an inbound DTN bundle whose recipient is us.
    /// Returns the decoded <see cref="ScrobbleEvent"/> so the caller can
    /// hand it back into the inner scrobbler when the local network is
    /// available.
    /// </summary>
    public ScrobbleEvent DecodeIncomingBundle(byte[] encryptedPayload)
    {
        ArgumentNullException.ThrowIfNull(encryptedPayload);
        var plaintext = DecryptPayload(encryptedPayload);
        return ScrobblePayload.FromBytes(plaintext).ToEvent();
    }

    private byte[] EncryptPayload(ScrobblePayload payload)
    {
        var plaintext = payload.ToBytes();
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
        var cipher = new byte[plaintext.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];
        using var gcm = new AesGcm(_aesKey, tag.Length);
        gcm.Encrypt(nonce, plaintext, cipher, tag);

        // Wire layout: [nonce(12)][tag(16)][cipher(N)]
        var bundle = new byte[nonce.Length + tag.Length + cipher.Length];
        Buffer.BlockCopy(nonce,  0, bundle, 0,                       nonce.Length);
        Buffer.BlockCopy(tag,    0, bundle, nonce.Length,            tag.Length);
        Buffer.BlockCopy(cipher, 0, bundle, nonce.Length + tag.Length, cipher.Length);
        return bundle;
    }

    private byte[] DecryptPayload(byte[] bundle)
    {
        var nonceLen = AesGcm.NonceByteSizes.MaxSize;
        var tagLen = AesGcm.TagByteSizes.MaxSize;
        if (bundle.Length < nonceLen + tagLen)
            throw new FormatException("Encrypted scrobble payload is too short.");
        var nonce = bundle.AsSpan(0, nonceLen);
        var tag = bundle.AsSpan(nonceLen, tagLen);
        var cipher = bundle.AsSpan(nonceLen + tagLen);
        var plaintext = new byte[cipher.Length];
        using var gcm = new AesGcm(_aesKey, tagLen);
        gcm.Decrypt(nonce, cipher, tag, plaintext);
        return plaintext;
    }
}
