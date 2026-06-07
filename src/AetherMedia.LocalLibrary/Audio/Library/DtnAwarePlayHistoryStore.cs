// SPDX-License-Identifier: MIT

using System.Text.Json;
using AetherMedia.LocalLibrary.Audio.Mesh;
using AetherNet.Dtn;
using AetherNet.Models;

namespace AetherMedia.LocalLibrary.Audio.Library;

/// <summary>
/// Mesh-syncing <see cref="IPlayHistoryStore"/>. Wraps any local store, and
/// on every <see cref="RecordAsync"/> also opens a DTN bundle to the user's
/// own UHID — so play counts and "most played" rankings stay coherent
/// across every device the user owns.
///
/// <para>
/// Backed by the <c>formal/multi-device-sync</c> Petri net. Idempotent on
/// receipt — applying the same <see cref="PlayEvent"/> twice produces the
/// same final stats because <see cref="IPlayHistoryStore.RecordAsync"/> is
/// already additive.
/// </para>
/// </summary>
public sealed class DtnAwarePlayHistoryStore : IPlayHistoryStore
{
    private readonly IPlayHistoryStore _inner;
    private readonly IDtnService _dtn;
    private readonly string _selfUhid;
    private readonly byte[] _aesKey;

    public DtnAwarePlayHistoryStore(IPlayHistoryStore inner, IDtnService dtn, string selfUhid, byte[] payloadEncryptionKey)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _dtn = dtn ?? throw new ArgumentNullException(nameof(dtn));
        ArgumentException.ThrowIfNullOrEmpty(selfUhid);
        _selfUhid = selfUhid;
        _aesKey = payloadEncryptionKey ?? throw new ArgumentNullException(nameof(payloadEncryptionKey));
    }

    /// <inheritdoc/>
    public async Task RecordAsync(PlayEvent ev, CancellationToken ct = default)
    {
        await _inner.RecordAsync(ev, ct).ConfigureAwait(false);
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(ev);
        var encrypted = AesGcmEnvelope.Encrypt(_aesKey, plaintext);
        await _dtn.CreateBundleAsync(_selfUhid, encrypted, BundlePriority.Low, cancellationToken: ct)
                  .ConfigureAwait(false);
    }

    /// <summary>Apply an inbound play-history sync bundle from another device.</summary>
    public async Task ApplyIncomingBundleAsync(byte[] encryptedPayload, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(encryptedPayload);
        var plaintext = AesGcmEnvelope.Decrypt(_aesKey, encryptedPayload);
        var ev = JsonSerializer.Deserialize<PlayEvent>(plaintext)
            ?? throw new FormatException("Play event sync payload deserialised to null.");
        await _inner.RecordAsync(ev, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<PlayStatistics> GetAsync(string filePath, CancellationToken ct = default) =>
        _inner.GetAsync(filePath, ct);

    /// <inheritdoc/>
    public Task<IReadOnlyList<PlayStatistics>> MostPlayedAsync(int limit, CancellationToken ct = default) =>
        _inner.MostPlayedAsync(limit, ct);

    /// <inheritdoc/>
    public Task<IReadOnlyList<PlayStatistics>> LeastPlayedAsync(int limit, CancellationToken ct = default) =>
        _inner.LeastPlayedAsync(limit, ct);

    /// <inheritdoc/>
    public Task<IReadOnlyList<PlayStatistics>> RecentlyPlayedAsync(int limit, CancellationToken ct = default) =>
        _inner.RecentlyPlayedAsync(limit, ct);
}
