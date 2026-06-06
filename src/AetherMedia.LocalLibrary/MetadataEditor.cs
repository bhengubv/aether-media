// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Interfaces;
using AetherMedia.LocalLibrary.Models;
using Microsoft.Extensions.Logging;
using TagLib;

namespace AetherMedia.LocalLibrary;

/// <summary>
/// TagLibSharp-backed implementation of <see cref="IMetadataEditor"/>.
/// All file I/O is marshalled to the thread-pool via <see cref="Task.Run"/> so the
/// calling thread (Avalonia UI thread) is never blocked.
/// </summary>
public sealed class MetadataEditor : IMetadataEditor
{
    private readonly ILogger<MetadataEditor> _logger;

    // Extensions supported by TagLibSharp that make sense for music tagging
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".flac", ".m4a", ".aac", ".ogg", ".oga", ".opus",
        ".wma", ".wav", ".aiff", ".aif", ".ape", ".mpc", ".wv",
        ".m4b", ".m4p", ".mp4"   // AAC container variants
    };

    public MetadataEditor(ILogger<MetadataEditor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public bool CanEdit(string filePath) =>
        SupportedExtensions.Contains(Path.GetExtension(filePath));

    /// <inheritdoc/>
    public Task<TrackMetadata?> ReadAsync(string filePath, CancellationToken ct = default) =>
        Task.Run(() => ReadCore(filePath), ct);

    /// <inheritdoc/>
    public Task WriteAsync(TrackMetadata metadata, CancellationToken ct = default) =>
        Task.Run(() => WriteCore(metadata), ct);

    // ── Core synchronous helpers (run on thread-pool) ──────────────────────

    private TrackMetadata? ReadCore(string filePath)
    {
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogWarning("MetadataEditor.ReadAsync: file not found: {Path}", filePath);
            return null;
        }

        try
        {
            using var file = TagLib.File.Create(filePath);
            var tag = file.Tag;

            byte[]? coverBytes   = null;
            string? coverMime    = null;

            // TagLib stores pictures in an array; take the first FrontCover or just [0]
            var pictures = tag.Pictures;
            if (pictures.Length > 0)
            {
                var pic      = pictures.FirstOrDefault(p => p.Type == PictureType.FrontCover)
                               ?? pictures[0];
                coverBytes   = pic.Data.ToArray();
                coverMime    = pic.MimeType;
            }

            return new TrackMetadata
            {
                FilePath        = filePath,
                Title           = tag.Title        ?? string.Empty,
                Artist          = tag.FirstPerformer ?? string.Empty,
                AlbumArtist     = tag.FirstAlbumArtist ?? string.Empty,
                Album           = tag.Album        ?? string.Empty,
                Track           = tag.Track,
                TrackCount      = tag.TrackCount,
                Disc            = tag.Disc,
                DiscCount       = tag.DiscCount,
                Year            = tag.Year,
                Comment         = tag.Comment      ?? string.Empty,
                Genres          = tag.Genres       ?? [],
                Rating          = NormaliseRating(tag),
                DurationMs      = (long)file.Properties.Duration.TotalMilliseconds,
                CoverArt        = coverBytes,
                CoverArtMimeType = coverMime
            };
        }
        catch (UnsupportedFormatException)
        {
            _logger.LogDebug("MetadataEditor: unsupported format for {Path}", filePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MetadataEditor.ReadAsync failed for {Path}", filePath);
            return null;
        }
    }

    private void WriteCore(TrackMetadata metadata)
    {
        try
        {
            using var file = TagLib.File.Create(metadata.FilePath);
            var tag = file.Tag;

            tag.Title           = metadata.Title;
            tag.Performers      = string.IsNullOrEmpty(metadata.Artist)
                                  ? [] : [metadata.Artist];
            tag.AlbumArtists    = string.IsNullOrEmpty(metadata.AlbumArtist)
                                  ? [] : [metadata.AlbumArtist];
            tag.Album           = metadata.Album;
            tag.Track           = metadata.Track;
            tag.TrackCount      = metadata.TrackCount;
            tag.Disc            = metadata.Disc;
            tag.DiscCount       = metadata.DiscCount;
            tag.Year            = metadata.Year;
            tag.Comment         = metadata.Comment;
            tag.Genres          = metadata.Genres;

            // Cover art
            if (metadata.CoverArt is { Length: > 0 } coverBytes)
            {
                var byteVector = new ByteVector(coverBytes);
                tag.Pictures =
                [
                    new Picture(byteVector)
                    {
                        Type     = PictureType.FrontCover,
                        MimeType = metadata.CoverArtMimeType ?? "image/jpeg",
                        Description = "Cover"
                    }
                ];
            }

            file.Save();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MetadataEditor.WriteAsync failed for {Path}", metadata.FilePath);
            throw;
        }
    }

    // ── Rating normalisation ───────────────────────────────────────────────

    /// <summary>
    /// TagLib exposes no standard rating field, but ID3v2 POPM frames and other formats
    /// store a 0–255 value.  We expose a 0–5 scale.
    /// TagLib doesn't surface POPM directly in the base Tag — fall back to 0.
    /// </summary>
    private static uint NormaliseRating(Tag tag)
    {
        // For ID3v2 tags we can check for a custom TXXX or POPM frame via the Id3v2.Tag subclass.
        if (tag is TagLib.Id3v2.Tag id3v2)
        {
            var popm = id3v2.GetFrames<TagLib.Id3v2.PopularimeterFrame>().FirstOrDefault();
            if (popm is not null && popm.Rating > 0)
            {
                // POPM: 1=20, 64=40, 128=60(?), 196=80(?), 255=100 — map 0-255 → 0-5
                return (uint)Math.Round(popm.Rating / 255.0 * 5.0);
            }
        }
        return 0;
    }
}
