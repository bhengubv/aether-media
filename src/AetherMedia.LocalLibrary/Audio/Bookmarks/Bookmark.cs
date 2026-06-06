// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Bookmarks;

/// <summary>
/// A saved position inside a track. Winamp's <c>Add to Bookmarks</c> /
/// <c>Resume</c> menu items map here. The optional <see cref="Label"/> lets
/// the user remember why they bookmarked it ("come back to this riff").
/// </summary>
/// <param name="FilePath">Absolute path of the bookmarked file.</param>
/// <param name="PositionMs">Position within the file in milliseconds.</param>
/// <param name="Label">Optional human-readable label.</param>
/// <param name="CreatedAtUtc">When the bookmark was created.</param>
public sealed record Bookmark(
    string FilePath,
    long PositionMs,
    string? Label = null,
    DateTimeOffset CreatedAtUtc = default);
