using AetherMesh.Media.Core.Models;

namespace AetherMesh.Media.Core;

/// <summary>
/// Controls playback of a single media item (audio or video).
/// Implementations wrap platform-specific decoders (e.g. ExoPlayer, AVPlayer,
/// MediaElement) while exposing a uniform async API to the Aether layer.
/// </summary>
public interface IMediaPlayer : IAsyncDisposable
{
    // ── State ──────────────────────────────────────────────────────────────

    /// <summary>Current lifecycle state of the player.</summary>
    MediaPlayerState State { get; }

    /// <summary>Current playback position in milliseconds.</summary>
    long PositionMs { get; }

    /// <summary>
    /// Total duration in milliseconds of the loaded item, or 0 when unknown
    /// (e.g. live streams).
    /// </summary>
    long DurationMs { get; }

    /// <summary>Output volume in the range [0.0, 1.0].</summary>
    double Volume { get; }

    /// <summary>Playback speed multiplier in the range [0.5, 4.0].</summary>
    double PlaybackSpeed { get; }

    /// <summary><c>true</c> when audio output is muted.</summary>
    bool IsMuted { get; }

    /// <summary>
    /// SHA-256 hex hash of the currently loaded content, or <c>null</c> when
    /// the player opened a raw URI rather than a catalogued
    /// <see cref="MediaContent"/> item.
    /// </summary>
    string? CurrentContentHash { get; }

    // ── Events ─────────────────────────────────────────────────────────────

    /// <summary>Raised whenever <see cref="State"/> transitions.</summary>
    event EventHandler<MediaPlayerState>? StateChanged;

    /// <summary>
    /// Raised approximately every 500 ms while the player is in the
    /// <see cref="MediaPlayerState.Playing"/> state.  The argument is the
    /// current position in milliseconds.
    /// </summary>
    event EventHandler<long>? PositionChanged;

    /// <summary>Raised after a new media item has been decoded and is ready to play.</summary>
    event EventHandler<MediaContent>? MediaLoaded;

    /// <summary>Raised when playback reaches the natural end of the media item.</summary>
    event EventHandler? MediaEnded;

    /// <summary>
    /// Raised when an unrecoverable playback error occurs.  The argument is a
    /// human-readable error message.
    /// </summary>
    event EventHandler<string>? ErrorOccurred;

    // ── Commands ───────────────────────────────────────────────────────────

    /// <summary>
    /// Opens and prepares a media item from an arbitrary URI (local path or
    /// network URL).  Does not start playback.
    /// </summary>
    Task OpenAsync(string uri, CancellationToken ct = default);

    /// <summary>
    /// Opens a catalogued <see cref="MediaContent"/> item using a pre-resolved
    /// local file path.  Allows the player to surface metadata (title, art,
    /// duration) immediately without an additional network round-trip.
    /// Does not start playback.
    /// </summary>
    Task OpenContentAsync(MediaContent content, string localPath, CancellationToken ct = default);

    /// <summary>Begins or resumes playback of the loaded item.</summary>
    Task PlayAsync(CancellationToken ct = default);

    /// <summary>Pauses playback at the current position.</summary>
    Task PauseAsync(CancellationToken ct = default);

    /// <summary>Stops playback and resets the position to 0.</summary>
    Task StopAsync(CancellationToken ct = default);

    /// <summary>Seeks to the specified position in milliseconds.</summary>
    Task SeekAsync(long positionMs, CancellationToken ct = default);

    /// <summary>
    /// Sets the output volume.  Values outside [0.0, 1.0] are clamped to the
    /// nearest boundary.
    /// </summary>
    Task SetVolumeAsync(double volume, CancellationToken ct = default);

    /// <summary>
    /// Sets the playback speed multiplier.  Values outside [0.5, 4.0] are
    /// clamped to the nearest boundary.
    /// </summary>
    Task SetSpeedAsync(double speed, CancellationToken ct = default);

    /// <summary>Mutes audio output without changing the stored volume level.</summary>
    Task MuteAsync(CancellationToken ct = default);

    /// <summary>Restores audio output to the stored volume level.</summary>
    Task UnmuteAsync(CancellationToken ct = default);
}
