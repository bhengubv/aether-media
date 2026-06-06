// SPDX-License-Identifier: MIT

using AetherMedia.Reel.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherMedia.Reel;

/// <summary>
/// On-device For You feed scorer.
///
/// Scoring formula (all terms normalised to [0, 1] before weighting):
/// <code>
///   score = WatchTimeRatio  × completionRatio
///         + ReplayBonus     × min(replayCount / 3, 1)
///         + LikeSignal      × (liked ? 1 : 0)
///         + ShareSignal     × (shared ? 1 : 0)
///         - SkipPenalty     × (skipped ? 1 : 0)
///         + HashtagAffinity × maxHashtagAffinity
///         + CreatorAffinity × creatorAffinity
///         + NoveltyBonus    × noveltyScore          ← anti-echo-chamber
///         + RecencyBonus    × recencyScore
///         + MeshPopularity  × normalised(likeCount)
/// </code>
///
/// Novelty score: 1.0 for Reels whose hashtags have NOT appeared in the user's
/// recent watch history; 0.0 for clusters the user has engaged with heavily.
/// </summary>
public sealed class ReelFeed : IReelFeed
{
    private readonly IReelService           _service;
    private readonly IReelEngagementTracker _tracker;
    private readonly IReelDiscovery         _discovery;
    private readonly ILogger<ReelFeed>      _logger;

    // Last-computed score explanations — keyed by contentHash
    private readonly Dictionary<string, string> _scoreExplanations = [];

    public ReelAlgorithmWeights Weights { get; set; } = ReelAlgorithmWeights.Default;

    // ── Constructor ───────────────────────────────────────────────────────────

    public ReelFeed(
        IReelService           service,
        IReelEngagementTracker tracker,
        IReelDiscovery         discovery,
        ILogger<ReelFeed>?     logger = null)
    {
        _service   = service   ?? throw new ArgumentNullException(nameof(service));
        _tracker   = tracker   ?? throw new ArgumentNullException(nameof(tracker));
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _logger    = logger    ?? NullLogger<ReelFeed>.Instance;
    }

