// SPDX-License-Identifier: MIT

namespace AetherMesh.Media.LocalLibrary.Models;

/// <summary>
/// A single subtitle candidate returned by the OpenSubtitles search.
/// Pass <see cref="FileId"/> to <see cref="Interfaces.ISubtitleService.DownloadAsync"/> to fetch the file.
/// </summary>
public sealed record SubtitleSearchResult(
    /// <summary>OpenSubtitles internal file ID — used to request a download link.</summary>
    string FileId,

    /// <summary>Movie or series title from the OpenSubtitles database.</summary>
    string MovieTitle,

    /// <summary>BCP-47 language code (e.g. <c>"en"</c>, <c>"af"</c>).</summary>
    string Language,

    /// <summary>Release group / source name as listed on the site.</summary>
    string ReleaseName,

    /// <summary>Total number of times this subtitle has been downloaded.</summary>
    int DownloadCount,

    /// <summary>Community rating on a 0–10 scale.</summary>
    float Rating,

    /// <summary>Whether this result was matched on the movie file hash (high-confidence).</summary>
    bool HashMatch);
