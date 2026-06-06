// SPDX-License-Identifier: MIT

using AetherMesh.Content.Models;
using AetherMesh.Media.Core.Models;
using AetherMesh.Media.Social.Tests.Helpers;
using AetherMesh.Streaming.Models;

namespace AetherMesh.Media.Social.Tests;

/// <summary>
/// Unit tests for <see cref="FeedAggregator"/>: start/stop lifecycle, feed building
/// from content announcements and stream events, deduplication, capping, and pagination.
/// </summary>
public sealed class FeedAggregatorTests
{
    // ── Factory ────────────────────────────────────────────────────────────

    private static (FeedAggregator agg, FakeSocialGraph graph,
                    FakeStreamingService streaming, FakeContentService content)
        Make()
    {
        var graph     = new FakeSocialGraph();
        var streaming = new FakeStreamingService();
        var content   = new FakeContentService();
        var agg       = new FeedAggregator(graph, streaming, content);
        return (agg, graph, streaming, content);
    }

    private static ContentDescriptor MakeDescriptor(string hash, string name = "test.mp4") =>
        new ContentDescriptor { RootHash = hash, Name = name, TotalBytes = 1024, ContentType = "video/mp4" };

    private static StreamSession MakeLiveSession(string publisherUhid, string? title = null) =>
        new StreamSession
        {
            PublisherUhid    = publisherUhid,
            Title            = title ?? $"Stream by {publisherUhid}",
            State            = StreamState.Live,
            ContentType      = "video/mp4",
            Codec            = "h264",
            SegmentDurationMs = 2000,
        };

    // ── Lifecycle ──────────────────────────────────────────────────────────

    [Fact]
    public async Task StartAsync_IsIdempotent()
    {
        var (agg, _, _, content) = Make();
        await agg.StartAsync();
        await agg.StartAsync(); // second call must not double-subscribe

        content.RaiseContentAnnounced(MakeDescriptor("hash-idempotent"));

        var feed = await agg.GetFeedAsync();
        Assert.Single(feed); // not two items
    }

    [Fact]
    public async Task StopAsync_UnsubscribesEvents()
    {
        var (agg, _, _, content) = Make();
        await agg.StartAsync();
        await agg.StopAsync();

        content.RaiseContentAnnounced(MakeDescriptor("hash-after-stop"));

        var feed = await agg.GetFeedAsync();
        Assert.Empty(feed);
    }

    [Fact]
    public async Task StopAsync_IsIdempotent()
    {
        var (agg, _, _, _) = Make();
        await agg.StopAsync(); // stop without start — must not throw
        await agg.StopAsync();
    }

    // ── GetFeedAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetFeedAsync_ReturnsEmpty_WhenNoItems()
    {
        var (agg, _, _, _) = Make();
        await agg.StartAsync();

        var feed = await agg.GetFeedAsync();
        Assert.Empty(feed);
    }

    [Fact]
    public async Task GetFeedAsync_RespectsLimit()
    {
        var (agg, _, _, content) = Make();
        await agg.StartAsync();

        for (var i = 0; i < 10; i++)
            content.RaiseContentAnnounced(MakeDescriptor($"hash-{i:D3}"));

        var feed = await agg.GetFeedAsync(limit: 3);
        Assert.Equal(3, feed.Count);
    }

    [Fact]
    public async Task GetFeedAsync_RespectsOffset()
    {
        var (agg, _, _, content) = Make();
        await agg.StartAsync();

        for (var i = 0; i < 5; i++)
            content.RaiseContentAnnounced(MakeDescriptor($"hash-off-{i:D3}"));

        var all    = await agg.GetFeedAsync(limit: 5, offset: 0);
        var paged  = await agg.GetFeedAsync(limit: 5, offset: 3);

        Assert.Equal(2, paged.Count);
        Assert.Equal(all[3].Content.ContentHash, paged[0].Content.ContentHash);
    }

    [Fact]
    public async Task GetFeedAsync_ReturnsEmpty_WhenOffsetBeyondEnd()
    {
        var (agg, _, _, content) = Make();
        await agg.StartAsync();

        content.RaiseContentAnnounced(MakeDescriptor("hash-only-one"));

        var feed = await agg.GetFeedAsync(offset: 99);
        Assert.Empty(feed);
    }

