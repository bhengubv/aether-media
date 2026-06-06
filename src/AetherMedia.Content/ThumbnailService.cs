// SPDX-License-Identifier: MIT

using AetherNet.Content;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TagLib;

namespace AetherMedia.Content;

/// <summary>
/// Extracts embedded cover artwork from media files using TagLibSharp and distributes
/// the bytes as chunked content via <see cref="IContentService"/>. The returned content
/// hash is stored in <see cref="Core.Models.MediaContent.ThumbnailHash"/>.
/// </summary>
public sealed class ThumbnailService : IThumbnailService
{
    private readonly IContentService _content;
    private readonly ILogger<ThumbnailService> _logger;

    public ThumbnailService(
        IContentService content,
        ILogger<ThumbnailService>? logger = null)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _logger = logger ?? NullLogger<ThumbnailService>.Instance;
    }

    /// <inheritdoc/>
    public async Task<string?> ExtractAndPublishAsync(
        string filePath,
        string creatorUhid,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(creatorUhid);

        TagLib.File? tagFile = null;
        byte[]? artworkBytes = null;
        string mimeType = "image/jpeg";

        try
        {
            tagFile = TagLib.File.Create(filePath);

            var pictures = tagFile.Tag.Pictures;
            if (pictures is null || pictures.Length == 0)
            {
                _logger.LogDebug("ThumbnailService: no embedded artwork in '{Path}'", filePath);
                return null;
            }

            var picture = pictures[0];
            var raw = picture.Data?.Data;

            if (raw is null || raw.Length == 0)
            {
                _logger.LogDebug("ThumbnailService: embedded artwork is empty in '{Path}'", filePath);
                return null;
            }

            artworkBytes = raw;

            // Use the picture's declared MIME type when it is not empty.
            if (!string.IsNullOrWhiteSpace(picture.MimeType))
                mimeType = picture.MimeType.Trim();
        }
        catch (UnsupportedFormatException ex)
        {
            _logger.LogWarning(ex, "ThumbnailService: TagLib cannot read '{Path}'", filePath);
            return null;
        }
        catch (CorruptFileException ex)
        {
            _logger.LogWarning(ex, "ThumbnailService: corrupt file '{Path}'", filePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ThumbnailService: unexpected error reading '{Path}'", filePath);
            return null;
        }
        finally
        {
            tagFile?.Dispose();
        }

        // Publish the artwork bytes as chunked content.
        var thumbnailName = $"thumb_{Path.GetFileNameWithoutExtension(filePath)}";
        var descriptor = await _content.PublishAsync(
            name: thumbnailName,
            data: artworkBytes,
            contentType: mimeType,
            cancellationToken: ct).ConfigureAwait(false);

        await _content.AnnounceAsync(descriptor, ct).ConfigureAwait(false);

        _logger.LogDebug("ThumbnailService: published artwork root={Root} bytes={Bytes} type={Type}",
            descriptor.RootHash, artworkBytes.Length, mimeType);

        return descriptor.RootHash;
    }

    /// <inheritdoc/>
    public async Task<byte[]?> FetchAsync(
        string thumbnailHash,
        string ownerUhid,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbnailHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerUhid);

        // Try to reassemble from locally stored chunks first.
        var local = await _content.AssembleAsync(thumbnailHash, ct).ConfigureAwait(false);
        if (local is not null)
            return local;

        // Request chunks from the owning peer (falls back to broadcast if no route).
        await _content.RequestChunksAsync(
            rootHash: thumbnailHash,
            chunkIndices: Array.Empty<int>(),   // empty = request all chunks
            peerUhid: ownerUhid,
            cancellationToken: ct).ConfigureAwait(false);

        var assembled = await _content.AssembleAsync(thumbnailHash, ct).ConfigureAwait(false);

        if (assembled is null)
            _logger.LogDebug("ThumbnailService: thumbnail root={Root} not yet fully available", thumbnailHash);

        return assembled;
    }
}
