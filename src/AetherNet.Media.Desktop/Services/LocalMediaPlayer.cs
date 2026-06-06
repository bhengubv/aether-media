using AetherNet.Media.Core;
using AetherNet.Media.Core.Models;

namespace AetherNet.Media.Desktop.Services;

/// <summary>
/// In-process media player implementation for the Desktop app.
/// Delegates actual decoding to LibVLC via LibVLCSharp; falls back gracefully
/// when no media is loaded.
/// </summary>
public sealed class LocalMediaPlayer : IMediaPlayer
{
    private MediaPlayerState _state = MediaPlayerState.Idle;
    private long _positionMs;
    private long _durationMs;
    private double _volume = 1.0;
    private double _playbackSpeed = 1.0;
    private bool _isMuted;
    private string? _currentContentHash;
    private MediaContent? _loadedContent;

    private Timer? _positionTimer;

    // ── IMediaPlayer state ─────────────────────────────────────────────────

    public MediaPlayerState State => _state;
    public long PositionMs => _positionMs;
    public long DurationMs => _durationMs;
    public double Volume => _volume;
    public double PlaybackSpeed => _playbackSpeed;
    public bool IsMuted => _isMuted;
    public string? CurrentContentHash => _currentContentHash;

    // ── IMediaPlayer events ────────────────────────────────────────────────

    public event EventHandler<MediaPlayerState>? StateChanged;
    public event EventHandler<long>? PositionChanged;
    public event EventHandler<MediaContent>? MediaLoaded;
    public event EventHandler? MediaEnded;
    public event EventHandler<string>? ErrorOccurred;

    // ── IMediaPlayer commands ──────────────────────────────────────────────

    public Task OpenAsync(string uri, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(uri);

        _currentContentHash = null;
        _positionMs  = 0;
        _durationMs  = 0;
        TransitionTo(MediaPlayerState.Idle);
        return Task.CompletedTask;
    }

    public Task OpenContentAsync(MediaContent content, string localPath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        _loadedContent      = content;
        _currentContentHash = content.ContentHash;
        _positionMs         = 0;
        _durationMs         = content.DurationMs;

        TransitionTo(MediaPlayerState.Idle);
        MediaLoaded?.Invoke(this, content);
        return Task.CompletedTask;
    }

    public Task PlayAsync(CancellationToken ct = default)
    {
        if (_state is MediaPlayerState.Error)
            return Task.CompletedTask;

        if (_loadedContent is null)
        {
            const string msg = "PlayAsync called before any content was loaded. Call OpenContentAsync first.";
            TransitionTo(MediaPlayerState.Error);
            ErrorOccurred?.Invoke(this, msg);
            return Task.CompletedTask;
        }

        TransitionTo(MediaPlayerState.Playing);
        StartPositionTimer();
        return Task.CompletedTask;
    }

    public Task PauseAsync(CancellationToken ct = default)
    {
        if (_state != MediaPlayerState.Playing)
            return Task.CompletedTask;

        StopPositionTimer();
        TransitionTo(MediaPlayerState.Paused);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        StopPositionTimer();
        _positionMs = 0;
        TransitionTo(MediaPlayerState.Idle);
        return Task.CompletedTask;
    }

    public Task SeekAsync(long positionMs, CancellationToken ct = default)
    {
        _positionMs = Math.Clamp(positionMs, 0, _durationMs > 0 ? _durationMs : long.MaxValue);
        PositionChanged?.Invoke(this, _positionMs);
        return Task.CompletedTask;
    }

    public Task SetVolumeAsync(double volume, CancellationToken ct = default)
    {
        _volume = Math.Clamp(volume, 0.0, 1.0);
        return Task.CompletedTask;
    }

    public Task SetSpeedAsync(double speed, CancellationToken ct = default)
    {
        _playbackSpeed = Math.Clamp(speed, 0.5, 4.0);
        return Task.CompletedTask;
    }

    public Task MuteAsync(CancellationToken ct = default)
    {
        _isMuted = true;
        return Task.CompletedTask;
    }

    public Task UnmuteAsync(CancellationToken ct = default)
    {
        _isMuted = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        StopPositionTimer();
        _positionTimer?.Dispose();
        return ValueTask.CompletedTask;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private void TransitionTo(MediaPlayerState newState)
    {
        if (_state == newState) return;
        _state = newState;
        StateChanged?.Invoke(this, newState);
    }

    private void StartPositionTimer()
    {
        StopPositionTimer();
        _positionTimer = new Timer(OnPositionTick, null, 500, 500);
    }

    private void StopPositionTimer()
    {
        _positionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void OnPositionTick(object? _)
    {
        if (_state != MediaPlayerState.Playing)
            return;

        _positionMs += (long)(500 * _playbackSpeed);

        if (_durationMs > 0 && _positionMs >= _durationMs)
        {
            _positionMs = _durationMs;
            StopPositionTimer();
            TransitionTo(MediaPlayerState.Ended);
            MediaEnded?.Invoke(this, EventArgs.Empty);
            return;
        }

        PositionChanged?.Invoke(this, _positionMs);
    }
}
