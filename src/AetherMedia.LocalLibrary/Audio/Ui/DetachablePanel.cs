// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Ui;

/// <summary>One panel that can be detached into its own window.</summary>
public enum DetachablePanel
{
    Playlist,
    Equalizer,
    Visualizer,
    Library,
}

/// <summary>
/// Tracks which of the secondary panels are detached vs docked to the main
/// window — Winamp's separable Playlist / EQ / AVS windows.
/// </summary>
public interface IDetachablePanelManager
{
    /// <summary>True if the panel is currently floating in its own window.</summary>
    bool IsDetached(DetachablePanel panel);

    /// <summary>Detach a panel into its own window.</summary>
    void Detach(DetachablePanel panel);

    /// <summary>Re-dock a panel into the main window.</summary>
    void Attach(DetachablePanel panel);

    /// <summary>Fires when a panel's state changes.</summary>
    event EventHandler<(DetachablePanel Panel, bool IsDetached)>? StateChanged;
}
