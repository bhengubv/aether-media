// SPDX-License-Identifier: MIT

namespace AetherMedia.Ingest;

/// <summary>The kind of media a <see cref="MediaSegment"/> carries.</summary>
public enum TrackKind
{
    /// <summary>Encoded (possibly muxed) video.</summary>
    Video,

    /// <summary>Encoded audio.</summary>
    Audio,

    /// <summary>Captions / subtitles (e.g. WebVTT).</summary>
    Text,

    /// <summary>Timed metadata (e.g. ID3, SCTE-35).</summary>
    Metadata,
}
