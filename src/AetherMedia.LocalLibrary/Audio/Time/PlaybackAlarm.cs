// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Time;

/// <summary>
/// Default <see cref="IPlaybackAlarm"/> built on <see cref="System.Threading.Timer"/>.
/// Single-shot — re-arm in the <see cref="IPlaybackAlarm.Triggered"/> handler
/// for daily / weekly alarms.
/// </summary>
public sealed class PlaybackAlarm : IPlaybackAlarm
{
    private readonly object _gate = new();
    private System.Threading.Timer? _timer;
    private DateTimeOffset? _next;
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler? Triggered;

    /// <inheritdoc/>
    public DateTimeOffset? NextTrigger
    {
        get { lock (_gate) return _next; }
    }

    /// <inheritdoc/>
    public void ArmAt(DateTimeOffset when)
    {
        var delay = when - DateTimeOffset.UtcNow;
        if (delay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(when), "Alarm time must be in the future.");

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _timer?.Dispose();
            _next = when;
            _timer = new System.Threading.Timer(OnTimer, state: null, delay, System.Threading.Timeout.InfiniteTimeSpan);
        }
    }

    /// <inheritdoc/>
    public void Cancel()
    {
        lock (_gate)
        {
            _timer?.Dispose();
            _timer = null;
            _next = null;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _timer?.Dispose();
            _timer = null;
            _next = null;
        }
    }

    private void OnTimer(object? state)
    {
        lock (_gate)
        {
            _timer = null;
            _next = null;
        }
        Triggered?.Invoke(this, EventArgs.Empty);
    }
}
