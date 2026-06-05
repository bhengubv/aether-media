// SPDX-License-Identifier: MIT

using AetherMesh.Extensibility;
using Aether.Media.AI.Tests.Helpers;
using Aether.Media.Core.Models;

namespace Aether.Media.AI.Tests;

/// <summary>
/// Unit tests for <see cref="RoutePreseeder"/>.
///
/// <para>
/// Key invariants under test:
/// <list type="bullet">
///   <item>Empty feed → no routing calls.</item>
///   <item>Null / no routing service → no-op (magic-potion rule).</item>
///   <item>AI unavailable → all distinct creators pre-warmed up to cap.</item>
///   <item>AI high-confidence → only creators above threshold pre-warmed.</item>
///   <item>AI all-low-confidence → falls back to feed-order pre-warm.</item>
///   <item>Duplicate creators are de-duplicated before calling routing.</item>
///   <item>Cap of 10 is respected regardless of feed size.</item>
///   <item>AI exception per creator → skips that creator, others proceed.</item>
///   <item>Routing exception → swallowed, does not propagate.</item>
/// </list>
/// </para>
/// </summary>
public sealed class RoutePreseederTests
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private static MediaFeedItem MakeItem(string creatorUhid)
    {
        var content = new MediaContent(
            ContentHash:   Guid.NewGuid().ToString("N"),
            Title:         "Video",
            DurationMs:    60_000,
            Codec:         "H264",
            ContentType:   "video/mp4",
            CreatorUhid:   creatorUhid,
            SizeBytes:     1_000_000,
            CreatedAtMs:   DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ThumbnailHash: null,
            Tags:          Array.Empty<string>());

        return new MediaFeedItem(
            Content:       content,
            LikeCount:     0,
            ShareCount:    0,
            CommentCount:  0,
            WatchCount:    0,
            IsLive:        false,
            StreamId:      null,
            TopReactions:  Array.Empty<MediaReaction>(),
            PublishedAtMs: DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds());
    }

    private static IReadOnlyList<MediaFeedItem> Feed(params string[] creatorUhids)
        => creatorUhids.Select(MakeItem).ToList().AsReadOnly();

    // ── No-op cases ────────────────────────────────────────────────────────

    [Fact]
    public async Task PreseedFeedRoutes_EmptyList_NoRoutingCalls()
    {
        var ai      = new FakeAiProvider();
        var routing = new FakeRoutingService();
        var seeder  = new RoutePreseeder(ai, routing);

        await seeder.PreseedFeedRoutesAsync([]);

        Assert.Empty(routing.FindRouteCalls);
    }

    [Fact]
    public async Task PreseedFeedRoutes_NullRoutingService_IsNoOp()
    {
        var ai     = new FakeAiProvider();
        var seeder = new RoutePreseeder(ai, routing: null);

        // Must not throw even with items present
        await seeder.PreseedFeedRoutesAsync(Feed("creator-1", "creator-2"));
    }

    // ── AI unavailable ─────────────────────────────────────────────────────

    [Fact]
    public async Task PreseedFeedRoutes_AiUnavailable_PrewarmsAllCreators()
    {
        var ai      = new FakeAiProvider { Available = false };
        var routing = new FakeRoutingService();
        var seeder  = new RoutePreseeder(ai, routing);

        await seeder.PreseedFeedRoutesAsync(Feed("c-1", "c-2", "c-3"));

        Assert.Equal(3, routing.FindRouteCalls.Count);
        Assert.Contains("c-1", routing.FindRouteCalls);
        Assert.Contains("c-2", routing.FindRouteCalls);
        Assert.Contains("c-3", routing.FindRouteCalls);
    }

    [Fact]
    public async Task PreseedFeedRoutes_AiUnavailable_RespectsMaxCap()
    {
        var ai      = new FakeAiProvider { Available = false };
        var routing = new FakeRoutingService();
        var seeder  = new RoutePreseeder(ai, routing);

        // 15 creators, cap is 10
        var creators = Enumerable.Range(1, 15).Select(i => $"creator-{i}").ToArray();
        await seeder.PreseedFeedRoutesAsync(Feed(creators));

        Assert.Equal(10, routing.FindRouteCalls.Count);
    }

    // ── AI available — high confidence ────────────────────────────────────

    [Fact]
    public async Task PreseedFeedRoutes_AiHighConfidence_PrewarmsTopCreators()
    {
        var ai      = new FakeAiProvider();
        var routing = new FakeRoutingService();
        var seeder  = new RoutePreseeder(ai, routing);

        // Only "good-creator" has a high-confidence suggestion; others have none
        ai.RouteSuggestions["good-creator"] =
        [
            new AiRouteSuggestion(["hop-1", "hop-2"], Confidence: 0.9),
        ];

        await seeder.PreseedFeedRoutesAsync(Feed("good-creator", "unknown-creator"));

        // "good-creator" was pre-warmed; "unknown-creator" had confidence 0 < 0.6
        // so it's excluded (good-creator's list is non-empty → at least one qualifies)
        Assert.Contains("good-creator", routing.FindRouteCalls);
        Assert.DoesNotContain("unknown-creator", routing.FindRouteCalls);
    }

    [Fact]
    public async Task PreseedFeedRoutes_AiHighConfidence_SortsByConfidenceDescending()
    {
        var ai      = new FakeAiProvider();
        var routing = new FakeRoutingService();
        var seeder  = new RoutePreseeder(ai, routing);

        // Two creators with different confidence; both above threshold
        ai.RouteSuggestions["a"] = [new AiRouteSuggestion([], 0.7)];
        ai.RouteSuggestions["b"] = [new AiRouteSuggestion([], 0.95)];

        await seeder.PreseedFeedRoutesAsync(Feed("a", "b"));

        Assert.Equal(2, routing.FindRouteCalls.Count);
        // "b" should have been warmed first (highest confidence)
        Assert.Equal("b", routing.FindRouteCalls[0]);
    }

    // ── AI available — all low confidence → fallback ──────────────────────

    [Fact]
    public async Task PreseedFeedRoutes_AiAllLowConfidence_FallsBackToFeedOrder()
    {
        var ai      = new FakeAiProvider();
        var routing = new FakeRoutingService();
        var seeder  = new RoutePreseeder(ai, routing);

        // Both creators return confidence below threshold
        ai.RouteSuggestions["c-1"] = [new AiRouteSuggestion([], 0.3)];
        ai.RouteSuggestions["c-2"] = [new AiRouteSuggestion([], 0.2)];

        await seeder.PreseedFeedRoutesAsync(Feed("c-1", "c-2"));

        // Falls back to feed-order pre-warm (magic-potion: AI enhances, never blocks)
        Assert.Equal(2, routing.FindRouteCalls.Count);
        Assert.Contains("c-1", routing.FindRouteCalls);
        Assert.Contains("c-2", routing.FindRouteCalls);
    }

    // ── Duplicate creators ─────────────────────────────────────────────────

    [Fact]
    public async Task PreseedFeedRoutes_DuplicateCreators_FindRouteCalledOnce()
    {
        var ai      = new FakeAiProvider { Available = false };
        var routing = new FakeRoutingService();
        var seeder  = new RoutePreseeder(ai, routing);

        // Same creator appears three times (e.g. multiple videos)
        await seeder.PreseedFeedRoutesAsync(Feed("same", "same", "same"));

        Assert.Single(routing.FindRouteCalls);
        Assert.Equal("same", routing.FindRouteCalls[0]);
    }

    // ── AI exception per creator → skip, others proceed ──────────────────

    [Fact]
    public async Task PreseedFeedRoutes_AiThrowsForOneCreator_OthersStillPrewarmed()
    {
        var ai      = new ThrowingForUhidAiProvider("bad-creator");
        var routing = new FakeRoutingService();
        var seeder  = new RoutePreseeder(ai, routing);

        // "bad-creator" causes AI to throw; "good-creator" has a valid suggestion
        ai.RouteSuggestions["good-creator"] = [new AiRouteSuggestion([], 0.8)];

        await seeder.PreseedFeedRoutesAsync(Feed("bad-creator", "good-creator"));

        // "good-creator" was still pre-warmed despite "bad-creator" throwing
        Assert.Contains("good-creator", routing.FindRouteCalls);
    }

    // ── Routing exception → swallowed ─────────────────────────────────────

    [Fact]
    public async Task PreseedFeedRoutes_RoutingThrows_DoesNotPropagate()
    {
        var ai      = new FakeAiProvider { Available = false };
        var routing = new FakeRoutingService { ThrowOnFind = true };
        var seeder  = new RoutePreseeder(ai, routing);

        // Should complete without throwing even though FindRouteAsync throws
        var exception = await Record.ExceptionAsync(
            () => seeder.PreseedFeedRoutesAsync(Feed("c-1", "c-2")));

        Assert.Null(exception);
    }

    // ── Helper: AI that throws for a specific UHID ─────────────────────────

    /// <summary>
    /// Standalone AI provider that throws <see cref="InvalidOperationException"/>
    /// for a designated UHID and returns suggestions from a dictionary for all others.
    /// Implements <see cref="IAetherAiProvider"/> directly (FakeAiProvider is sealed).
    /// </summary>
    private sealed class ThrowingForUhidAiProvider : IAetherAiProvider
    {
        private readonly string _throwForUhid;

        public bool IsAvailable => true;

        public Dictionary<string, List<AiRouteSuggestion>> RouteSuggestions { get; } = new();

        public ThrowingForUhidAiProvider(string throwForUhid)
            => _throwForUhid = throwForUhid;

        public Task<IReadOnlyList<AiRouteSuggestion>> SuggestRoutesAsync(
            string destinationUhid,
            int payloadBytes,
            CancellationToken cancellationToken = default)
        {
            if (destinationUhid == _throwForUhid)
                throw new InvalidOperationException($"Simulated AI failure for {destinationUhid}");

            IReadOnlyList<AiRouteSuggestion> result =
                RouteSuggestions.TryGetValue(destinationUhid, out var list)
                    ? list
                    : Array.Empty<AiRouteSuggestion>();

            return Task.FromResult(result);
        }
    }
}
