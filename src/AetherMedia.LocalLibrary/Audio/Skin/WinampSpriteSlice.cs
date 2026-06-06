// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Skin;

/// <summary>
/// A rectangular region within a named sprite sheet — addressed by the
/// (atlas-relative) bitmap name plus the pixel rectangle inside it.
/// Matches the documented layout of Winamp 2.x classic skins, where each
/// .bmp packs many UI elements at known offsets.
/// </summary>
public sealed record WinampSpriteSlice(
    string SpriteName,
    int X,
    int Y,
    int Width,
    int Height);
