// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Skin;

/// <summary>
/// Sprite-coordinate constants for the classic Winamp 2.x equaliser window
/// (<c>eqmain.bmp</c>). Used by <see cref="WinampEqRenderer"/> to paint a
/// skinned EQ window matching the player skin.
/// </summary>
public static class WinampEqWindowLayout
{
    /// <summary>EQ window background — full top portion of eqmain.bmp.</summary>
    public static WinampSpriteSlice Background { get; } = new("eqmain", 0, 0, 275, 116);

    /// <summary>Active title bar.</summary>
    public static WinampSpriteSlice TitleBarActive   { get; } = new("eqmain", 0, 134, 275, 14);

    /// <summary>Inactive title bar.</summary>
    public static WinampSpriteSlice TitleBarInactive { get; } = new("eqmain", 0, 149, 275, 14);

    /// <summary>EQ band slider thumb.</summary>
    public static WinampSpriteSlice SliderThumb { get; } = new("eqmain", 0, 164, 11, 11);

    /// <summary>Pressed (drag) slider thumb.</summary>
    public static WinampSpriteSlice SliderThumbPressed { get; } = new("eqmain", 12, 164, 11, 11);

    /// <summary>EQ-band slider rail.</summary>
    public static WinampSpriteSlice SliderRail { get; } = new("eqmain", 13, 164, 14, 63);

    /// <summary>X (frequency band) coordinates inside the EQ window — 10 bands.</summary>
    public static IReadOnlyList<int> BandX { get; } =
    [
        78, 96, 114, 132, 150, 168, 186, 204, 222, 240,
    ];

    /// <summary>Y top of the band slider area.</summary>
    public const int BandTopY = 38;

    /// <summary>Band slider travel range in pixels.</summary>
    public const int BandTravel = 63;
}
