// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Ui;

/// <summary>Default in-memory <see cref="IDetachablePanelManager"/>.</summary>
public sealed class DetachablePanelManager : IDetachablePanelManager
{
    private readonly object _gate = new();
    private readonly HashSet<DetachablePanel> _detached = new();

    /// <inheritdoc/>
    public event EventHandler<(DetachablePanel Panel, bool IsDetached)>? StateChanged;

    /// <inheritdoc/>
    public bool IsDetached(DetachablePanel panel)
    {
        lock (_gate) return _detached.Contains(panel);
    }

    /// <inheritdoc/>
    public void Detach(DetachablePanel panel)
    {
        bool changed;
        lock (_gate) changed = _detached.Add(panel);
        if (changed) StateChanged?.Invoke(this, (panel, true));
    }

    /// <inheritdoc/>
    public void Attach(DetachablePanel panel)
    {
        bool changed;
        lock (_gate) changed = _detached.Remove(panel);
        if (changed) StateChanged?.Invoke(this, (panel, false));
    }
}