    [Fact]
    public async Task GetFeedAsync_NegativeLimit_DefaultsToFifty()
    {
        var (agg, _, _, content) = Make();
        await agg.StartAsync();

        for (var i = 0; i < 5; i++)
            content.RaiseContentAnnounced(MakeDescriptor($"hash-lim-{i:D3}"));

        // Negative limit → treat as 50
        var feed = await agg.GetFeedAsync(limit: -1);
        Assert.Equal(5, feed.Count);
    }

    // ── ContentAnnounced ──────────────────────────────────────────────────

    [Fact]
    public async Task ContentAnnounced_AddsItemToFeed()
    {
        var (agg, _, _, content) = Make();
        await agg.StartAsync();

        content.RaiseContentAnnounced(MakeDescriptor("hash-vod-01", "my-video.mp4"));

        var feed = await agg.GetFeedAsync();
        Assert.Single(feed);
        Assert.Equal("hash-vod-01", feed[0].Content.ContentHash);
        Assert.Equal("my-video.mp4", feed[0].Content.Title);
        Assert.False(feed[0].IsLive);
    }

    [Fact]
    public async Task ContentAnnounced_InsertsNewestFirst()
    {
        var (agg, _, _, content) = Make();
        await agg.StartAsync();

        content.RaiseContentAnnounced(MakeDescriptor("hash-first"));
        content.RaiseContentAnnounced(MakeDescriptor("hash-second"));

        var feed = await agg.GetFeedAsync();
        Assert.Equal("hash-second", feed[0].Content.ContentHash);
        Assert.Equal("hash-first",  feed[1].Content.ContentHash);
    }

    [Fact]
    public async Task ContentAnnounced_DeduplicatesByHash()
    {
        var (agg, _, _, content) = Make();
        await agg.StartAsync();

        content.RaiseContentAnnounced(MakeDescriptor("hash-dup"));
        content.RaiseContentAnnounced(MakeDescriptor("hash-dup")); // same hash again

        var feed = await agg.GetFeedAsync();
        Assert.Single(feed);
    }

    [Fact]
    public async Task ContentAnnounced_CapAt500Items()
    {
        var (agg, _, _, content) = Make();
        await agg.StartAsync();

        for (var i = 0; i < 510; i++)
            content.RaiseContentAnnounced(MakeDescriptor($"hash-cap-{i:D4}"));

        var feed = await agg.GetFeedAsync(limit: 600);
        Assert.Equal(500, feed.Count);
    }

    [Fact]
    public async Task ContentAnnounced_RaisesItemArrivedEvent()
    {
        var (agg, _, _, content) = Make();
        await agg.StartAsync();

        MediaFeedItem? received = null;
        agg.ItemArrived += (_, item) => received = item;

        content.RaiseContentAnnounced(MakeDescriptor("hash-event"));

        Assert.NotNull(received);
        Assert.Equal("hash-event", received!.Content.ContentHash);
    }

    // ── StreamAnnounced ───────────────────────────────────────────────────

    [Fact]
    public async Task StreamAnnounced_AddsLiveItem_ForFollowedCreator()
    {
        var (agg, graph, streaming, _) = Make();
        await graph.FollowAsync("creator-A");
        await agg.StartAsync();

        // Use TCS to wait for the async void OnStreamAnnounced to finish
        var tcs = new TaskCompletionSource<MediaFeedItem>(TaskCreationOptions.RunContinuationsAsynchronously);
        agg.ItemArrived += (_, item) => tcs.TrySetResult(item);

        streaming.RaiseStreamAnnounced(MakeLiveSession("creator-A"));

        var arrived = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.True(arrived.IsLive);
        Assert.Equal("creator-A", arrived.Content.CreatorUhid);
    }

    [Fact]
    public async Task StreamAnnounced_IgnoresNonFollowedCreator()
    {
        var (agg, _, streaming, _) = Make();
        // graph has no follows
        await agg.StartAsync();

        var fired = false;
        agg.ItemArrived += (_, _) => fired = true;

        streaming.RaiseStreamAnnounced(MakeLiveSession("creator-nobody"));

        // Wait briefly for any residual async work to complete
        await Task.Delay(200);
        Assert.False(fired);

        var feed = await agg.GetFeedAsync();
        Assert.Empty(feed);
    }

    [Fact]
    public async Task StreamAnnounced_IgnoresNonLiveState()
    {
        var (agg, graph, streaming, _) = Make();
        await graph.FollowAsync("creator-B");
        await agg.StartAsync();

        var idleSession = MakeLiveSession("creator-B");
        idleSession.State = StreamState.Idle;

        streaming.RaiseStreamAnnounced(idleSession);

        await Task.Delay(200);
        var feed = await agg.GetFeedAsync();
        Assert.Empty(feed);
    }

