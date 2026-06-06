// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Lyrics;

/// <summary>
/// One line of synchronised lyrics — a wall-clock offset into the track
/// (always non-negative) plus the line of text shown at that time.
/// </summary>
public sealed record LyricLine(TimeSpan Offset, string Text);
