// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text;
using AetherNet.Content;
using AetherNet.Content.Models;

namespace AetherMedia.LocalLibrary.Audio.Podcast;

/// <summary>
/// Podcast episode downloader that prefers the mesh over the open internet.
///
/// <para>
/// For each episode we want to fetch:
/// </para>
/// <list type="number">
///   <item><description>Compute a stable content key from <c>(episode.AudioUrl ?? GUID)</c>.</description></item>
///   <item><description>Ask <see cref="IContentService"/> if any peer is already
///     announcing that content. If yes — pull the chunks from the mesh and
///     reassemble locally.</description></item>
///   <item><description>If the mesh has nothing, fall back to the HTTP downloader
///     (the inner <see cref="PodcastDownloader"/>).</description></item>
///   <item><description>Either way, once the bytes are local we
///     <see cref="IContentService.PublishAsync"/> them so other peers can
///     pull from us on their next request — the mesh learns from every
///     download.</description></item>
/// </list>
///
/// <para>
/// Backed by the <c>formal/content-bitmap</c> Petri net: chunks are added
/// to a peer's bitmap as they're verified; downstream peers issue
/// targeted <c>ChunkRequest</c> packets only for missing indices; the
/// model proves the bitmap converges to fully-replicated for every honest
/// peer in finite time.
/// </para>
/// </summary>
public sealed class MeshFirstPodcastDownloader : IDisposable
{
    private readonly PodcastDownloader _inner;
    private readonly IContentService _content;
    private readonly IDirectoryService? _directory;
    private readonly TimeSpan _meshTimeout;

    public MeshFirstPodcastDownloader(
        PodcastDownloader inner,
        IContentService content,
        TimeSpan? meshTimeout = null,
        IDirectoryService? directory = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _directory = directory;
        _meshTimeout = meshTimeout ?? TimeSpan.FromSeconds(8);
    }

    /// <summary>Compute the deterministic content key for an episode.</summary>
    public static string ContentKeyFor(PodcastEpisode episode)
    {
        ArgumentNullException.ThrowIfNull(episode);
        var basis = $"podcast:{episode.Guid}|{episode.AudioUrl}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(basis));
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Download <paramref name="episode"/>, mesh-first. Returns the final
    /// on-disk path. Republishes the bytes to <see cref="IContentService"/>
    /// after either path (mesh or HTTP) so the next caller pulls from the
    /// mesh.
    /// </summary>
    public async Task<string> DownloadAsync(
        PodcastEpisode episode,
        string destDir,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(episode);
        ArgumentException.ThrowIfNullOrEmpty(destDir);
        Directory.CreateDirectory(destDir);

        var key = ContentKeyFor(episode);

        // 1. Try the mesh.
        var meshBytes = await TryMeshFetchAsync(key, ct).ConfigureAwait(false);
        string finalPath;
        byte[] payload;
        if (meshBytes is not null)
        {
            finalPath = Path.Combine(destDir, SafeName(episode));
            await File.WriteAllBytesAsync(finalPath, meshBytes, ct).ConfigureAwait(false);
            payload = meshBytes;
            progress?.Report(1.0);
        }
        else
        {
            // 2. Fall back to HTTP.
            finalPath = await _inner.DownloadAsync(episode, destDir, progress, ct).ConfigureAwait(false);
            payload = await File.ReadAllBytesAsync(finalPath, ct).ConfigureAwait(false);
        }

        // 3. Publish to the mesh so the next caller skips the HTTP path.
        var descriptor = await _content
            .PublishAsync(name: key, data: payload, contentType: episode.MimeType ?? "audio/mpeg", cancellationToken: ct)
            .ConfigureAwait(false);
        await _content.AnnounceAsync(descriptor, ct).ConfigureAwait(false);
        await _content.BroadcastBitmapAsync(descriptor.RootHash, ct).ConfigureAwait(false);
        return finalPath;
    }

    /// <inheritdoc/>
    public void Dispose() => _inner.Dispose();

    private async Task<byte[]?> TryMeshFetchAsync(string contentKey, CancellationToken ct)
    {
        // Preferred path (AetherNet 1.2.0+): IDirectoryService.ResolveAsync.
        if (_directory is not null)
        {
            try
            {
                var descriptor = await _directory.ResolveAsync(contentKey, _meshTimeout, ct).ConfigureAwait(false);
                if (descriptor is null) return null;
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(_meshTimeout);
                var indices = Enumerable.Range(0, descriptor.ChunkCount).ToList();
                await _content.RequestChunksAsync(descriptor.RootHash, indices, peerUhid: null, timeout.Token)
                              .ConfigureAwait(false);
                return await _content.AssembleAsync(descriptor.RootHash, timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { return null; }
        }

        // Legacy fallback: hash-as-name + ContentAnnounced-listen.
        var seen = new TaskCompletionSource<ContentDescriptor?>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnAnnounced(object? sender, ContentDescriptor descriptor)
        {
            if (string.Equals(descriptor.Name, contentKey, StringComparison.Ordinal))
                seen.TrySetResult(descriptor);
        }
        _content.ContentAnnounced += OnAnnounced;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(_meshTimeout);
            using var reg = timeout.Token.Register(() => seen.TrySetResult(null));

            // Issue an empty request so peers' BitmapBroadcasts are forwarded to us.
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
        finally
        {
            _content.ContentAnnounced -= OnAnnounced;
        }
    }

    private static string SafeName(PodcastEpisode episode)
    {
        var ext = Path.GetExtension(episode.AudioUrl.AbsolutePath);
        if (string.IsNullOrEmpty(ext)) ext = ".mp3";
        var basis = $"{episode.PublishedAtUtc:yyyy-MM-dd} - {episode.Title}";
        Span<char> buf = stackalloc char[basis.Length];
        for (var i = 0; i < basis.Length; i++)
            buf[i] = "*?<>|:\"\\/".Contains(basis[i]) ? '_' : basis[i];
        return new string(buf) + ext;
    }
}
