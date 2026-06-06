// SPDX-License-Identifier: MIT

namespace AetherMesh.Media.Reel;

/// <summary>
/// Immutable descriptor for a single Reel — a short-form video published to the
/// Aether mesh. Content is identified by <see cref="ContentHash"/> (SHA-256 of the
/// video bytes), which is also the key used to fetch chunks via
/// <c>IContentService</c>.
///
/// Maximum duration is enforced at 60 000 ms by <c>ReelService</c> before the
/// record is created. The model is mesh-safe: it contains no file paths, no device
/// identifiers beyond <see cref="CreatorUhid"/>, and no personal engagement data.
/// View/like counts are gossipped aggregates — approximate and untraceable.
/// </summary>
/// <param name="ContentHash">SHA-256 of the raw video bytes — primary key.</param>
/// <param name="CreatorUhid">UHID of the creator node.</param>
/// <param name="Title">Display title. May be empty but never null.</param>
/// <param name="DurationMs">Duration in milliseconds (max 60 000).</param>
/// <param name="SoundHash">Content hash of the background audio track, or null for original audio.</param>
/// <param name="SoundTitle">Human-readable sound name. Null when no sound.</param>
/// <param name="Hashtags">Hashtags without the '#' prefix, lower-case.</param>
/// <param name="Type">Original, Duet, or Stitch.</param>
/// <param name="SourceReelHash">Source Reel hash for Duet/Stitch. Null for Original.</param>
/// <param name="ThumbnailHash">Content hash of the thumbnail chunk, or null.</param>
/// <param name="CreatedAtMs">Unix millisecond timestamp when published.</param>
/// <param name="ViewCount">Gossipped aggregate view count.</param>
/// <param name="LikeCount">Gossipped aggregate like count.</param>
public sealed record Reel(
    string         ContentHash,
    string         CreatorUhid,
    string         Title,
    long           DurationMs,
    string?        SoundHash,
    string?        SoundTitle,
    string[]       Hashtags,
    ReelType       Type,
    string?        SourceReelHash,
    string?        ThumbnailHash,
    long           CreatedAtMs,
    long           ViewCount,
    long           LikeCount)
{
    /// <summary>
    /// Hashtags, guaranteed non-null. Defaults to empty array when the positional
    /// parameter is null (safe for JSON deserialisation).
    /// </summary>
    public string[] Hashtags { get; init; } = Hashtags ?? [];
}
