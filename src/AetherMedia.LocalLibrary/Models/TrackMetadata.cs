// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Models;

/// <summary>
/// All tag fields for a music file.  Read via <see cref="Interfaces.IMetadataEditor.ReadAsync"/>
/// and written back via <see cref="Interfaces.IMetadataEditor.WriteAsync"/>.
/// </summary>
public sealed class TrackMetadata
{
    /// <summary>Absolute path to the source file.  Read-only after construction.</summary>
    public required string FilePath { get; init; }

    public string Title        { get; set; } = string.Empty;
    public string Artist       { get; set; } = string.Empty;
    public string AlbumArtist  { get; set; } = string.Empty;
    public string Album        { get; set; } = string.Empty;
    public uint   Track        { get; set; }
    public uint   TrackCount   { get; set; }
    public uint   Disc         { get; set; }
    public uint   DiscCount    { get; set; }
    public uint   Year         { get; set; }
    public string Comment      { get; set; } = string.Empty;

    /// <summary>Genre names (may be multiple).</summary>
    public string[] Genres { get; set; } = [];

    /// <summary>
    /// Rating 0–5 (maps from tag values: 0 = unrated, 1–5 stars).
    /// Different formats store different raw scales — the editor normalises to 0-5.
    /// </summary>
    public uint Rating { get; set; }

    /// <summary>Duration in milliseconds.  Written by the editor; ignored on WriteAsync.</summary>
    public long DurationMs { get; set; }

    /// <summary>Raw JPEG/PNG bytes of the embedded cover art, or <c>null</c> if none.</summary>
    public byte[]? CoverArt { get; set; }

    /// <summary>MIME type of <see cref="CoverArt"/> (e.g. <c>"image/jpeg"</c>).</summary>
    public string? CoverArtMimeType { get; set; }
}
