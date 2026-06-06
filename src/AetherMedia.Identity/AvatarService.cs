// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using AetherNet.Content;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherMedia.Identity;

/// <summary>
/// Distributes avatar images over the Aether content layer (chunked P2P transfer).
/// Published avatars are stored as <c>IContentService</c> entries; the returned
/// root hash is recorded in <see cref="Core.Models.MediaProfile.AvatarHash"/>.
/// Fetched bytes are cached in-process to avoid redundant network round-trips.
/// </summary>
public sealed class AvatarService : IAvatarService
{
    private readonly IContentService _content;
    private readonly ILogger<AvatarService> _logger;

    // In-memory cache: contentHash → raw image bytes.
    private readonly ConcurrentDictionary<string, byte[]> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    // The root hash of the local node's published avatar (null until first publish).
    private string? _localAvatarHash;

    /// <param name="content">Chunked content distribution service from AetherNet.Content.</param>
    /// <param name="logger">Optional logger.</param>
    public AvatarService(
        IContentService content,
        ILogger<AvatarService>? logger = null)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _logger = logger ?? NullLogger<AvatarService>.Instance;
    }

    /// <inheritdoc/>
    public async Task<string> PublishAvatarAsync(
        byte[] imageBytes,
        string mimeType,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);

        if (imageBytes.Length == 0)
            throw new ArgumentException("Avatar image bytes must not be empty.", nameof(imageBytes));

        // Normalise MIME type to a safe content-type string.
        var contentType = mimeType.Trim();

        var descriptor = await _content.PublishAsync(
            name: "avatar",
            data: imageBytes,
            contentType: contentType,
            cancellationToken: ct).ConfigureAwait(false);

        _localAvatarHash = descriptor.RootHash;

        // Populate the local cache so self-fetches are free.
        _cache[descriptor.RootHash] = imageBytes;

        // Announce to the mesh so peers can request chunks.
        await _content.AnnounceAsync(descriptor, ct).ConfigureAwait(false);

        _logger.LogInformation("Avatar published root={Root} bytes={Bytes} type={Type}",
            descriptor.RootHash, imageBytes.Length, contentType);

        return descriptor.RootHash;
    }

    /// <inheritdoc/>
    public async Task<byte[]?> FetchAvatarAsync(
        string contentHash,
        string ownerUhid,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerUhid);

        // Return cached bytes if we already have them.
        if (_cache.TryGetValue(contentHash, out var cached))
            return cached;

        // Request all chunks from the owner peer (falls back to broadcast if no route).
        await _content.RequestChunksAsync(
            rootHash: contentHash,
            chunkIndices: Array.Empty<int>(),   // empty = request all chunks
            peerUhid: ownerUhid,
            cancellationToken: ct).ConfigureAwait(false);

        // Attempt to reassemble from locally stored chunks.
        var assembled = await _content.AssembleAsync(contentHash, ct).ConfigureAwait(false);

        if (assembled is not null)
        {
            _cache[contentHash] = assembled;
            _logger.LogDebug("Avatar fetched and cached root={Root} bytes={Bytes}", contentHash, assembled.Length);
        }
        else
        {
            _logger.LogDebug("Avatar root={Root} not yet fully assembled after fetch attempt", contentHash);
        }

        return assembled;
    }

    /// <inheritdoc/>
    public Task<string?> GetLocalAvatarHashAsync(CancellationToken ct = default)
        => Task.FromResult(_localAvatarHash);
}
