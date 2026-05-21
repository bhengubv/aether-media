// SPDX-License-Identifier: MIT

namespace Aether.Media.Reel.Interfaces;

/// <summary>
/// Persists and queries on-device engagement signals for the For You algorithm.
///
/// All data stored by this interface is strictly local — it is never transmitted
/// to any server or peer. It is the user's private taste profile.
///
/// Affinity scores are derived values: the tracker aggregates raw signals across
/// all Reels to compute per-hashtag and per-creator affinities, which the
/// <c>IReelFeed</c> scorer uses to personalise results.
/// </summary>
public interface IReelEngagementTracker
{
    // ── Recording ────────────────────────────────────────────────────────────

    /// <summary>
    /// Upserts the engagement signal for a Reel. If a signal already exists for
    /// <see cref="ReelEngagementSignal.ReelHash"/>, it is merged (watch time
    /// accumulated, replay count incremented, boolean flags OR-merged).
    /// </summary>
    Task RecordAsync(ReelEngagementSignal signal, CancellationToken ct = default);

    // ── Retrieval ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the accumulated engagement signal for a specific Reel, or
    /// <c>null</c> if the Reel has never been seen.
    /// </summary>
    Task<ReelEngagementSignal?> GetAsync(string reelHash, CancellationToken ct = default);

    /// <summary>Returns all stored engagement signals.</summary>
    Task<IReadOnlyList<ReelEngagementSignal>> GetAllAsync(CancellationToken ct = default);

    // ── Affinity computation ──────────────────────────────────────────────────

    /// <summary>
    /// Returns a map of hashtag → affinity score in the range [0, 1].
    ///
    /// Affinity is the weighted average of completion ratios across all Reels
    /// tagged with that hashtag, boosted by likes and shares, penalised by skips.
    /// </summary>
    Task<IReadOnlyDictionary<string, float>> GetHashtagAffinitiesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Returns a map of creator UHID → affinity score in the range [0, 1].
    ///
    /// Computed the same way as hashtag affinities but keyed on creator.
    /// </summary>
    Task<IReadOnlyDictionary<string, float>> GetCreatorAffinitiesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Returns the set of hashtag clusters seen in the last
    /// <paramref name="windowMs"/> milliseconds. Used by the novelty bonus
    /// calculation to identify over-represented clusters.
    /// </summary>
    Task<IReadOnlySet<string>> GetRecentHashtagsAsync(
        long windowMs = 3_600_000,   // 1 hour default
        CancellationToken ct = default);

    // ── Management ───────────────────────────────────────────────────────────

    /// <summary>
    /// Clears all stored engagement data. Resets the user's personalisation
    /// profile to zero.
    /// </summary>
    Task ResetAsync(CancellationToken ct = default);
}
