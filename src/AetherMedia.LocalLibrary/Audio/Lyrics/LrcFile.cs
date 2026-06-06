// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Lyrics;

/// <summary>
/// A parsed LRC file. <see cref="Lines"/> is sorted by offset so callers can
/// binary-search the current line.
/// </summary>
public sealed record LrcFile(
    string? Title,
    string? Artist,
    string? Album,
    IReadOnlyList<LyricLine> Lines)
{
    /// <summary>Empty LRC — no lines, no metadata.</summary>
    public static LrcFile Empty { get; } = new(null, null, null, Array.Empty<LyricLine>());
}
