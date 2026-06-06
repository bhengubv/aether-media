// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Crossfade;

/// <summary>How transitions between consecutive media items render.</summary>
public enum CrossfadeMode
{
    /// <summary>Hard cut: end A, then start B. Default for video.</summary>
    Off,

    /// <summary>
    /// Sample-accurate transition with no silence — first sample of B is
    /// rendered the frame after the last sample of A.
    /// </summary>
    Gapless,

    /// <summary>
    /// Fade A's tail down while fading B's head up over a configurable window.
    /// </summary>
    Crossfade,
}
