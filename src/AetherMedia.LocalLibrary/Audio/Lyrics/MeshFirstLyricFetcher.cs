// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text;
using AetherNet.Content;
using AetherNet.Content.Models;

namespace AetherMedia.LocalLibrary.Audio.Lyrics;

/// <summary>
/// Mesh-first lyric fetcher. Same shape as <see cref="MeshFirstCoverArtFetcher"/>:
/// derive a key from (artist + track + album), look it up via
/// <see cref="IContentService"/>, fall back to the inner HTTP fetcher,
/// republish to the mesh. Stays on the <see cref="ILyricFetcher"/>
/// contract so it drops into existing callers transparently.
/// </summary>
public sealed class MeshFirstLyricFetcher : ILyricFetcher, IDisposable
{
    private readonly ILyricFetcher _inner;
    private readonly IContentService _content;
    private readonly IDirectoryService? _directory;
    private readonly LrcParser _parser = new();
    private readonly TimeSpan _meshTimeout;

    public MeshFirstLyricFetcher(
        ILyricFetcher inner,
        IContentService content,
        TimeSpan? meshTimeout = null,
        IDirectoryService? directory = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _directory = directory;
        _meshTimeout = meshTimeout ?? TimeSpan.FromSeconds(4);
    }

    public static string ContentKeyFor(string artist, string trackTitle, string? album)
    {
        var basis = $"lyrics:{artist.ToLowerInvariant()}|{trackTitle.ToLowerInvariant()}|{(album ?? "").ToLowerInvariant()}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(basis)));
    }

    /// <inheritdoc/>
    public async Task<LrcFile?> FetchAsync(string artist, string trackTitle, string? album = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(artist) || string.IsNullOrWhiteSpace(trackTitle)) return null;
        var key = ContentKeyFor(artist, trackTitle, album);

        var meshBytes = await TryMeshAsync(key, ct).ConfigureAwait(false);
        if (meshBytes is not null)
            return _parser.Parse(Encoding.UTF8.GetString(meshBytes));

        var fromHttp = await _inner.FetchAsync(artist, trackTitle, album, ct).ConfigureAwait(false);
        if (fromHttp is null || fromHttp.Lines.Count == 0) return fromHttp;

        // Republish: re-emit synced LRC if present, otherwise the plain text.
        var lrcText = string.Join('\n', fromHttp.Lines.Select(l =>
            $"[{l.Offset.Minutes:00}:{l.Offset.Seconds:00}.{l.Offset.Milliseconds:000}]{l.Text}"));
        var payload = Encoding.UTF8.GetBytes(lrcText);
        var descriptor = await _content
            .PublishAsync(key, payload, contentType: "text/plain; charset=utf-8", cancellationToken: ct)
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
                await _content.RequestChunksAsync(descriptor.RootHash, indices, null, timeout.Token).ConfigureAwait(false);
                return await _content.AssembleAsync(descriptor.RootHash, timeout.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { return null; }
        }

        // Legacy fallback: hash-as-name + ContentAnnounced-listen.
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
            catch (OperationCanceledException) { }

            var descriptor = await seen.Task.ConfigureAwait(false);
            if (descriptor is null) return null;
            var indices = Enumerable.Range(0, descriptor.ChunkCount).ToList();
            await _content.RequestChunksAsync(descriptor.RootHash, indices, null, timeout.Token).ConfigureAwait(false);
            return await _content.AssembleAsync(descriptor.RootHash, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { return null; }
        finally { _content.ContentAnnounced -= OnAnnounced; }
    }
}
