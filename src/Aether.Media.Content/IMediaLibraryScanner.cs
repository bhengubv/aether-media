// SPDX-License-Identifier: MIT

using Aether.Media.Core.Models;

namespace Aether.Media.Content;

/// <summary>
/// Walks the local filesystem for supported media files, extracts metadata via
/// TagLibSharp, hashes content, and publishes descriptors to the Aether content layer.
/// </summary>
public interface IMediaLibraryScanner
{
    /// <summary>
    /// Scan a directory for supported audio/video files and return the resulting
    /// <see cref="MediaContent"/> records. Fires <see cref="ContentDiscovered"/> once
    /// per successfully resolved file.
    /// </summary>
    Task<IReadOnlyList<MediaContent>> ScanDirectoryAsync(
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
