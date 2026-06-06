// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Audio.Crossfade;

/// <summary>
/// How transitions between consecutive media items should be rendered.
/// </summary>
public enum CrossfadeMode
{
    /// <summary>Hard cut: end track A, then start track B. Default for video.</summary>
    Off,

    /// <summary>
    /// Sample-accurate transition with no silence between tracks. The first
    /// sample of B is rendered the frame after the last sample of A. The
    /// standard for albums mixed as a continuous program (Pink Floyd,
    /// classical movements, DJ mixes).
    /// </summary>
    Gapless,

    /// <summary>
    /// Fade A's tail down while fading B's head up over a configurable window.
    /// </summary>
    Crossfade,
}
