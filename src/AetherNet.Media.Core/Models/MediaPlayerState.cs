namespace AetherNet.Media.Core.Models;

/// <summary>
/// Represents the playback state of a media player instance.
/// </summary>
public enum MediaPlayerState
{
    /// <summary>Player is initialised but no media has been loaded.</summary>
    Idle = 0,

    /// <summary>Media is actively decoding and rendering frames/samples.</summary>
    Playing = 1,

    /// <summary>Playback is paused at the current position.</summary>
    Paused = 2,

    /// <summary>Player is waiting for the network or decoder to supply more data.</summary>
    Buffering = 3,

    /// <summary>Playback reached the natural end of the media item.</summary>
    Ended = 4,

    /// <summary>An unrecoverable error has occurred; the player must be re-opened.</summary>
    Error = 5,
}
