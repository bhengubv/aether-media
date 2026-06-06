// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Models;

/// <summary>
/// Criteria for a smart collection.  All non-null fields must match (AND logic).
/// Null fields are ignored.
/// </summary>
public sealed record SmartCollectionFilter
{
    /// <summary>Match content whose genre list contains this value (case-insensitive).</summary>
    public string? Genre { get; init; }

    /// <summary>Earliest year to include (inclusive).</summary>
    public int? YearFrom { get; init; }

    /// <summary>Latest year to include (inclusive).</summary>
    public int? YearTo { get; init; }

    /// <summary>Match audio content by artist name (case-insensitive, partial match).</summary>
    public string? Artist { get; init; }

    /// <summary>Minimum rating (0–10 scale).  Content must have rating ≥ this value.</summary>
    public float? MinRating { get; init; }

    /// <summary>Maximum duration in milliseconds.  Content must be shorter than this.</summary>
    public long? MaxDurationMs { get; init; }

    /// <summary>
    /// When <c>true</c>, only watched items are returned.
    /// When <c>false</c>, only unwatched items are returned.
    /// When <c>null</c>, both are included.
    /// </summary>
    public bool? Watched { get; init; }

    /// <summary>Content must include ALL of these tags (case-insensitive).</summary>
    public string[] RequiredTags { get; init; } = [];
}
