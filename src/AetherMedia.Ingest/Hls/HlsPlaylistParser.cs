// SPDX-License-Identifier: MIT

using System.Globalization;

namespace AetherMedia.Ingest.Hls;

/// <summary>A single segment reference from an HLS media playlist.</summary>
internal sealed record HlsSegmentRef(Uri Uri, double DurationSeconds, long MediaSequence);

/// <summary>A parsed HLS media playlist.</summary>
internal sealed record HlsMediaPlaylist(
    IReadOnlyList<HlsSegmentRef> Segments,
    long MediaSequence,
    double TargetDuration,
    bool HasEndList);

/// <summary>
/// Minimal, real HLS playlist parser: master detection, variant listing, and media-playlist parsing.
/// Enough to drive passthrough ingest of standard live and VOD HLS.
/// </summary>
internal static class HlsPlaylistParser
{
    /// <summary>True when the playlist is a master (references variant streams).</summary>
    public static bool IsMaster(string content) =>
        content.Contains("#EXT-X-STREAM-INF", StringComparison.Ordinal);

    /// <summary>Parse the variant URIs from a master playlist, in listed order.</summary>
    public static IReadOnlyList<Uri> ParseMasterVariants(string content, Uri baseUri)
    {
        var variants = new List<Uri>();
        var expectUri = false;

        foreach (var raw in content.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("#EXT-X-STREAM-INF", StringComparison.Ordinal))
            {
                expectUri = true;
            }
            else if (expectUri && line[0] != '#')
            {
                variants.Add(new Uri(baseUri, line));
                expectUri = false;
            }
        }

        return variants;
    }

    /// <summary>Parse a media playlist (segment list). Throws <see cref="FormatException"/> if not HLS.</summary>
    public static HlsMediaPlaylist Parse(string content, Uri baseUri)
    {
        var segments = new List<HlsSegmentRef>();
        long mediaSequence = 0;
        double targetDuration = 0;
        var hasEndList = false;
        double pendingDuration = -1;
        long sequence = 0;
        var started = false;

        foreach (var raw in content.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (!started)
            {
                if (!line.StartsWith("#EXTM3U", StringComparison.Ordinal))
                {
                    throw new FormatException("Not an HLS playlist (missing #EXTM3U).");
                }

                started = true;
                continue;
            }

            if (line.StartsWith("#EXT-X-MEDIA-SEQUENCE:", StringComparison.Ordinal))
            {
                mediaSequence = long.Parse(
                    line.AsSpan("#EXT-X-MEDIA-SEQUENCE:".Length), CultureInfo.InvariantCulture);
                sequence = mediaSequence;
            }
            else if (line.StartsWith("#EXT-X-TARGETDURATION:", StringComparison.Ordinal))
            {
                targetDuration = double.Parse(
                    line.AsSpan("#EXT-X-TARGETDURATION:".Length), CultureInfo.InvariantCulture);
            }
            else if (line.StartsWith("#EXTINF:", StringComparison.Ordinal))
            {
                var value = line.AsSpan("#EXTINF:".Length);
                var comma = value.IndexOf(',');
                var durationSpan = comma >= 0 ? value[..comma] : value;
                pendingDuration = double.Parse(durationSpan, CultureInfo.InvariantCulture);
            }
            else if (line.StartsWith("#EXT-X-ENDLIST", StringComparison.Ordinal))
            {
                hasEndList = true;
            }
            else if (line[0] != '#')
            {
                segments.Add(new HlsSegmentRef(
                    new Uri(baseUri, line),
                    pendingDuration < 0 ? 0 : pendingDuration,
                    sequence));
                sequence++;
                pendingDuration = -1;
            }
        }

        return new HlsMediaPlaylist(segments, mediaSequence, targetDuration, hasEndList);
    }
}
