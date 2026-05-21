// SPDX-License-Identifier: MIT

namespace Aether.Media.Reel;

/// <summary>
/// Describes how a <see cref="Reel"/> relates to existing content.
/// </summary>
public enum ReelType
{
    /// <summary>Entirely new content — not a response to anything.</summary>
    Original,

    /// <summary>
    /// Side-by-side response to another Reel.
    /// <see cref="Reel.SourceReelHash"/> identifies the original.
    /// </summary>
    Duet,

    /// <summary>
    /// Clips the first 5 s of another Reel then continues with new footage.
    /// <see cref="Reel.SourceReelHash"/> identifies the original.
    /// </summary>
    Stitch,
}