    // ── StreamEnded ───────────────────────────────────────────────────────

    [Fact]
    public async Task StreamEnded_MarksLiveItemAsNotLive()
    {
        var (agg, graph, streaming, _) = Make();
        await graph.FollowAsync("creator-C");
        await agg.StartAsync();

        var tcs = new TaskCompletionSource<MediaFeedItem>(TaskCreationOptions.RunContinuationsAsynchronously);
        agg.ItemArrived += (_, item) => tcs.TrySetResult(item);

        var session = MakeLiveSession("creator-C");
        streaming.RaiseStreamAnnounced(session);

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // Now end the stream
        streaming.RaiseStreamEnded(session);

        var feed = await agg.GetFeedAsync();
        Assert.Single(feed);
        Assert.False(feed[0].IsLive);
    }

    // ── MarkWatchedAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task MarkWatchedAsync_IncrementsWatchCount()
    {
        var (agg, _, _, content) = Make();
        await agg.StartAsync();

        content.RaiseContentAnnounced(MakeDescriptor("hash-watch"));

        await agg.MarkWatchedAsync("hash-watch", 5_000);

        var feed = await agg.GetFeedAsync();
        Assert.Equal(1, feed[0].WatchCount);
    }

    [Fact]
    public async Task MarkWatchedAsync_AccumulatesMultipleCalls()
    {
        var (agg, _, _, content) = Make();
        await agg.StartAsync();

        content.RaiseContentAnnounced(MakeDescriptor("hash-multi-watch"));

        await agg.MarkWatchedAsync("hash-multi-watch", 1_000);
        await agg.MarkWatchedAsync("hash-multi-watch", 2_000);
        await agg.MarkWatchedAsync("hash-multi-watch", 3_000);

        var feed = await agg.GetFeedAsync();
        Assert.Equal(3, feed[0].WatchCount);
    }

    [Fact]
    public async Task MarkWatchedAsync_IgnoresNegativeMs()
    {
        var (agg, _, _, content) = Make();
        await agg.StartAsync();

        content.RaiseContentAnnounced(MakeDescriptor("hash-neg-watch"));

        await agg.MarkWatchedAsync("hash-neg-watch", -1);

        var feed = await agg.GetFeedAsync();
        Assert.Equal(0, feed[0].WatchCount);
    }

    [Fact]
    public async Task MarkWatchedAsync_IgnoresUnknownHash()
    {
        var (agg, _, _, _) = Make();
        await agg.StartAsync();

        // Must not throw
        await agg.MarkWatchedAsync("non-existent-hash", 5_000);
    }

    // ── GetNearbyLiveStreamsAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetNearbyLiveStreamsAsync_ReturnsLiveSessions()
    {
        var (agg, graph, streaming, _) = Make();
        await graph.FollowAsync("creator-D");

        var liveSession = MakeLiveSession("creator-D");
        streaming.ActiveStreams.Add(liveSession);

        await agg.StartAsync();

        var streams = await agg.GetNearbyLiveStreamsAsync();
        Assert.Single(streams);
        Assert.Equal("creator-D", streams[0].CreatorUhid);
    }

    [Fact]
    public async Task GetNearbyLiveStreamsAsync_ExcludesNonLiveSessions()
    {
        var (agg, _, streaming, _) = Make();

        streaming.ActiveStreams.Add(new StreamSession
        {
            PublisherUhid = "creator-idle",
            State         = StreamState.Ended,
        });

        await agg.StartAsync();

        var streams = await agg.GetNearbyLiveStreamsAsync();
        Assert.Empty(streams);
    }

    [Fact]
    public async Task GetNearbyLiveStreamsAsync_SortsFollowedCreatorsFirst()
    {
        var (agg, graph, streaming, _) = Make();
        await graph.FollowAsync("creator-followed");

        streaming.ActiveStreams.Add(MakeLiveSession("creator-nearby"));
        streaming.ActiveStreams.Add(MakeLiveSession("creator-followed"));

        await agg.StartAsync();

        var streams = await agg.GetNearbyLiveStreamsAsync();
        Assert.Equal(2, streams.Count);
        Assert.Equal("creator-followed", streams[0].CreatorUhid);
    }
}
