// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Playlists;

/// <summary>Reads a playlist from a file path or stream.</summary>
public interface IPlaylistReader
{
    /// <summary>The playlist format this reader handles, e.g. "m3u".</summary>
    string FormatId { get; }

    /// <summary>Read a playlist from disk.</summary>
    Task<Playlist> ReadAsync(string filePath, CancellationToken ct = default);

    /// <summary>Read a playlist from an in-memory stream.</summary>
    Task<Playlist> ReadAsync(Stream stream, CancellationToken ct = default);
}
