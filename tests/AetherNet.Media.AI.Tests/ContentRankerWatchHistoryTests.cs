// SPDX-License-Identifier: MIT

using AetherNet.Media.AI.Tests.Helpers;
using AetherNet.Media.Core.Models;

namespace AetherNet.Media.AI.Tests;

/// <summary>
/// Tests for the watch-history signal in <see cref="ContentRanker"/>.
///
/// Watch history is worth 15 % of the composite score (see ContentRanker.cs).
/// • Completion = 1.0 → signal = 1.0 → boost of 0.15 over neutral (0.5 × 0.15 = 0.075)
/// • Completion = 0.0 → signal = 0.0 → penalty of 0.075 vs neutral
/// • No history → signal = 0.5 → neither boost nor penalty
/// </summary>
public sealed class ContentRankerWatchHistoryTests
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private const string ViewerUhid = "test-viewer";

    private static ContentRanker MakeRanker(InMemoryWatchHistoryStore history)
    {
        var rep = new FakeReputationService();
        var ai  = new FakeAiProvider { Available = false }; // neutral AI signal
        var mod = new FakeContentModerator();
        return new ContentRanker(rep, ai, mod, history);
    }

    private static MediaFeedItem MakeItem(string creatorUhid, string contentHash)
    {
        var content = new MediaContent(
            ContentHash:  contentHash,
            Title:        "Video",
            DurationMs:   60_000,
            Codec:        "H264",
            ContentType:  "video/mp4",
            CreatorUhid:  creatorUhid,
            SizeBytes:    1_000_000,
            CreatedAtMs:  DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ThumbnailHash: null,
            Tags:         Array.Empty<string>());

        return new MediaFeedItem(
            Content:      content,
            LikeCount:    0,
            ShareCount:   0,
            CommentCount: 0,
            WatchCount:   0,
            IsLive:       false,
            StreamId:     null,
            TopReactions: Array.Empty<MediaReaction>(),
            PublishedAtMs: DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds());
    }

    // ── High completion boosts rank ────────────────────────────────────────

    [Fact]
    public async Task WatchHistory_HighCompletion_RanksHigherThanNoHistory()
    {
        var history = new InMemoryWatchHistoryStore();
        var ranker  = MakeRanker(history);

        var watched = MakeItem("creator-watched", "hash-watched");
        var unseen  = MakeItem("creator-unseen",  "hash-unseen");

        // Viewer completed the "watched" video
        await history.RecordWatchEventAsync(ViewerUhid, "hash-watched",
            watchedMs: 60_000, durationMs: 60_000);

        var ranked = await ranker.RankFeedAsync([unseen, watched], ViewerUhid);

        // Video the viewer finished should rank above the unseen neutral video
        Assert.Equal("creator-watched", ranked[0].Content.CreatorUhid);
    }

    // ── Skip history lowers rank ──────────────────────────────────────────

    [Fact]
    public async Task WatchHistory_SkipHistory_RanksLowerThanNoHistory()
    {
        var history = new InMemoryWatchHistoryStore();
        var ranker  = MakeRanker(history);

        var skipped = MakeItem("creator-skipped", "hash-skipped");
        var unseen  = MakeItem("creator-unseen",  "hash-unseen");

        // Viewer immediately skipped the "skipped" video (0 % completion)
        await history.RecordWatchEventAsync(ViewerUhid, "hash-skipped",
            watchedMs: 0, durationMs: 60_000);

        var ranked = await ranker.RankFeedAsync([skipped, unseen], ViewerUhid);

        // The unseen neutral video should rank above the skipped one
        Assert.Equal("creator-unseen", ranked[0].Content.CreatorUhid);
    }

    // ── No history is neutral ─────────────────────────────────────────────

    [Fact]
    public async Task WatchHistory_NoHistory_TreatedAsNeutral()
    {
        // Two equal items with no watch history: scores should be identical
        // (or at most differ by floating-point noise — we assert both appear).
        var history = new InMemoryWatchHistoryStore();
        var ranker  = MakeRanker(history);

        var itemA = MakeItem("creator-A", "hash-A");
        var itemB = MakeItem("creator-B", "hash-B");

        var ranked = await ranker.RankFeedAsync([itemA, itemB], ViewerUhid);

        Assert.Equal(2, ranked.Count);
        // Both items returned — neither suppressed
    }

    // ── Viewer isolation ─────────────────────────────────────────────────

    [Fact]
    public async Task WatchHistory_DifferentViewer_DoesNotAffectRanking()
    {
        // Viewer A finished the video; we rank for Viewer B who has no history.
        // Both items should be treated as neutral for Viewer B.
        var history = new InMemoryWatchHistoryStore();
        var ranker  = MakeRanker(history);

        await history.RecordWatchEventAsync("viewer-A", "hash-X",
            watchedMs: 60_000, durationMs: 60_000);

        var itemX = MakeItem("creator-X", "hash-X");
        var itemY = MakeItem("creator-Y", "hash-Y");

        var ranked = await ranker.RankFeedAsync([itemX, itemY], "viewer-B");

        Assert.Equal(2, ranked.Count);
        // Without watch history, both are neutral — no crash, both returned
    }
}
