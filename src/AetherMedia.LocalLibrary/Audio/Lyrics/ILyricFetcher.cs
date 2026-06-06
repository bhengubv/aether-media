// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Lyrics;

/// <summary>
/// Fetches lyrics for a track from an online directory. Returns null when no
/// match is found; an empty <see cref="LrcFile"/> is reserved for "found but
/// no lyrics yet".
/// </summary>
public interface ILyricFetcher
{
    Task<LrcFile?> FetchAsync(string artist, string trackTitle, string? album = null, CancellationToken ct = default);
}
