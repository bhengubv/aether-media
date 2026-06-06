// SPDX-License-Identifier: MIT

namespace AetherMesh.Media.Reel;

/// <summary>
/// A hashtag that is currently trending on the local peer cluster.
///
/// Counts are gossipped aggregates — they represent how many times the tag has
/// appeared in Reels seen by this node and its direct peers in the past 24 hours.
/// No individual viewing data is included.
/// </summary>
public sealed record TrendingHashtag(
    /// <summary>The hashtag without the '#' prefix, lower-case.</summary>
    string Tag,

    /// <summary>Approximate number of Reel appearances in the last 24 hours.</summary>
    long Count24h,

    /// <summary>
    /// Growth velocity — ratio of Count24h to the previous 24-hour window.
    /// Values > 1.0 indicate acceleration; values &lt; 1.0 indicate decline.
    /// </summary>
    float Velocity
);
