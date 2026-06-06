// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using AetherMesh.Content;
using AetherMesh.Media.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherMesh.Media.Content;

/// <summary>
/// Walks local filesystem directories, computes a SHA-256 content hash per file,
/// resolves metadata via <see cref="IMetadataResolver"/>, extracts thumbnail artwork
/// via <see cref="IThumbnailService"/>, and publishes descriptors to the Aether
/// content layer. Already-known content (identified by root hash) is skipped.
/// </summary>
public sealed class MediaLibraryScanner : IMediaLibraryScanner
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".avi", ".mov", ".webm",
        ".mp3", ".flac", ".aac", ".ogg", ".wav", ".m4a", ".m4v",
    };

    private readonly IMetadataResolver _resolver;
    private readonly IThumbnailService _thumbnails;
    private readonly IContentService _content;
    private readonly string _localUhid;
    private readonly ILogger<MediaLibraryScanner> _logger;

    public event EventHandler<MediaContent>? ContentDiscovered;

    /// <param name="resolver">Metadata resolver used per-file.</param>
    /// <param name="thumbnails">Thumbnail extractor used per-file.</param>
    /// <param name="content">Aether content service for publishing and existence checks.</param>
    /// <param name="localUhid">UHID of the local node; set as <see cref="MediaContent.CreatorUhid"/>.</param>
    /// <param name="logger">Optional logger.</param>
    public MediaLibraryScanner(
        IMetadataResolver resolver,
        IThumbnailService thumbnails,
        IContentService content,
        string localUhid,
        ILogger<MediaLibraryScanner>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localUhid);
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _thumbnails = thumbnails ?? throw new ArgumentNullException(nameof(thumbnails));
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _localUhid = localUhid;
        _logger = logger ?? NullLogger<MediaLibraryScanner>.Instance;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ScannedMediaItem>> ScanDirectoryAsync(
        string path,
        bool recursive,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Media library path not found: '{path}'");

        var option = recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        var results = new List<ScannedMediaItem>();

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(path, "*", option);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "ScanDirectory: access denied on '{Path}'", path);
            return results;
        }

        foreach (var filePath in files)
        {
            ct.ThrowIfCancellationRequested();

            var content = await ScanFileAsync(filePath, ct).ConfigureAwait(false);
            if (content is not null)
                results.Add(new ScannedMediaItem(content, filePath));
        }

        _logger.LogInformation("ScanDirectory complete: {Count} items found in '{Path}'", results.Count, path);
        return results;
    }

    /// <inheritdoc/>
    public async Task<MediaContent?> ScanFileAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var ext = Path.GetExtension(filePath);
        if (!SupportedExtensions.Contains(ext))
            return null;

        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogDebug("ScanFile: file not found '{Path}'", filePath);
            return null;
        }

        // Compute SHA-256 of the raw file bytes.
        string contentHash;
        byte[] fileBytes;
        try
        {
            fileBytes = await System.IO.File.ReadAllBytesAsync(filePath, ct).ConfigureAwait(false);
            contentHash = Convert.ToHexString(SHA256.HashData(fileBytes)).ToLowerInvariant();
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "ScanFile: could not read '{Path}'", filePath);
            return null;
        }

        // Skip files already published to the content layer.
        var existing = await _content.AssembleAsync(contentHash, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            _logger.LogDebug("ScanFile: '{Path}' already known (root={Root}), skipping", filePath, contentHash);
            return null;
        }

        // Resolve metadata.
        var mediaContent = await _resolver.ResolveAsync(filePath, contentHash, _localUhid, ct)
            .ConfigureAwait(false);

        if (mediaContent is null)
        {
            _logger.LogDebug("ScanFile: metadata resolution returned null for '{Path}'", filePath);
            return null;
        }

        // Extract and publish thumbnail; wire the hash into a new MediaContent record.
        string? thumbnailHash = null;
        try
        {
            thumbnailHash = await _thumbnails.ExtractAndPublishAsync(filePath, _localUhid, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ScanFile: thumbnail extraction failed for '{Path}'", filePath);
        }

        if (thumbnailHash is not null)
        {
            mediaContent = mediaContent with { ThumbnailHash = thumbnailHash };
        }

        // Publish the full media file to the content layer.
        try
        {
            var descriptor = await _content.PublishAsync(
                name: Path.GetFileName(filePath),
                data: fileBytes,
                contentType: mediaContent.ContentType,
                cancellationToken: ct).ConfigureAwait(false);

            await _content.AnnounceAsync(descriptor, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ScanFile: content publish failed for '{Path}'", filePath);
            return null;
        }

        _logger.LogInformation("ScanFile: discovered '{Title}' ({Hash})", mediaContent.Title, contentHash);
        ContentDiscovered?.Invoke(this, mediaContent);
        return mediaContent;
    }
}
