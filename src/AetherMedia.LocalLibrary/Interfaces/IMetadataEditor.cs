// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Models;

namespace AetherMedia.LocalLibrary.Interfaces;

/// <summary>
/// Reads and writes embedded tag metadata for audio files
/// (MP3 / FLAC / AAC / OGG / WMA / ALAC / AIFF).
/// Backed by TagLibSharp.
/// </summary>
public interface IMetadataEditor
{
    /// <summary>
    /// Returns <c>true</c> if the file at <paramref name="filePath"/> can be opened
    /// by TagLibSharp (based on extension and magic bytes).
    /// </summary>
    bool CanEdit(string filePath);

    /// <summary>
    /// Reads all tag fields from the file.
    /// Returns <c>null</c> if the format is unsupported or the file does not exist.
    /// </summary>
    Task<TrackMetadata?> ReadAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Writes the supplied tag fields back to the file.
    /// Only fields that differ from the current file tags are updated;
    /// <see cref="TrackMetadata.DurationMs"/> is ignored (read-only property of the container).
    /// </summary>
    Task WriteAsync(TrackMetadata metadata, CancellationToken ct = default);
}
