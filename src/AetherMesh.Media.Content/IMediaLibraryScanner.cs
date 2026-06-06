// SPDX-License-Identifier: MIT

using AetherMesh.Media.Core.Models;

namespace AetherMesh.Media.Content;

/// <summary>
/// Walks the local filesystem for supported media files, extracts metadata via
/// TagLibSharp, hashes content, and publishes descriptors to the Aether content layer.
/// </summary>
public interface IMediaLibraryScanner
{
    /// <summary>
    /// Scans a directory for supported audio/video files and returns one
    /// <see cref="ScannedMediaItem"/> per successfully resolved file.
    /// Each item pairs the <see cref="MediaContent"/> descriptor with the
    /// absolute source file path so callers can open the file for metadata
    /// editing or subtitle lookup without a reverse hash lookup.
    /// Fires <see cref="ContentDiscovered"/> once per item.
    /// </summary>
    Task<IReadOnlyList<ScannedMediaItem>> ScanDirectoryAsync(
        string path,
        bool recursive,
        CancellationToken ct = default);

    /// <summary>
    /// Scan a single file. Returns null when the file extension is unsupported or
    /// metadata resolution fails.
    /// </summary>
    Task<MediaContent?> ScanFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>Fired each time a new <see cref="MediaContent"/> record is created during a scan.</summary>
    event EventHandler<MediaContent>? ContentDiscovered;
}
