// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Ui;

/// <summary>
/// Default in-memory <see cref="IPlayerWindowController"/>. Thread-safe so
/// the controller can be flipped from a hotkey thread and the UI thread
/// gets the notification.
/// </summary>
public sealed class PlayerWindowController : IPlayerWindowController
{
    private readonly object _gate = new();
    private PlayerWindowMode _mode = PlayerWindowMode.Normal;
    private bool _alwaysOnTop;

    /// <inheritdoc/>
    public event EventHandler<PlayerWindowMode>? ModeChanged;

    /// <inheritdoc/>
    public event EventHandler<bool>? AlwaysOnTopChanged;

    /// <inheritdoc/>
    public PlayerWindowMode Mode
    {
        get { lock (_gate) return _mode; }
        set
        {
            bool changed;
            lock (_gate) { changed = _mode != value; _mode = value; }
            if (changed) ModeChanged?.Invoke(this, value);
        }
    }

    /// <inheritdoc/>
    public bool AlwaysOnTop
    {
        get { lock (_gate) return _alwaysOnTop; }
        set
        {
            bool changed;
            lock (_gate) { changed = _alwaysOnTop != value; _alwaysOnTop = value; }
            if (changed) AlwaysOnTopChanged?.Invoke(this, value);
        }
    }
}
