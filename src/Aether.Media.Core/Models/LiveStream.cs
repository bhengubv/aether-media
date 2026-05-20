namespace Aether.Media.Core.Models;

/// <summary>
/// Represents an active live broadcast being relayed through the Aether mesh.
/// </summary>
public sealed record LiveStream(
    Guid StreamId,
    string Title,
    string CreatorUhid,
    string Codec,
    int SegmentDurationMs,
    DateTime StartedAt,
    int ViewerCount,
    bool IsActive,
    IReadOnlyList<string> Tags)
{
    /// <summary>
    /// Wall-clock milliseconds elapsed since the broadcast started (UTC).
    /// Always >= 0; clamped to 0 if the clock is somehow behind
    /// <see cref="StartedAt"/>.
    /// </summary>
    public long ElapsedMs
    {
        get
        {
            var elapsed = (long)(DateTime.UtcNow - StartedAt.ToUniversalTime()).TotalMilliseconds;
            return elapsed < 0 ? 0L : elapsed;
        }
    }

    /// <summary>
    /// Human-readable elapsed time formatted as <c>H:MM:SS</c> (hours omitted
    /// when less than 60 minutes).
    /// </summary>
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
