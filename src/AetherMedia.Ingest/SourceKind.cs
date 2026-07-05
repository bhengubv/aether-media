// SPDX-License-Identifier: MIT

namespace AetherMedia.Ingest;

/// <summary>
/// A hint for the kind of external source. Dispatch is by
/// <see cref="ISourceAdapter.CanHandle"/>, so adding a new kind needs no change here — the
/// list only reflects the declared surface, never a cap on it.
/// </summary>
public enum SourceKind
{
    /// <summary>HTTP Live Streaming (<c>.m3u8</c>).</summary>
    Hls,

    /// <summary>MPEG-DASH (<c>.mpd</c>).</summary>
    Dash,

    /// <summary>A single continuous HTTP byte stream (progressive / MPEG-TS-over-HTTP).</summary>
    Continuous,

    /// <summary>A local or remote file.</summary>
    File,

    /// <summary>A live capture device (camera / screen).</summary>
    Capture,

    /// <summary>WebRTC ingest (WHEP).</summary>
    WebRtc,

    /// <summary>RTMP contribution.</summary>
    Rtmp,

    /// <summary>SRT contribution.</summary>
    Srt,
}
