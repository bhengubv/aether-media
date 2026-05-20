// SPDX-License-Identifier: MIT

using Aether.Media.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TagLib;

namespace Aether.Media.Content;

/// <summary>
/// Reads embedded ID3 / Vorbis / MP4 tags from a media file using TagLibSharp
/// and maps them into a <see cref="MediaContent"/> record.
/// </summary>
public sealed class MetadataResolver : IMetadataResolver
{
    private static readonly IReadOnlyDictionary<string, string> MimeMap
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".mp4"]  = "video/mp4",
            [".mkv"]  = "video/x-matroska",
            [".avi"]  = "video/x-msvideo",
            [".mov"]  = "video/quicktime",
            [".webm"] = "video/webm",
            [".m4v"]  = "video/x-m4v",
            [".mp3"]  = "audio/mpeg",
            [".flac"] = "audio/flac",
            [".aac"]  = "audio/aac",
            [".ogg"]  = "audio/ogg",
            [".wav"]  = "audio/wav",
            [".m4a"]  = "audio/mp4",
        };

    private static readonly IReadOnlyDictionary<string, string> CodecMap
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".mp4"]  = "h264",
            [".mkv"]  = "h264",
            [".avi"]  = "mpeg4",
            [".mov"]  = "h264",
            [".webm"] = "vp9",
            [".m4v"]  = "h264",
            [".mp3"]  = "mp3",
            [".flac"] = "flac",
            [".aac"]  = "aac",
            [".ogg"]  = "vorbis",
            [".wav"]  = "pcm",
            [".m4a"]  = "aac",
        };

    private readonly ILogger<MetadataResolver> _logger;

    public MetadataResolver(ILogger<MetadataResolver>? logger = null)
    {
        _logger = logger ?? NullLogger<MetadataResolver>.Instance;
    }

    /// <inheritdoc/>
    public Task<MediaContent?> ResolveAsync(
        string filePath,
        string contentHash,
        string creatorUhid,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(creatorUhid);

        var ext = Path.GetExtension(filePath);

        if (!MimeMap.TryGetValue(ext, out var contentType))
        {
            _logger.LogDebug("MetadataResolver: unsupported extension '{Ext}' for {Path}", ext, filePath);
            return Task.FromResult<MediaContent?>(null);
        }

        TagLib.File? tagFile = null;
        try
        {
            tagFile = TagLib.File.Create(filePath);

            var tag = tagFile.Tag;
            var fileInfo = new FileInfo(filePath);

            // Prefer the embedded title; fall back to the file name without extension.
            var title = !string.IsNullOrWhiteSpace(tag.Title)
                ? tag.Title.Trim()
                : Path.GetFileNameWithoutExtension(filePath);

            var durationMs = (long)tagFile.Properties.Duration.TotalMilliseconds;

            CodecMap.TryGetValue(ext, out var codec);
            codec ??= ext.TrimStart('.').ToUpperInvariant();

            // Attempt to read a more specific codec from TagLib properties.
            if (tagFile.Properties is { } props)
            {
                var codecDesc = props.Description;
                if (!string.IsNullOrWhiteSpace(codecDesc))
                    codec = codecDesc.Trim();
            }

            // Flatten genres and performers into the tags list.
            var tags = new List<string>();
            if (tag.Genres is { Length: > 0 })
                tags.AddRange(tag.Genres.Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g.Trim()));
            if (tag.Performers is { Length: > 0 })
                tags.AddRange(tag.Performers.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()));

            var content = new MediaContent(
                ContentHash: contentHash,
                Title: title,
                DurationMs: durationMs,
                Codec: codec,
                ContentType: contentType,
                CreatorUhid: creatorUhid,
                SizeBytes: fileInfo.Length,
                CreatedAt: fileInfo.CreationTimeUtc,
                ThumbnailHash: null,       // populated separately by IThumbnailService
                Tags: tags.AsReadOnly());

            return Task.FromResult<MediaContent?>(content);
        }
        catch (UnsupportedFormatException ex)
        {
            _logger.LogWarning(ex, "MetadataResolver: TagLib cannot read '{Path}'", filePath);
            return Task.FromResult<MediaContent?>(null);
        }
        catch (CorruptFileException ex)
        {
            _logger.LogWarning(ex, "MetadataResolver: corrupt file '{Path}'", filePath);
            return Task.FromResult<MediaContent?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MetadataResolver: unexpected error reading '{Path}'", filePath);
            return Task.FromResult<MediaContent?>(null);
        }
        finally
        {
            tagFile?.Dispose();
        }
    }
}
