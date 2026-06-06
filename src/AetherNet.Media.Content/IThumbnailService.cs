// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Content;

/// <summary>
/// Extracts embedded artwork from media files and distributes it as chunked content
/// over the Aether network. Returns content hashes suitable for storage in
/// <see cref="Core.Models.MediaContent.ThumbnailHash"/>.
/// </summary>
public interface IThumbnailService
{
    /// <summary>
    /// Extract the first embedded picture from <paramref name="filePath"/>, publish it as
    /// chunked content attributed to <paramref name="creatorUhid"/>, and return the
    /// content hash. Returns null when the file contains no embedded artwork.
    /// </summary>
    Task<string?> ExtractAndPublishAsync(
        string filePath,
        string creatorUhid,
        CancellationToken ct = default);

    /// <summary>
    /// Fetch and reassemble thumbnail bytes by content hash. Returns null when the
    /// content is not locally available and cannot be fetched from the network.
    /// </summary>
    Task<byte[]?> FetchAsync(string thumbnailHash, string ownerUhid, CancellationToken ct = default);
}
