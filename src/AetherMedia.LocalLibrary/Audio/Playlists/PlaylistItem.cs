// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Playlists;

/// <summary>
/// A single entry in a playlist file.
/// </summary>
/// <param name="Path">
/// File path or URI. Local files are stored as the verbatim path emitted by
/// the playlist writer (the loader preserves relative paths). Stream entries
/// hold their HTTP/Aether URL here.
/// </param>
/// <param name="Title">
/// Optional display title — populated from <c>#EXTINF</c> (M3U), the
/// <c>TitleN</c> entry (PLS), or the <c>&lt;title&gt;</c> element (XSPF).
/// Null when the format doesn't carry one.
/// </param>
/// <param name="DurationSeconds">
/// Optional track length, when the format carries it (<c>#EXTINF:NN</c>,
/// PLS <c>LengthN</c>, XSPF <c>duration</c>).
/// </param>
public sealed record PlaylistItem(
    string Path,
    string? Title = null,
    int? DurationSeconds = null);
