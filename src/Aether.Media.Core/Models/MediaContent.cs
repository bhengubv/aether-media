namespace Aether.Media.Core.Models;

/// <summary>
/// Immutable description of a single piece of media (video or audio) stored
/// on the Aether network.  The primary key is <see cref="ContentHash"/> —
/// a SHA-256 hex digest of the raw encoded bytes.
/// </summary>
public sealed record MediaContent(
    string ContentHash,
    string Title,
    long DurationMs,
    string Codec,
    string ContentType,
    string CreatorUhid,
    long SizeBytes,
    DateTime CreatedAt,
    string? ThumbnailHash,
    IReadOnlyList<string> Tags)
{
    /// <summary>
    /// Human-readable duration formatted as <c>H:MM:SS</c> (hours omitted when
    /// less than 60 minutes) or <c>"Live"</c> when <see cref="DurationMs"/> is 0.
    /// </summary>
    public string FormattedDuration
    {
        get
        {
            if (DurationMs <= 0)
                return "Live";

            var totalSeconds = DurationMs / 1000L;
            var hours   = totalSeconds / 3600;
            var minutes = (totalSeconds % 3600) / 60;
            var seconds = totalSeconds % 60;

            return hours > 0
                ? $"{hours}:{minutes:D2}:{seconds:D2}"
                : $"{minutes}:{seconds:D2}";
        }
    }

    /// <summary>
    /// <c>true</c> when the MIME type indicates a video stream
    /// (i.e. <see cref="ContentType"/> starts with <c>"video/"</c>).
    /// </summary>
    public bool IsVideo => ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// <c>true</c> when the MIME type indicates a pure audio stream
    /// (i.e. <see cref="ContentType"/> starts with <c>"audio/"</c>).
    /// </summary>
    public bool IsAudio => ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
}
