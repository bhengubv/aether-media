// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Library;

/// <summary>
/// Downloads album / track cover art from an online directory. Returns the
/// JPEG / PNG bytes the caller can embed in tags (via
/// <see cref="Tags.AudioTags"/>) or cache for the library UI.
/// </summary>
public interface ICoverArtFetcher
{
    /// <summary>
    /// Search by tag triplet — artist + album + track (track optional, helps
    /// for compilations). Returns the highest-confidence image bytes or null.
    /// </summary>
    Task<byte[]?> FetchAsync(string artist, string album, string? track = null, CancellationToken ct = default);
}
