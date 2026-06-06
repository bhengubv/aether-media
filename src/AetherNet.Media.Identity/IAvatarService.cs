// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Identity;

/// <summary>
/// Chunked avatar distribution using the Aether content layer.
/// Avatars are published as named content and fetched on demand; results are
/// cached in-process to avoid redundant network round-trips.
/// </summary>
public interface IAvatarService
{
    /// <summary>
    /// Publish raw image bytes as chunked content and return the content hash
    /// (the descriptor's <c>RootHash</c>) for use in <see cref="MediaProfile.AvatarHash"/>.
    /// </summary>
    Task<string> PublishAvatarAsync(byte[] imageBytes, string mimeType, CancellationToken ct = default);

    /// <summary>
    /// Fetch and reassemble avatar bytes by content hash. Returns null when the
    /// content is not yet available locally and cannot be fetched from the network.
    /// </summary>
    Task<byte[]?> FetchAvatarAsync(string contentHash, string ownerUhid, CancellationToken ct = default);

    /// <summary>Returns the content hash of the local node's own avatar, or null if none has been published.</summary>
    Task<string?> GetLocalAvatarHashAsync(CancellationToken ct = default);
}
