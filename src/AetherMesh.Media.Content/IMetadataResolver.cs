// SPDX-License-Identifier: MIT

using AetherMesh.Media.Core.Models;

namespace AetherMesh.Media.Content;

/// <summary>
/// Resolves a <see cref="MediaContent"/> record from a file on disk using tag
/// metadata and a pre-computed content hash. Implementations read embedded tags
/// (title, duration, genre, performer) via TagLibSharp and produce a fully-populated
/// record ready for storage and announcement.
/// </summary>
public interface IMetadataResolver
{
    /// <summary>
    /// Build a <see cref="MediaContent"/> record for <paramref name="filePath"/>.
    /// Returns null when the file cannot be opened or is in an unsupported format.
    /// </summary>
    Task<MediaContent?> ResolveAsync(
        string filePath,
        string contentHash,
        string creatorUhid,
        CancellationToken ct = default);
}
