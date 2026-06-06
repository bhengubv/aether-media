// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Ui;

/// <summary>
/// Default <see cref="IOnScreenDisplay"/>. Uses a <see cref="Timer"/> to fire
/// <see cref="IOnScreenDisplay.MessageDismissed"/> after the configured
/// duration. Host shells render the actual overlay (Avalonia popup, native
/// notification, OBS plug-in, etc.).
/// </summary>
public sealed class InMemoryOnScreenDisplay : IOnScreenDisplay, IDisposable
{
    private readonly object _gate = new();
    private System.Threading.Timer? _timer;
    private OsdMessage? _current;
    private bool _disposed;

    /// <inheritdoc/>
    public OsdMessage? CurrentMessage
    {
        get { lock (_gate) return _current; }
    }

    /// <inheritdoc/>
    public event EventHandler<OsdMessage>? MessageShown;

    /// <inheritdoc/>
    public event EventHandler? MessageDismissed;

    /// <inheritdoc/>
    public void Show(OsdMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _current = message;
            _timer?.Dispose();
            _timer = new System.Threading.Timer(OnElapsed, state: null,
                message.Duration, System.Threading.Timeout.InfiniteTimeSpan);
        }
        MessageShown?.Invoke(this, message);
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

    private void OnElapsed(object? state)
    {
        lock (_gate) _current = null;
        MessageDismissed?.Invoke(this, EventArgs.Empty);
    }
}
