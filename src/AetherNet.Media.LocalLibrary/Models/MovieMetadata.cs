// SPDX-License-Identifier: MIT

namespace AetherNet.Media.LocalLibrary.Models;

/// <summary>
/// Metadata for a video file.  Persisted as a Kodi-compatible <c>.nfo</c> XML file
/// placed alongside the video file with the same stem name.
/// </summary>
public sealed class MovieMetadata
{
    /// <summary>Absolute path to the video file.  Read-only after construction.</summary>
    public required string FilePath { get; init; }

    public string   Title          { get; set; } = string.Empty;
    public int      Year           { get; set; }
    public string   Plot           { get; set; } = string.Empty;
    public string   Tagline        { get; set; } = string.Empty;

    /// <summary>Rating on a 0–10 scale (matches TMDB/IMDB convention).</summary>
    public float    Rating         { get; set; }

    public int      RuntimeMinutes { get; set; }
    public string[] Genres         { get; set; } = [];
    public string[] Directors      { get; set; } = [];
    public string[] Cast           { get; set; } = [];

    /// <summary>TMDB numeric ID as a string, or <c>null</c> if not linked.</summary>
    public string?  TmdbId         { get; set; }

    /// <summary>IMDB ID in <c>tt0000000</c> format, or <c>null</c> if not linked.</summary>
    public string?  ImdbId         { get; set; }

    /// <summary>Whether the user has marked this film as watched.</summary>
    public bool     Watched        { get; set; }

    /// <summary>
    /// Path to a local poster image (usually <c>movie-poster.jpg</c> alongside the video),
    /// or <c>null</c> if none.
    /// </summary>
    public string?  PosterPath     { get; set; }
}
