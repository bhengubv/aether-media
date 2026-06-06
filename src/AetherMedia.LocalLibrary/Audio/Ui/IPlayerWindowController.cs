// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Ui;

/// <summary>
/// Holds the player window's display state. Host shells (Avalonia, MAUI,
/// WinUI) subscribe to <see cref="ModeChanged"/> / <see cref="AlwaysOnTopChanged"/>
/// and apply the corresponding Window properties.
/// </summary>
public interface IPlayerWindowController
{
    /// <summary>Current display mode.</summary>
    PlayerWindowMode Mode { get; set; }

    /// <summary>Whether the window is pinned above other apps.</summary>
    bool AlwaysOnTop { get; set; }

    /// <summary>Fires when <see cref="Mode"/> changes.</summary>
    event EventHandler<PlayerWindowMode>? ModeChanged;

    /// <summary>Fires when <see cref="AlwaysOnTop"/> flips.</summary>
    event EventHandler<bool>? AlwaysOnTopChanged;
}