    // ── IReelFeed ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ReelFeedItem>> GetForYouAsync(
        int count = 20,
        CancellationToken ct = default)
    {
        // Candidate pool: all locally-indexed Reels
        var candidates   = await _discovery.SearchAsync(string.Empty, count: 500, ct).ConfigureAwait(false);
        return await RankAsync(candidates, count, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ReelFeedItem>> GetFollowingAsync(
        int count = 20,
        CancellationToken ct = default)
    {
        // Placeholder — a real implementation would use ISocialGraph.GetFollowingAsync()
        // and filter by followed creator UHIDs. For now, return For You.
        return await GetForYouAsync(count, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ReelFeedItem>> GetByHashtagAsync(
        string hashtag,
        int    count = 20,
        CancellationToken ct = default)
    {
        var candidates = await _discovery.SearchAsync(hashtag, count: count * 3, ct).ConfigureAwait(false);
        var filtered   = candidates.Where(r =>
            r.Hashtags.Any(h => h.Equals(hashtag.TrimStart('#'), StringComparison.OrdinalIgnoreCase)));
        return await RankAsync(filtered.ToList(), count, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ReelFeedItem>> GetByCreatorAsync(
        string creatorUhid,
        int    count = 20,
        CancellationToken ct = default)
    {
        var reels = await _service.GetByCreatorAsync(creatorUhid, ct).ConfigureAwait(false);
        return await RankAsync(reels.ToList(), count, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ReelFeedItem>> GetBookmarksAsync(
        int count = 50,
        CancellationToken ct = default)
    {
        var all = await _discovery.SearchAsync(string.Empty, count: 1000, ct).ConfigureAwait(false);
        var bookmarked = new List<Reel>();
        foreach (var r in all)
        {
            if (await _service.IsBookmarkedAsync(r.ContentHash, ct).ConfigureAwait(false))
                bookmarked.Add(r);
        }
        return await RankAsync(bookmarked, count, ct).ConfigureAwait(false);
    }

    // ── Engagement recording ──────────────────────────────────────────────────

    public async Task RecordWatchAsync(
        string contentHash,
        long   watchedMs,
        bool   replayed,
        CancellationToken ct = default)
    {
        var reel = await _service.GetAsync(contentHash, ct).ConfigureAwait(false);
        if (reel is null) return;

        var signal = new ReelEngagementSignal(
            ReelHash:       contentHash,
            WatchedMs:      watchedMs,
            DurationMs:     reel.DurationMs,
            ReplayCount:    replayed ? 1 : 0,
            Liked:          false,
            Shared:         false,
            Skipped:        false,
            LastWatchedAt:  DateTimeOffset.UtcNow);

        await _tracker.RecordAsync(signal, ct).ConfigureAwait(false);
    }

    public async Task RecordSkipAsync(string contentHash, CancellationToken ct = default)
    {
        var reel = await _service.GetAsync(contentHash, ct).ConfigureAwait(false);
        if (reel is null) return;

        var signal = new ReelEngagementSignal(
            ReelHash:       contentHash,
            WatchedMs:      0,
            DurationMs:     reel.DurationMs,
            ReplayCount:    0,
            Liked:          false,
            Shared:         false,
            Skipped:        true,
            LastWatchedAt:  DateTimeOffset.UtcNow);

        await _tracker.RecordAsync(signal, ct).ConfigureAwait(false);
    }

    public async Task RecordShareAsync(string contentHash, CancellationToken ct = default)
    {
        var existing = await _tracker.GetAsync(contentHash, ct).ConfigureAwait(false);
        if (existing is null)
        {
            var reel = await _service.GetAsync(contentHash, ct).ConfigureAwait(false);
            if (reel is null) return;
            existing = new ReelEngagementSignal(contentHash, 0, reel.DurationMs, 0,
                false, false, false, DateTimeOffset.UtcNow);
        }
        await _tracker.RecordAsync(existing with { Shared = true }, ct).ConfigureAwait(false);
    }

    public async Task<string> ExplainScoreAsync(string contentHash, CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        return _scoreExplanations.TryGetValue(contentHash, out var explanation)
            ? explanation
            : "No score explanation available — run GetForYouAsync first.";
    }

    // ── Core scoring ──────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<ReelFeedItem>> RankAsync(
        IReadOnlyList<Reel> candidates,
        int                 count,
        CancellationToken   ct)
    {
        if (candidates.Count == 0)
            return [];

        var signals          = await _tracker.GetAllAsync(ct).ConfigureAwait(false);
        var hashtagAffinities = await _tracker.GetHashtagAffinitiesAsync(ct).ConfigureAwait(false);
        var creatorAffinities = await _tracker.GetCreatorAffinitiesAsync(ct).ConfigureAwait(false);
        var recentHashes     = await _tracker.GetRecentHashtagsAsync(ct: ct).ConfigureAwait(false);
        var w                = Weights;

        var signalMap = signals.ToDictionary(s => s.ReelHash);

        // Find max like count for normalisation
        var maxLikes = candidates.Max(r => r.LikeCount);
        if (maxLikes == 0) maxLikes = 1;

        var now = DateTimeOffset.UtcNow;

        var scored = new List<(Reel Reel, float Score, string Explanation)>();

        foreach (var reel in candidates)
        {
            signalMap.TryGetValue(reel.ContentHash, out var sig);

            // ── Individual terms ──────────────────────────────────────────────
            var completion   = sig?.CompletionRatio ?? 0f;
            var replay       = sig is not null ? Math.Min(sig.ReplayCount / 3f, 1f) : 0f;
            var like         = (sig?.Liked    == true) ? 1f : 0f;
            var share        = (sig?.Shared   == true) ? 1f : 0f;
            var skip         = (sig?.Skipped  == true) ? 1f : 0f;

            var maxHashtagAff = reel.Hashtags.Length > 0
                ? reel.Hashtags.Max(h => hashtagAffinities.GetValueOrDefault(h, 0f))
                : 0f;

            var creatorAff = creatorAffinities.GetValueOrDefault(reel.CreatorUhid, 0f);

            // Novelty: 1.0 if none of this Reel's hashtags appear in recent watch history
            var novelty = reel.Hashtags.Length == 0 || !reel.Hashtags.Any(recentHashes.Contains)
                ? 1f : 0f;

            // Recency: exponential decay — full score at 0 h, half at 24 h, ~0 at 7 days
            var ageHours  = (float)(now - DateTimeOffset.FromUnixTimeMilliseconds(reel.CreatedAtMs)).TotalHours;
            var recency   = MathF.Exp(-ageHours / 24f);

            var meshPop   = (float)reel.LikeCount / maxLikes;

            // ── Weighted sum ─────────────────────────────────────────────────
            var score =
                w.WatchTimeRatio  * completion    +
                w.ReplayBonus     * replay        +
                w.LikeSignal      * like          +
                w.ShareSignal     * share         -
                w.SkipPenalty     * skip          +
                w.HashtagAffinity * maxHashtagAff +
                w.CreatorAffinity * creatorAff    +
                w.NoveltyBonus    * novelty       +
                w.RecencyBonus    * recency       +
                w.MeshPopularity  * meshPop;

            var explanation =
                $"completion={completion:F2}×{w.WatchTimeRatio} " +
                $"replay={replay:F2}×{w.ReplayBonus} " +
                $"like={like}×{w.LikeSignal} " +
                $"share={share}×{w.ShareSignal} " +
                $"skip=-{skip}×{w.SkipPenalty} " +
                $"hashtag={maxHashtagAff:F2}×{w.HashtagAffinity} " +
                $"creator={creatorAff:F2}×{w.CreatorAffinity} " +
                $"novelty={novelty}×{w.NoveltyBonus} " +
                $"recency={recency:F2}×{w.RecencyBonus} " +
                $"mesh={meshPop:F2}×{w.MeshPopularity} " +
                $"→ {score:F3}";

            scored.Add((reel, score, explanation));
        }

        var results = new List<ReelFeedItem>();
        foreach (var (reel, score, explanation) in scored.OrderByDescending(x => x.Score).Take(count))
        {
            _scoreExplanations[reel.ContentHash] = explanation;
            var liked      = await _service.IsLikedAsync(reel.ContentHash, ct).ConfigureAwait(false);
            var bookmarked = await _service.IsBookmarkedAsync(reel.ContentHash, ct).ConfigureAwait(false);
            results.Add(new ReelFeedItem(reel, score, liked, bookmarked));
        }

        return results;
    }
}
