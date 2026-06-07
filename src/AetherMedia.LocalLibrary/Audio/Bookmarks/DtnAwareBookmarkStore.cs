// SPDX-License-Identifier: MIT

using System.Text.Json;
using AetherMedia.LocalLibrary.Audio.Mesh;
using AetherNet.Dtn;
using AetherNet.Models;

namespace AetherMedia.LocalLibrary.Audio.Bookmarks;

/// <summary>
/// Mesh-syncing <see cref="IBookmarkStore"/>. Wraps any local store, and on
/// every <see cref="AddAsync"/> / <see cref="RemoveAsync"/> / <see cref="ClearAsync"/>
/// also opens a DTN bundle addressed to the user's own UHID — so when one
/// of the user's other devices reconnects, the bookmark change appears
/// there too.
///
/// <para>
/// Maps to the <c>formal/multi-device-sync</c> Petri net: every state
/// mutation produces a DTN bundle; the bundle eventually reaches Delivered
/// or Expired; receiving devices apply the mutation idempotently. The
/// invariant predicate ships in <c>MultiDeviceSyncInvariant</c>.
/// </para>
/// </summary>
public sealed class DtnAwareBookmarkStore : IBookmarkStore
{
    private readonly IBookmarkStore _inner;
    private readonly IDtnService _dtn;
    private readonly string _selfUhid;
    private readonly byte[] _aesKey;

    public DtnAwareBookmarkStore(IBookmarkStore inner, IDtnService dtn, string selfUhid, byte[] payloadEncryptionKey)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _dtn = dtn ?? throw new ArgumentNullException(nameof(dtn));
        ArgumentException.ThrowIfNullOrEmpty(selfUhid);
        _selfUhid = selfUhid;
        _aesKey = payloadEncryptionKey ?? throw new ArgumentNullException(nameof(payloadEncryptionKey));
    }

    /// <inheritdoc/>
    public async Task AddAsync(Bookmark bookmark, CancellationToken ct = default)
    {
        await _inner.AddAsync(bookmark, ct).ConfigureAwait(false);
        await EmitMutationAsync(BookmarkMutation.Added, bookmark, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Bookmark>> ListAsync(string? filePath = null, CancellationToken ct = default) =>
        _inner.ListAsync(filePath, ct);

    /// <inheritdoc/>
    public Task<Bookmark?> ResumeFor(string filePath, CancellationToken ct = default) =>
        _inner.ResumeFor(filePath, ct);

    /// <inheritdoc/>
    public async Task RemoveAsync(Bookmark bookmark, CancellationToken ct = default)
    {
        await _inner.RemoveAsync(bookmark, ct).ConfigureAwait(false);
        await EmitMutationAsync(BookmarkMutation.Removed, bookmark, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ClearAsync(CancellationToken ct = default)
    {
        await _inner.ClearAsync(ct).ConfigureAwait(false);
        await EmitMutationAsync(BookmarkMutation.Cleared, bookmark: null, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Decode + apply an inbound bookmark-sync bundle that arrived from
    /// another of the user's devices. The host shell wires this to its DTN
    /// bundle-received pump.
    /// </summary>
    public async Task ApplyIncomingBundleAsync(byte[] encryptedPayload, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(encryptedPayload);
        var plaintext = AesGcmEnvelope.Decrypt(_aesKey, encryptedPayload);
        var payload = JsonSerializer.Deserialize<BookmarkSyncPayload>(plaintext)
            ?? throw new FormatException("Bookmark sync payload deserialised to null.");

        switch (payload.Op)
        {
            case BookmarkMutation.Added when payload.Bookmark is not null:
                await _inner.AddAsync(payload.Bookmark, ct).ConfigureAwait(false);
                break;
            case BookmarkMutation.Removed when payload.Bookmark is not null:
                await _inner.RemoveAsync(payload.Bookmark, ct).ConfigureAwait(false);
                break;
            case BookmarkMutation.Cleared:
                await _inner.ClearAsync(ct).ConfigureAwait(false);
                break;
        }
    }

    private async Task EmitMutationAsync(BookmarkMutation op, Bookmark? bookmark, CancellationToken ct)
    {
        var payload = new BookmarkSyncPayload(op, bookmark);
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(payload);
        var encrypted = AesGcmEnvelope.Encrypt(_aesKey, plaintext);
        await _dtn.CreateBundleAsync(_selfUhid, encrypted, BundlePriority.Normal, cancellationToken: ct)
                  .ConfigureAwait(false);
    }

    /// <summary>The kind of state change one bundle carries.</summary>
    public enum BookmarkMutation { Added, Removed, Cleared }

    /// <summary>Wire payload for a bookmark mutation.</summary>
    public sealed record BookmarkSyncPayload(BookmarkMutation Op, Bookmark? Bookmark);
}
