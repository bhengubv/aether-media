// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Skin;

/// <summary>
/// Sprite-coordinate constants for the classic Winamp 2.x main window. The
/// values match the published "Winamp Skinning Guide" — every classic skin
/// in the museum lays its bitmaps out the same way, so a single coordinate
/// table works for all of them.
/// </summary>
public static class WinampMainWindowLayout
{
    /// <summary>Main window background lives at 0,0 in main.bmp; sized 275 × 116.</summary>
    public static WinampSpriteSlice Background { get; } = new("main", 0, 0, 275, 116);

    /// <summary>Active title bar — top 14 px of titlebar.bmp.</summary>
    public static WinampSpriteSlice TitleBarActive   { get; } = new("titlebar", 27, 0, 275, 14);
    /// <summary>Inactive title bar.</summary>
    public static WinampSpriteSlice TitleBarInactive { get; } = new("titlebar", 27, 15, 275, 14);

    /// <summary>Transport buttons sprite sheet — cbuttons.bmp packs 5 buttons.</summary>
    /// <remarks>Layout: prev / play / pause / stop / next, each 23 px wide × 18 px tall.</remarks>
    public static WinampSpriteSlice ButtonPrevUp    { get; } = new("cbuttons", 0,   0, 23, 18);
    public static WinampSpriteSlice ButtonPlayUp    { get; } = new("cbuttons", 23,  0, 23, 18);
    public static WinampSpriteSlice ButtonPauseUp   { get; } = new("cbuttons", 46,  0, 23, 18);
    public static WinampSpriteSlice ButtonStopUp    { get; } = new("cbuttons", 69,  0, 23, 18);
    public static WinampSpriteSlice ButtonNextUp    { get; } = new("cbuttons", 92,  0, 22, 18);
    public static WinampSpriteSlice ButtonPrevDown  { get; } = new("cbuttons", 0,  18, 23, 18);
    public static WinampSpriteSlice ButtonPlayDown  { get; } = new("cbuttons", 23, 18, 23, 18);
    public static WinampSpriteSlice ButtonPauseDown { get; } = new("cbuttons", 46, 18, 23, 18);
    public static WinampSpriteSlice ButtonStopDown  { get; } = new("cbuttons", 69, 18, 23, 18);
    public static WinampSpriteSlice ButtonNextDown  { get; } = new("cbuttons", 92, 18, 22, 18);

    /// <summary>Window-level destination rectangle for the cbuttons row — y = 88 on main.</summary>
    public static (int X, int Y) ButtonsOrigin { get; } = (16, 88);

    /// <summary>Where the title-bar slice lands inside the main window.</summary>
    public static (int X, int Y) TitleBarOrigin { get; } = (0, 0);

    /// <summary>Approximate area used for the visualisation widget (oscilloscope / spectrum).</summary>
    public static WinampSpriteSlice VisualizationViewport { get; } = new("main", 24, 43, 76, 16);
}
