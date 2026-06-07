// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text;
using AetherNet.Content;
using AetherNet.Content.Models;

namespace AetherMedia.LocalLibrary.Audio.Library;

/// <summary>
/// Cover-art fetcher that asks the mesh first. Maintains the same
/// <see cref="ICoverArtFetcher"/> contract as <see cref="MusicBrainzCoverArtFetcher"/>,
/// so it can stand in transparently — but every call does:
/// <list type="number">
///   <item><description>Derive a deterministic content key from <c>(artist, album, track)</c>.</description></item>
///   <item><description>Ask <see cref="IContentService"/> if any peer is announcing that key. If yes — pull + assemble.</description></item>
///   <item><description>Otherwise fall back to the inner HTTP fetcher.</description></item>
///   <item><description>Publish the resulting bytes so the next caller pulls from the mesh.</description></item>
/// </list>
///
/// <para>
/// Backed by the <c>formal/content-bitmap</c> Petri net (same as podcast
/// episode distribution). The cache-warmup pattern converges across the
/// mesh — every node that has seen the artwork once seeds it for everyone
/// else.
/// </para>
/// </summary>
public sealed class MeshFirstCoverArtFetcher : ICoverArtFetcher, IDisposable
{
    private readonly ICoverArtFetcher _inner;
    private readonly IContentService _content;
    private readonly TimeSpan _meshTimeout;

    public MeshFirstCoverArtFetcher(ICoverArtFetcher inner, IContentService content, TimeSpan? meshTimeout = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _meshTimeout = meshTimeout ?? TimeSpan.FromSeconds(4);
    }

    /// <summary>The content key the mesh will index this cover art under.</summary>
    public static string ContentKeyFor(string artist, string album, string? track)
    {
        var basis = $"coverart:{artist.ToLowerInvariant()}|{album.ToLowerInvariant()}|{(track ?? "").ToLowerInvariant()}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(basis)));
    }

    /// <inheritdoc/>
    public async Task<byte[]?> FetchAsync(string artist, string album, string? track = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(album)) return null;
        var key = ContentKeyFor(artist, album, track);

        var fromMesh = await TryMeshAsync(key, ct).ConfigureAwait(false);
        if (fromMesh is not null) return fromMesh;

        var fromHttp = await _inner.FetchAsync(artist, album, track, ct).ConfigureAwait(false);
        if (fromHttp is null || fromHttp.Length == 0) return null;

        // Republish so the next request pulls from the mesh.
        var descriptor = await _content
            .PublishAsync(key, fromHttp, contentType: "image/jpeg", cancellationToken: ct)
            .ConfigureAwait(false);
        await _content.AnnounceAsync(descriptor, ct).ConfigureAwait(false);
        await _content.BroadcastBitmapAsync(descriptor.RootHash, ct).ConfigureAwait(false);
        return fromHttp;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_inner is IDisposable d) d.Dispose();
    }

    private async Task<byte[]?> TryMeshAsync(string contentKey, CancellationToken ct)
    {
        var seen = new TaskCompletionSource<ContentDescriptor?>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnAnnounced(object? s, ContentDescriptor d)
        {
            if (string.Equals(d.Name, contentKey, StringComparison.Ordinal))
                seen.TrySetResult(d);
        }
        _content.ContentAnnounced += OnAnnounced;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(_meshTimeout);
            timeout.Token.Register(() => seen.TrySetResult(null));

            try { await _content.BroadcastBitmapAsync(contentKey, timeout.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) { /* timed out — fine */ }

            var descriptor = await seen.Task.ConfigureAwait(false);
            if (descriptor is null) return null;
            var indices = Enumerable.Range(0, descriptor.ChunkCount).ToList();
            await _content.RequestChunksAsync(descriptor.RootHash, indices, peerUhid: null, timeout.Token)
                          .ConfigureAwait(false);
            return await _content.AssembleAsync(descriptor.RootHash, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { return null; }
        finally { _content.ContentAnnounced -= OnAnnounced; }
    }
}
