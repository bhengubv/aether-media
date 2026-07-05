// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;

namespace AetherMedia.Ingest.Hls;

/// <summary>
/// HLS source adapter: resolves a master playlist to a media playlist, then pulls each segment over
/// HTTP and yields it verbatim as a passthrough <see cref="MediaSegment"/>. Live playlists are
/// re-polled for new segments; VOD (with <c>#EXT-X-ENDLIST</c>) ends after one pass. No transcode —
/// the encoded segment bytes are carried unchanged.
/// </summary>
public sealed class HlsSourceAdapter : ISourceAdapter
{
    private readonly HttpClient _http;

    public HlsSourceAdapter(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    /// <inheritdoc />
    public bool CanHandle(SourceDescriptor source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Kind == SourceKind.Hls
            || source.Uri.AbsolutePath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MediaSegment> ReadAsync(
        SourceDescriptor source, [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var mediaUri = await ResolveMediaPlaylistAsync(source, ct).ConfigureAwait(false);
        var seen = new HashSet<long>();
        uint outSequence = 0;
        long presentationMs = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var content = await FetchTextAsync(mediaUri, source, ct).ConfigureAwait(false);
            var playlist = HlsPlaylistParser.Parse(content, mediaUri);

            foreach (var segmentRef in playlist.Segments)
            {
                if (!seen.Add(segmentRef.MediaSequence))
                {
                    continue;
                }

                var payload = await FetchBytesAsync(segmentRef.Uri, source, ct).ConfigureAwait(false);
                var durationMs = (long)Math.Round(segmentRef.DurationSeconds * 1000.0);

                yield return new MediaSegment
                {
                    Track = TrackKind.Video,
                    Codec = "h264",
                    Container = ContainerFor(segmentRef.Uri),
                    RungBitrateKbps = 0,
                    PresentationTimeMs = presentationMs,
                    DurationMs = durationMs,
                    Sequence = outSequence,
                    IsKeyframe = true,
                    Payload = payload,
                };

                outSequence++;
                presentationMs += durationMs;
            }

            if (playlist.HasEndList)
            {
                yield break;
            }

            var wait = TimeSpan.FromSeconds(Math.Max(1.0, playlist.TargetDuration / 2.0));
            var cancelled = false;
            try
            {
                await Task.Delay(wait, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            if (cancelled)
            {
                yield break;
            }
        }
    }

    private async Task<Uri> ResolveMediaPlaylistAsync(SourceDescriptor source, CancellationToken ct)
    {
        var content = await FetchTextAsync(source.Uri, source, ct).ConfigureAwait(false);
        if (!HlsPlaylistParser.IsMaster(content))
        {
            return source.Uri;
        }

        var variants = HlsPlaylistParser.ParseMasterVariants(content, source.Uri);
        if (variants.Count == 0)
        {
            throw new FormatException("HLS master playlist declares no variants.");
        }

        return variants[0];
    }

    private async Task<string> FetchTextAsync(Uri uri, SourceDescriptor source, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        ApplyHeaders(request, source);
        using var response = await _http
            .SendAsync(request, HttpCompletionOption.ResponseContentRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }

    private async Task<ReadOnlyMemory<byte>> FetchBytesAsync(
        Uri uri, SourceDescriptor source, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        ApplyHeaders(request, source);
        using var response = await _http
            .SendAsync(request, HttpCompletionOption.ResponseContentRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
    }

    private static void ApplyHeaders(HttpRequestMessage request, SourceDescriptor source)
    {
        foreach (var header in source.Headers)
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    private static string ContainerFor(Uri uri)
    {
        var path = uri.AbsolutePath;
        if (path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase))
        {
            return "ts";
        }

        if (path.EndsWith(".m4s", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
        {
            return "mp4";
        }

        return "";
    }
}
