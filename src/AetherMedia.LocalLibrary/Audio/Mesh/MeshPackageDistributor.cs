// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text;
using AetherNet.Content;
using AetherNet.Content.Models;
using AetherNet.Extensibility;
using AetherNet.Forge;
using AetherNet.Forge.Models;

namespace AetherMedia.LocalLibrary.Audio.Mesh;

/// <summary>
/// Generic mesh-distribution layer for plugin / skin / Milkdrop+AVS preset
/// packages. Combines three AetherNet services:
///
/// <list type="number">
///   <item><description><see cref="IForgeService"/> — package-cache index;
///     <c>QueryAsync(packageId)</c> resolves an ID to a content hash + size,
///     <c>CacheAsync</c> records new entries.</description></item>
///   <item><description><see cref="IContentService"/> — chunked content
///     distribution; <c>PublishAsync</c>, <c>AnnounceAsync</c>,
///     <c>RequestChunksAsync</c>, <c>AssembleAsync</c>.</description></item>
///   <item><description><see cref="IAetherNetIncentiveProvider"/> — records
///     relay credit every time the local node serves chunks of a package
///     onward to other peers, so the network's economic accounting accrues
///     to nodes that share popular packages.</description></item>
/// </list>
///
/// <para>
/// Maps to <c>formal/forge-integrity</c> (every cached entry's content hash
/// must verify against the bytes), <c>formal/content-bitmap</c> (chunks
/// converge across honest peers), and <c>formal/forge-eviction</c> (cache
/// pressure releases LRU entries).
/// </para>
/// </summary>
public sealed class MeshPackageDistributor
{
    private readonly IForgeService _forge;
    private readonly IContentService _content;
    private readonly IAetherNetIncentiveProvider _incentives;
    private readonly string _localNodeUhid;
    private readonly TimeSpan _meshTimeout;

    public MeshPackageDistributor(
        IForgeService forge,
        IContentService content,
        IAetherNetIncentiveProvider incentives,
        string localNodeUhid,
        TimeSpan? meshTimeout = null)
    {
        _forge = forge ?? throw new ArgumentNullException(nameof(forge));
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _incentives = incentives ?? throw new ArgumentNullException(nameof(incentives));
        ArgumentException.ThrowIfNullOrEmpty(localNodeUhid);
        _localNodeUhid = localNodeUhid;
        _meshTimeout = meshTimeout ?? TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Publish a package to the mesh. Stores chunks via
    /// <see cref="IContentService"/>, announces the descriptor, and records
    /// the cache entry in <see cref="IForgeService"/>. Returns the resulting
    /// <see cref="ForgeEntry"/> so the caller can show it in a "you
    /// published X" UI.
    /// </summary>
    public async Task<ForgeEntry> PublishAsync(
        string packageId,
        byte[] payload,
        string contentType,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(packageId);
        ArgumentNullException.ThrowIfNull(payload);

        var descriptor = await _content.PublishAsync(packageId, payload, contentType, cancellationToken: ct).ConfigureAwait(false);
        await _content.AnnounceAsync(descriptor, ct).ConfigureAwait(false);
        await _content.BroadcastBitmapAsync(descriptor.RootHash, ct).ConfigureAwait(false);
        return await _forge.CacheAsync(packageId, descriptor.RootHash, payload.LongLength, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetch a package, mesh-first.
    /// <list type="number">
    ///   <item><description>Check <see cref="IForgeService"/> for a local cache hit; if present, just reassemble from <see cref="IContentService"/>.</description></item>
    ///   <item><description>Otherwise broadcast a chunk-bitmap and wait briefly for any peer to announce the descriptor; pull + reassemble.</description></item>
    ///   <item><description>Return null on miss — the caller falls back to its existing online catalogue.</description></item>
    /// </list>
    /// </summary>
    public async Task<byte[]?> TryFetchAsync(string packageId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(packageId);

        var cached = await _forge.FetchAsync(packageId, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            var reassembled = await _content.AssembleAsync(cached.ContentHash, ct).ConfigureAwait(false);
            if (reassembled is not null) return reassembled;
        }

        // Cache miss — try the live mesh.
        var seen = new TaskCompletionSource<ContentDescriptor?>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnAnnounced(object? s, ContentDescriptor d)
        {
            if (string.Equals(d.Name, packageId, StringComparison.Ordinal))
                seen.TrySetResult(d);
        }
        _content.ContentAnnounced += OnAnnounced;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(_meshTimeout);
            timeout.Token.Register(() => seen.TrySetResult(null));

            try { await _content.BroadcastBitmapAsync(packageId, timeout.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) { }

            var descriptor = await seen.Task.ConfigureAwait(false);
            if (descriptor is null) return null;

            var indices = Enumerable.Range(0, descriptor.ChunkCount).ToList();
            await _content.RequestChunksAsync(descriptor.RootHash, indices, peerUhid: null, timeout.Token).ConfigureAwait(false);
            var bytes = await _content.AssembleAsync(descriptor.RootHash, timeout.Token).ConfigureAwait(false);
            if (bytes is null) return null;

            // Record locally + reward the mesh for the relays we benefited from.
            await _forge.CacheAsync(packageId, descriptor.RootHash, bytes.LongLength, timeout.Token).ConfigureAwait(false);
            return bytes;
        }
        catch (OperationCanceledException) { return null; }
        finally { _content.ContentAnnounced -= OnAnnounced; }
    }

    /// <summary>
    /// Record an outbound chunk relay we just served on behalf of a peer.
    /// Wired by the host shell when it sees a <c>ChunkData</c> packet we
    /// emitted. Drives the relay-incentive accounting at the protocol layer.
    /// </summary>
    public Task RecordChunkRelayAsync(AetherNet.Protocol.MeshPacket packet, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(packet);
        return _incentives.RecordRelayAsync(_localNodeUhid, packet, ct);
    }

    /// <summary>Stable identifier for a Winamp classic skin package.</summary>
    public static string SkinPackageId(string skinName) =>
        "skin:" + skinName.ToLowerInvariant();

    /// <summary>Stable identifier for a Milkdrop / AVS preset.</summary>
    public static string PresetPackageId(string family, string presetName) =>
        $"preset:{family.ToLowerInvariant()}:{presetName.ToLowerInvariant()}";

    /// <summary>Stable identifier for a Winamp-style plugin.</summary>
    public static string PluginPackageId(string pluginId, string version) =>
        $"plugin:{pluginId.ToLowerInvariant()}@{version}";

    /// <summary>SHA-256 over the bytes — convenience for integrity checks against the descriptor's root hash.</summary>
    public static string IntegrityHash(byte[] payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return Convert.ToHexString(SHA256.HashData(payload));
    }
}
