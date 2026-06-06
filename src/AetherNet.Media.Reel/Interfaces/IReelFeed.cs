// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Reel.Interfaces;

/// <summary>
/// On-device For You feed — scores and ranks Reels using local engagement signals
/// only. No data leaves the device.
///
/// The algorithm is fully transparent: callers can read <see cref="Weights"/> to see
/// exactly how scoring works, adjust them, and reset to defaults at any time.
/// </summary>
public interface IReelFeed
{
    // ── Feed retrieval ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns a ranked list of Reels for the For You feed, scored using the
    /// on-device algorithm. Includes content from both followed creators and
    /// mesh-discovered strangers.
    /// </summary>
    Task<IReadOnlyList<ReelFeedItem>> GetForYouAsync(
        int count = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Returns Reels from creators the local node follows, ordered by recency
    /// then by engagement score.
    /// </summary>
    Task<IReadOnlyList<ReelFeedItem>> GetFollowingAsync(
        int count = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Returns Reels tagged with <paramref name="hashtag"/>, ranked by score.
    /// </summary>
    Task<IReadOnlyList<ReelFeedItem>> GetByHashtagAsync(
        string hashtag,
        int    count = 20,
        CancellationToken ct = default);

    /// <summary>Returns Reels from a specific creator, newest first.</summary>
    Task<IReadOnlyList<ReelFeedItem>> GetByCreatorAsync(
        string creatorUhid,
        int    count = 20,
        CancellationToken ct = default);

    /// <summary>Returns Reels the local device has bookmarked.</summary>
    Task<IReadOnlyList<ReelFeedItem>> GetBookmarksAsync(
        int count = 50,
        CancellationToken ct = default);

    // ── Engagement recording ────────────────────────────────────────────────

    /// <summary>
    /// Records a watch event. Call when the Reel exits the viewport or playback ends.
    /// </summary>
    /// <param name="contentHash">The Reel that was watched.</param>
    /// <param name="watchedMs">Milliseconds actually played (not total duration).</param>
    /// <param name="replayed"><c>true</c> if the user replayed from the beginning.</param>
    Task RecordWatchAsync(
        string contentHash,
        long   watchedMs,
        bool   replayed,
        CancellationToken ct = default);

    /// <summary>
    /// Records a skip — the user swiped away within the first 20 % of the Reel.
    /// Strong negative signal.
    /// </summary>
    Task RecordSkipAsync(string contentHash, CancellationToken ct = default);

    /// <summary>Records a share. Strongest positive signal after completion rate.</summary>
    Task RecordShareAsync(string contentHash, CancellationToken ct = default);

    // ── Algorithm transparency ──────────────────────────────────────────────

    /// <summary>
    /// The weights used by the scoring function. Get to inspect; set to tune.
    /// Assign <see cref="ReelAlgorithmWeights.Default"/> to reset.
    /// </summary>
    ReelAlgorithmWeights Weights { get; set; }

    /// <summary>
    /// Returns a human-readable breakdown of why <paramref name="contentHash"/>
    /// received the score it did on the last GetForYou call. Drives the
    /// "Why am I seeing this?" UI feature.
    /// </summary>
    Task<string> ExplainScoreAsync(string contentHash, CancellationToken ct = default);
}
