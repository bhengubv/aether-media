// SPDX-License-Identifier: MIT

using AetherMesh.Media.LocalLibrary.Models;

namespace AetherMesh.Media.LocalLibrary.Interfaces;

/// <summary>
/// Finds and downloads subtitles via the OpenSubtitles REST API v1.
///
/// To use this service provide your API key via the constructor or application settings.
/// The service degrades gracefully (returns empty results) when the key is absent or
/// the network is unavailable — it never throws for transient failures.
/// </summary>
public interface ISubtitleService
{
    /// <summary>
    /// Searches for subtitles for the video at <paramref name="videoFilePath"/>.
    ///
    /// Strategy:
    /// <list type="number">
    ///   <item>Compute the movie hash and search by hash (high-confidence match).</item>
    ///   <item>If the hash search returns no results, fall back to a title + year search.</item>
    /// </list>
    ///
    /// Results are ordered by hash-match first, then by download count descending.
    ///
    /// Returns an empty list (never throws) if:
    /// <list type="bullet">
    ///   <item>no API key is configured,</item>
    ///   <item>the file is too short to hash,</item>
    ///   <item>the network is unavailable, or</item>
    ///   <item>OpenSubtitles returns an error.</item>
    /// </list>
    /// </summary>
    Task<IReadOnlyList<SubtitleSearchResult>> SearchAsync(
        string  videoFilePath,
        string? titleOverride = null,
        int?    yearOverride  = null,
        string  language     = "en",
        CancellationToken ct = default);

    /// <summary>
    /// Downloads the subtitle identified by <paramref name="fileId"/> and saves it as
    /// an <c>.srt</c> file alongside <paramref name="videoFilePath"/> with the same stem.
    ///
    /// If a subtitle file already exists at the target path it is overwritten.
    ///
    /// Returns the absolute path to the downloaded <c>.srt</c> file.
    /// </summary>
    Task<string> DownloadAsync(
        string videoFilePath,
        string fileId,
        CancellationToken ct = default);
}
