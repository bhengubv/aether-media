// SPDX-License-Identifier: MIT

namespace AetherMedia.Reel;

/// <summary>
/// All engagement data the device has recorded for a single Reel.
///
/// These signals are stored exclusively on the local device — they are NEVER sent
/// to any server, peer, or gossip channel. They feed the on-device For You algorithm
/// only.
///
/// Affinity scores (hashtag, creator) are derived from the aggregate of these
/// signals by <c>IReelEngagementTracker</c>.
/// </summary>
public sealed record ReelEngagementSignal(
    /// <summary>Content hash of the Reel these signals relate to.</summary>
    string ReelHash,

    /// <summary>Total milliseconds actually watched across all views.</summary>
    long WatchedMs,

    /// <summary>Duration of the Reel in ms — used to compute completion ratio.</summary>
    long DurationMs,

    /// <summary>Number of times the Reel was replayed from the beginning.</summary>
    int ReplayCount,

    /// <summary>Whether the user has liked this Reel.</summary>
    bool Liked,

    /// <summary>Whether the user has shared this Reel.</summary>
    bool Shared,

    /// <summary>
    /// Whether the user swiped away within the first 20 % of the Reel's duration —
    /// treated as a strong negative signal by the ranker.
    /// </summary>
    bool Skipped,

    /// <summary>UTC time of the most recent watch event.</summary>
    DateTimeOffset LastWatchedAt
)
{
    /// <summary>
    /// Ratio of watched time to total duration, clamped to [0, 1].
    /// This is the single most important input to the For You algorithm.
    /// </summary>
    public float CompletionRatio =>
        DurationMs <= 0 ? 0f : Math.Clamp((float)WatchedMs / DurationMs, 0f, 1f);
}
