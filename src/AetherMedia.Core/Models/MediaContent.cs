using System.Text.Json.Serialization;

namespace AetherMedia.Core.Models;

/// <summary>
/// Immutable description of a single piece of media (video or audio) stored
/// on the Aether network.  The primary key is <see cref="ContentHash"/> —
/// a SHA-256 hex digest of the raw encoded bytes.
/// </summary>
public sealed record MediaContent(
    [property: JsonPropertyName("content_hash")]  string ContentHash,
    [property: JsonPropertyName("title")]         string Title,
    [property: JsonPropertyName("duration_ms")]   long DurationMs,
    [property: JsonPropertyName("codec")]         string Codec,
    [property: JsonPropertyName("content_type")]  string ContentType,
    [property: JsonPropertyName("creator_uhid")]  string CreatorUhid,
    [property: JsonPropertyName("size_bytes")]    long SizeBytes,
    [property: JsonPropertyName("created_at_ms")] long CreatedAtMs,
    [property: JsonPropertyName("thumbnail_hash")] string? ThumbnailHash,
    [property: JsonPropertyName("tags")]          IReadOnlyList<string> Tags)
{
    /// <summary>
    /// Human-readable duration formatted as <c>H:MM:SS</c> (hours omitted when
    /// less than 60 minutes) or <c>"Live"</c> when <see cref="DurationMs"/> is 0.
    /// </summary>
    [JsonIgnore]
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
    [JsonIgnore]
    public bool IsVideo => ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// <c>true</c> when the MIME type indicates a pure audio stream
    /// (i.e. <see cref="ContentType"/> starts with <c>"audio/"</c>).
    /// </summary>
    [JsonIgnore]
    public bool IsAudio => ContentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase);
}
