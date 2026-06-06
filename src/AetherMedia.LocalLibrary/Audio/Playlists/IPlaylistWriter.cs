// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Playlists;

/// <summary>Writes a playlist to a file path or stream.</summary>
public interface IPlaylistWriter
{
    /// <summary>The playlist format this writer emits, e.g. "m3u".</summary>
    string FormatId { get; }

    /// <summary>Write a playlist to disk (UTF-8, no BOM unless required by format).</summary>
    Task WriteAsync(string filePath, Playlist playlist, CancellationToken ct = default);

    /// <summary>Write a playlist to a stream.</summary>
    Task WriteAsync(Stream stream, Playlist playlist, CancellationToken ct = default);
}
