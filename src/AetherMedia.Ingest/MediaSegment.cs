// SPDX-License-Identifier: MIT

namespace AetherMedia.Ingest;

/// <summary>
/// The normalisation target every <see cref="ISourceAdapter"/> emits and every gateway path
/// consumes: one encoded, self-describing media segment. Anything reducible to this plugs onto
/// the mesh unchanged — multi-audio, captions and timed metadata are simply more segments with a
/// different <see cref="Track"/>, so the ceiling is never capped by the format.
/// </summary>
public sealed record MediaSegment
{
    /// <summary>What kind of media this segment carries.</summary>
    public required TrackKind Track { get; init; }

    /// <summary>Advisory codec label, e.g. <c>"h264"</c>, <c>"aac"</c>, <c>"webvtt"</c>.</summary>
    public required string Codec { get; init; }

    /// <summary>Bitrate rung in Kbps, or 0 when the source declares no ladder.</summary>
    public required int RungBitrateKbps { get; init; }

    /// <summary>Presentation timestamp in milliseconds from the start of ingest.</summary>
    public required long PresentationTimeMs { get; init; }

    /// <summary>Segment duration in milliseconds.</summary>
    public required long DurationMs { get; init; }

    /// <summary>Monotonically increasing sequence number for this track.</summary>
    public required uint Sequence { get; init; }

    /// <summary>True when the segment is independently decodable (random-access).</summary>
    public required bool IsKeyframe { get; init; }

    /// <summary>The opaque, codec-encoded payload carried verbatim over the mesh.</summary>
    public required ReadOnlyMemory<byte> Payload { get; init; }

    /// <summary>Advisory container label, e.g. <c>"ts"</c>, <c>"mp4"</c>, or empty for raw.</summary>
    public string Container { get; init; } = "";
}
