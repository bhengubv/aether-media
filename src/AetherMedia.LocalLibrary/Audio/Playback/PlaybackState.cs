// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Playback;

/// <summary>What the engine is doing right now.</summary>
public enum PlaybackState
{
    /// <summary>Nothing loaded. The output device is closed.</summary>
    Idle = 0,

    /// <summary>A source is open and the device is running.</summary>
    Playing = 1,

    /// <summary>A source is open, the device is held but silent. Position is preserved.</summary>
    Paused = 2,

    /// <summary>
    /// A source is open but the decoder is still filling — the first buffers have not
    /// arrived. Distinct from <see cref="Playing"/> so a UI can say "loading" instead of
    /// showing a running clock against silence.
    /// </summary>
    Buffering = 3
}

/// <summary>Why a track stopped. Only <see cref="Failed"/> is evidence against the file.</summary>
public enum TrackEndReason
{
    /// <summary>Decoder reported end-of-stream. The normal ending.</summary>
    Completed = 0,

    /// <summary>Something else was loaded, or Stop was called.</summary>
    Replaced = 1,

    /// <summary>The decoder gave up mid-file — a truncated or corrupt source.</summary>
    Failed = 2
}

/// <summary>Raised when a track stops, whatever the cause.</summary>
public sealed class TrackEndedEventArgs(string sourcePath, TrackEndReason reason, Exception? error = null)
    : EventArgs
{
    /// <summary>The source that ended.</summary>
    public string SourcePath { get; } = sourcePath;

    /// <summary>Why it ended.</summary>
    public TrackEndReason Reason { get; } = reason;

    /// <summary>The decoder fault, when <see cref="Reason"/> is <see cref="TrackEndReason.Failed"/>.</summary>
    public Exception? Error { get; } = error;
}
