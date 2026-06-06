// SPDX-License-Identifier: MIT

namespace AetherMesh.Media.Reel;

/// <summary>
/// A single entry in a ranked Reel feed — pairs the <see cref="Reel"/> descriptor
/// with the on-device score that placed it in this position and the viewer's current
/// interaction state.
/// </summary>
public sealed record ReelFeedItem(
    /// <summary>The Reel descriptor.</summary>
    Reel Reel,

    /// <summary>
    /// Rank score computed by <c>IReelFeed</c> — higher is better. Exposed so the
    /// UI can show the algorithm debug view ("why am I seeing this?").
    /// </summary>
    float Score,

    /// <summary>Whether the viewing device has liked this Reel.</summary>
    bool IsLiked,

    /// <summary>Whether the viewing device has bookmarked this Reel.</summary>
    bool IsBookmarked
);
