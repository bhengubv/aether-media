// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Models;

namespace AetherMedia.LocalLibrary.Interfaces;

/// <summary>
/// Reads and writes Kodi-compatible <c>.nfo</c> XML files that live alongside video files.
/// The NFO file shares the video file's stem name:
/// <c>/media/films/The Matrix (1999)/The Matrix (1999).nfo</c>.
/// </summary>
public interface IMovieMetadataService
{
    /// <summary>
    /// Returns the expected <c>.nfo</c> path for <paramref name="videoFilePath"/>.
    /// The method does NOT check whether the file exists.
    /// </summary>
    string GetNfoPath(string videoFilePath);

    /// <summary>
    /// Reads metadata from the NFO file alongside <paramref name="videoFilePath"/>.
    /// Returns <c>null</c> if no NFO file is present.
    /// </summary>
    Task<MovieMetadata?> ReadAsync(string videoFilePath, CancellationToken ct = default);

    /// <summary>
    /// Serialises <paramref name="metadata"/> to a Kodi-compatible NFO XML file placed
    /// alongside the video file.  Creates or overwrites the NFO file.
    /// </summary>
    Task WriteAsync(MovieMetadata metadata, CancellationToken ct = default);
}
