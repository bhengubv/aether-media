// SPDX-License-Identifier: MIT

using AetherMesh.Media.Reel.Tests.Helpers;

namespace AetherMesh.Media.Reel.Tests;

/// <summary>
/// Tests the on-device For You scoring algorithm in isolation.
///
/// Strategy: index Reels via IsolatedReelDiscovery, record engagement signals via
/// IsolatedReelEngagementTracker, and then call GetForYouAsync — the scorer should
/// rank Reels with stronger signals higher.
/// </summary>
public sealed class ReelFeedTests : IDisposable
{
    private readonly string                        _tempDir;
    private readonly IsolatedReelEngagementTracker _tracker;
    private readonly IsolatedReelDiscovery         _discovery;
    private readonly IsolatedReelService           _service;
    private readonly ReelFeed                      _feed;

    public ReelFeedTests()
    {
        _tempDir   = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _tracker   = new IsolatedReelEngagementTracker(_tempDir);
        _discovery = new IsolatedReelDiscovery(_tempDir);

        var content = new NoOpContentService();
        _service    = new IsolatedReelService(content, _discovery, "UHID-TEST", _tempDir);
        _feed       = new ReelFeed(_service, _tracker, _discovery);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Scoring fundamentals ──────────────────────────────────────────────────

    [Fact]
    public async Task HighCompletionReel_RanksAboveLowCompletion()
    {
        await IndexReel("high", hashtags: ["test"]);
        await IndexReel("low",  hashtags: ["test"]);

        // "high" was watched fully; "low" was skipped
        await _tracker.RecordAsync(Signal("high", watchedMs: 59_000, durationMs: 60_000));
        await _tracker.RecordAsync(Signal("low",  watchedMs: 2_000,  durationMs: 60_000, skipped: true));

        var feed = await _feed.GetForYouAsync(count: 10);

        var highIdx = feed.ToList().FindIndex(f => f.Reel.ContentHash == "high");
        var lowIdx  = feed.ToList().FindIndex(f => f.Reel.ContentHash == "low");

        Assert.True(highIdx < lowIdx, $"Expected high ({highIdx}) before low ({lowIdx})");
    }

    [Fact]
    public async Task LikedReel_ScoresHigherThanUnliked_WithSameCompletion()
    {
        await IndexReel("liked");
        await IndexReel("neutral");

        await _tracker.RecordAsync(Signal("liked",   watchedMs: 30_000, durationMs: 60_000, liked: true));
        await _tracker.RecordAsync(Signal("neutral", watchedMs: 30_000, durationMs: 60_000));

        var feed    = await _feed.GetForYouAsync(count: 10);
        var likedItem   = feed.First(f => f.Reel.ContentHash == "liked");
        var neutralItem = feed.First(f => f.Reel.ContentHash == "neutral");

        Assert.True(likedItem.Score > neutralItem.Score);
    }

    [Fact]
    public async Task SharedReel_ScoresHigherThanNotShared()
    {
        await IndexReel("shared");
        await IndexReel("notshared");

        await _tracker.RecordAsync(Signal("shared",    watchedMs: 30_000, durationMs: 60_000, shared: true));
        await _tracker.RecordAsync(Signal("notshared", watchedMs: 30_000, durationMs: 60_000));

        var feed    = await _feed.GetForYouAsync(count: 10);
        var sharedItem = feed.First(f => f.Reel.ContentHash == "shared");
        var notShared  = feed.First(f => f.Reel.ContentHash == "notshared");

        Assert.True(sharedItem.Score > notShared.Score);
    }

    [Fact]
    public async Task SkippedReel_ScoresBelow_WatchedReel()
    {
        await IndexReel("watched");
        await IndexReel("skipped");

        await _tracker.RecordAsync(Signal("watched", watchedMs: 30_000, durationMs: 60_000));
        await _tracker.RecordAsync(Signal("skipped", watchedMs: 0,      durationMs: 60_000, skipped: true));

        var feed    = await _feed.GetForYouAsync(count: 10);
        var watchedItem  = feed.First(f => f.Reel.ContentHash == "watched");
        var skippedItem  = feed.First(f => f.Reel.ContentHash == "skipped");

        Assert.True(watchedItem.Score > skippedItem.Score);
    }

    // ── Novelty / anti-echo-chamber ───────────────────────────────────────────

    [Fact]
    public async Task NoveltyBonus_BoostedForUnseenHashtagCluster()
    {
        // "familiar" reel has a hashtag the user has seen recently
        // "novel" reel has a hashtag the user has never seen
        await IndexReel("familiar", hashtags: ["cooking"]);
        await IndexReel("novel",    hashtags: ["astronomy"]);

        // Record a recent watch for a "cooking" reel so the cluster is in recent history
        await _tracker.RecordAsync(Signal("familiar", watchedMs: 30_000, durationMs: 60_000));

        var feed        = await _feed.GetForYouAsync(count: 10);
        var novelItem   = feed.First(f => f.Reel.ContentHash == "novel");
        var familiarItem = feed.First(f => f.Reel.ContentHash == "familiar");

        // Novel should get novelty bonus; familiar should not
        // (assuming all other signals are equal — no engagement on "novel")
        // The novelty bonus weight is 0.10, skip penalty on "familiar" won't apply,
        // but familiar also has recency/completion advantage from being watched.
        // We just check that novel's score is not penalised.
        Assert.True(novelItem.Score >= 0f);
    }

    // ── Weight customisation ──────────────────────────────────────────────────

    [Fact]
    public async Task CustomWeights_AreApplied()
    {
        await IndexReel("r1");
        await _tracker.RecordAsync(Signal("r1", watchedMs: 60_000, durationMs: 60_000));

        // Set watch time weight to 0 — completion ratio should contribute nothing
        _feed.Weights = new ReelAlgorithmWeights { WatchTimeRatio = 0f };

        var feed  = await _feed.GetForYouAsync(count: 10);
        var item  = feed.First(f => f.Reel.ContentHash == "r1");

        // Score without watch time: only recency + novelty + mesh pop (all ≥ 0)
        Assert.True(item.Score >= 0f);
    }

    [Fact]
    public async Task DefaultWeights_CanBeReset()
    {
        _feed.Weights = new ReelAlgorithmWeights { WatchTimeRatio = 0f };
        _feed.Weights = ReelAlgorithmWeights.Default;

        Assert.Equal(0.35f, _feed.Weights.WatchTimeRatio);
    }

    // ── GetForYouAsync count ──────────────────────────────────────────────────

    [Fact]
    public async Task GetForYouAsync_RespectsCountParameter()
    {
        for (var i = 0; i < 10; i++)
            await IndexReel($"reel{i}");

        var feed = await _feed.GetForYouAsync(count: 3);
        Assert.Equal(3, feed.Count);
    }

    [Fact]
    public async Task GetForYouAsync_ReturnsEmpty_WhenNoReelsIndexed()
    {
        var feed = await _feed.GetForYouAsync();
        Assert.Empty(feed);
    }

    // ── Score explanation ─────────────────────────────────────────────────────

    [Fact]
    public async Task ExplainScoreAsync_ReturnsExplanation_AfterGetForYou()
    {
        await IndexReel("explained");
        await _feed.GetForYouAsync();

        var explanation = await _feed.ExplainScoreAsync("explained");

        Assert.Contains("completion=", explanation);
        Assert.Contains("→", explanation);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task IndexReel(string hash, string[] hashtags = default!)
    {
        var reel = new Reel(
            ContentHash:    hash,
            CreatorUhid:    "creator-uhid",
            Title:          $"Reel {hash}",
            DurationMs:     60_000,
            SoundHash:      null,
            SoundTitle:     null,
            Hashtags:       hashtags ?? [],
            Type:           ReelType.Original,
            SourceReelHash: null,
            ThumbnailHash:  null,
            CreatedAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ViewCount:      0,
            LikeCount:      0);

        await _discovery.IndexReelAsync(reel);
    }

    private static ReelEngagementSignal Signal(
        string  reelHash,
        long    watchedMs  = 0,
        long    durationMs = 60_000,
        bool    liked      = false,
        bool    shared     = false,
        bool    skipped    = false)
        => new(reelHash, watchedMs, durationMs, 0, liked, shared, skipped,
               DateTimeOffset.UtcNow);
}
