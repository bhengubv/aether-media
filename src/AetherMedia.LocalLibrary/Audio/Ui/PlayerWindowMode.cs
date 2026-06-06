// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Ui;

/// <summary>
/// Window-size presets the player can switch between — Winamp's
/// <c>Window Shade</c> / <c>Mini</c> / <c>Double-Size</c> modes consolidated.
/// </summary>
public enum PlayerWindowMode
{
    /// <summary>Standard 1× player window.</summary>
    Normal,

    /// <summary>Mini-player — tiny strip with title + transport controls.</summary>
    Mini,

    /// <summary>Window shade — collapsed to the title bar only.</summary>
    WindowShade,

    /// <summary>Compact — half-height with playlist hidden.</summary>
    Compact,

    /// <summary>2× scale — pixel-doubled for HiDPI screens.</summary>
    DoubleSize,
}
