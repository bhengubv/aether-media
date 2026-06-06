// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Time;

/// <summary>
/// Default <see cref="ISleepTimer"/> built on <see cref="System.Threading.Timer"/>.
/// </summary>
public sealed class SleepTimer : ISleepTimer
{
    private readonly object _gate = new();
    private System.Threading.Timer? _timer;
    private DateTimeOffset _scheduledFor;
    private TimeSpan _delay;
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler? Elapsed;

    /// <inheritdoc/>
    public bool IsArmed
    {
        get { lock (_gate) return _timer is not null; }
    }

    /// <inheritdoc/>
    public TimeSpan? Remaining
    {
        get
        {
            lock (_gate)
            {
                if (_timer is null) return null;
                var rem = _scheduledFor - DateTimeOffset.UtcNow;
                return rem < TimeSpan.Zero ? TimeSpan.Zero : rem;
            }
        }
    }

    /// <inheritdoc/>
    public void Arm(TimeSpan delay)
    {
        if (delay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), "Delay must be positive.");

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _timer?.Dispose();
            _delay = delay;
            _scheduledFor = DateTimeOffset.UtcNow + delay;
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
        }
    }

    private void OnTimer(object? state)
    {
        lock (_gate) _timer = null;
        Elapsed?.Invoke(this, EventArgs.Empty);
    }
}
