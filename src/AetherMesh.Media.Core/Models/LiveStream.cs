using System.Text.Json.Serialization;

namespace AetherMesh.Media.Core.Models;

/// <summary>
/// Represents an active live broadcast being relayed through the Aether mesh.
/// </summary>
public sealed record LiveStream(
    [property: JsonPropertyName("stream_id")]           Guid StreamId,
    [property: JsonPropertyName("title")]               string Title,
    [property: JsonPropertyName("creator_uhid")]        string CreatorUhid,
    [property: JsonPropertyName("codec")]               string Codec,
    [property: JsonPropertyName("segment_duration_ms")] int SegmentDurationMs,
    [property: JsonPropertyName("started_at_ms")]       long StartedAtMs,
    [property: JsonPropertyName("viewer_count")]        int ViewerCount,
    [property: JsonPropertyName("is_active")]           bool IsActive,
    [property: JsonPropertyName("tags")]                IReadOnlyList<string> Tags)
{
    /// <summary>
    /// Wall-clock milliseconds elapsed since the broadcast started (UTC).
    /// Always >= 0; clamped to 0 if the clock is somehow behind
    /// <see cref="StartedAtMs"/>.
    /// </summary>
    [JsonIgnore]
    public long ElapsedMs
    {
        get
        {
            var elapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - StartedAtMs;
            return elapsed < 0 ? 0L : elapsed;
        }
    }

    /// <summary>
    /// Human-readable elapsed time formatted as <c>H:MM:SS</c> (hours omitted
    /// when less than 60 minutes).
    /// </summary>
    [JsonIgnore]
    public string ElapsedFormatted
    {
        get
        {
            var totalSeconds = ElapsedMs / 1000L;
            var hours   = totalSeconds / 3600;
            var minutes = (totalSeconds % 3600) / 60;
            var seconds = totalSeconds % 60;

            return hours > 0
                ? $"{hours}:{minutes:D2}:{seconds:D2}"
                : $"{minutes}:{seconds:D2}";
        }
    }
}
