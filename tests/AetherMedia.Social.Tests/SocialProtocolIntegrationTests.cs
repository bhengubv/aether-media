// SPDX-License-Identifier: MIT

using AetherNet.Content.Models;
using AetherNet.Dtn;
using AetherMedia.Core.Models;
using AetherMedia.Social.Tests.Helpers;
using AetherNet.Models;
using AetherNet.Protocol;
using AetherNet.Routing;
using AetherNet.Streaming.Models;

namespace AetherMedia.Social.Tests;

/// <summary>
/// Integration tests proving the social protocol end-to-end within a single process:
///
/// <code>
///   NodeA.SocialGraph.FollowAsync(NodeB.Uhid)
///   NodeB publishes ContentDescriptor / LiveStream
///   NodeA.FeedAggregator.ItemArrived fires within 5 seconds
/// </code>
///
/// All mesh transport is simulated by in-process fakes; no network required.
/// These tests correspond to plan verification item 3:
///   "node A follows B, B publishes content, A receives it in feed within 5 seconds (no internet)"
/// </summary>
public sealed class SocialProtocolIntegrationTests
{
    // ── Inline fakes ───────────────────────────────────────────────────────

    private sealed class FakeDtnService : IDtnService
    {
        public event EventHandler<DtnDeliveryReceipt>? BundleDelivered;

        public void RaiseDelivered(DtnDeliveryReceipt receipt) =>
            BundleDelivered?.Invoke(this, receipt);

        public Task<DtnBundle> CreateBundleAsync(
            string            recipientUhid,
            byte[]            encryptedPayload,
            BundlePriority    priority             = BundlePriority.Normal,
            string?           recipientLastGeohash = null,
            CancellationToken cancellationToken    = default)
        {
            return Task.FromResult(new DtnBundle
            {
                RecipientUhid    = recipientUhid,
                EncryptedPayload = encryptedPayload,
                Priority         = priority,
            });
        }

        public Task HandleAsync(MeshPacket packet, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task RunDeliveryScanAsync(CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<int> ExpireStaleAsync(CancellationToken ct = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<DtnBundle>> GetActiveBundlesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DtnBundle>>(Array.Empty<DtnBundle>());
    }

    // ── Constants ──────────────────────────────────────────────────────────

    private const string NodeAUhid = "AAAAAA-NODEAAA";
    private const string NodeBUhid = "BBBBBB-NODEBBB";

    private static readonly TimeSpan Timeout5s = TimeSpan.FromSeconds(5);

    // ── Factory helpers ────────────────────────────────────────────────────

    private static (FeedAggregator Aggregator, SocialGraph Graph) MakeNodeA(
        FakeStreamingService streaming,
        FakeContentService   content)
    {
        var dtn    = new FakeDtnService();
        var sender = new FakeMeshSender { LocalUhid = NodeAUhid };
        var graph  = new SocialGraph(dtn, sender);
        var agg    = new FeedAggregator(graph, streaming, content);
        return (agg, graph);
    }

    private static StreamSession MakeLiveSession(string creatorUhid, string title) =>
        new StreamSession
        {
            PublisherUhid     = creatorUhid,
            Title             = title,
            Codec             = "H264",
            ContentType       = "video/mp4",
            SegmentDurationMs = 2_000,
            State             = StreamState.Live,
            StartedAt = DateTime.UtcNow,
        };

    private static ContentDescriptor MakeDescriptor(string rootHash, string name) =>
        new ContentDescriptor
        {
            RootHash    = rootHash,
            Name        = name,
            ContentType = "video/mp4",
            TotalBytes  = 104_857_600L,
            ChunkCount  = 100,
            ChunkHashes = Array.Empty<string>(),
            CreatedAt   = DateTime.UtcNow,
        };

    // ── Tests: Content announce ────────────────────────────────────────────

    [Fact(DisplayName = "NodeB publishes content → NodeA receives feed item within 5 s")]
    public async Task ContentAnnounce_ArrivesFeedWithin5Seconds()
    {
        // Arrange — shared in-process mesh bus (single fake service both nodes observe)
        var streaming = new FakeStreamingService();
        var content   = new FakeContentService();
        var (aggA, graphA) = MakeNodeA(streaming, content);

        await aggA.StartAsync();

        // NodeA follows NodeB (exercises the real SocialGraph DTN path)
        await graphA.FollowAsync(NodeBUhid);

        var tcs = new TaskCompletionSource<MediaFeedItem>(TaskCreationOptions.RunContinuationsAsynchronously);
        aggA.ItemArrived += (_, item) => tcs.TrySetResult(item);

        // Act — NodeB "publishes" a content descriptor
        content.RaiseContentAnnounced(MakeDescriptor("sha256-integration-001", "African Rhythms Vol. 1"));

        // Assert — feed item arrives within 5 seconds
        var item = await tcs.Task.WaitAsync(Timeout5s);

        Assert.NotNull(item);
        Assert.Equal("sha256-integration-001", item.Content.ContentHash);
        Assert.Equal("African Rhythms Vol. 1", item.Content.Title);
        Assert.False(item.IsLive);
    }

    [Fact(DisplayName = "NodeB publishes two items → NodeA feed has both, newest first")]
    public async Task ContentAnnounce_TwoItems_NewestFirst()
    {
        var streaming = new FakeStreamingService();
        var content   = new FakeContentService();
        var (aggA, _) = MakeNodeA(streaming, content);

        await aggA.StartAsync();

        var arrivedCount = 0;
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        aggA.ItemArrived += (_, _) =>
        {
            if (Interlocked.Increment(ref arrivedCount) >= 2)
                tcs.TrySetResult(true);
        };

        content.RaiseContentAnnounced(MakeDescriptor("hash-older", "Older Track"));
        content.RaiseContentAnnounced(MakeDescriptor("hash-newer", "New Mix"));

        await tcs.Task.WaitAsync(Timeout5s);

        var feed = await aggA.GetFeedAsync(limit: 10);
        Assert.Equal(2, feed.Count);

        // Newest inserted at index 0 (newest-first ordering)
        Assert.Equal("hash-newer", feed[0].Content.ContentHash);
        Assert.Equal("hash-older", feed[1].Content.ContentHash);
    }

    [Fact(DisplayName = "Duplicate content announce → only one feed item")]
    public async Task ContentAnnounce_Duplicate_DoesNotAddTwice()
    {
        var streaming = new FakeStreamingService();
        var content   = new FakeContentService();
        var (aggA, _) = MakeNodeA(streaming, content);

        await aggA.StartAsync();

        var tcs = new TaskCompletionSource<MediaFeedItem>(TaskCreationOptions.RunContinuationsAsynchronously);
        aggA.ItemArrived += (_, item) => tcs.TrySetResult(item);

        content.RaiseContentAnnounced(MakeDescriptor("hash-dup", "Duplicate"));
        await tcs.Task.WaitAsync(Timeout5s);

        // Announce the same hash again — should be deduplicated
        content.RaiseContentAnnounced(MakeDescriptor("hash-dup", "Duplicate Retransmit"));
        await Task.Delay(200); // give async void handler time

        var feed = await aggA.GetFeedAsync();
        Assert.Single(feed, i => i.Content.ContentHash == "hash-dup");
    }

    // ── Tests: Live stream announce ────────────────────────────────────────

    [Fact(DisplayName = "NodeB goes live → NodeA receives live feed item within 5 s (followed)")]
    public async Task StreamAnnounce_FollowedCreator_ArrivesFeedWithin5Seconds()
    {
        var streaming = new FakeStreamingService();
        var content   = new FakeContentService();
        var (aggA, graphA) = MakeNodeA(streaming, content);

        await aggA.StartAsync();
        await graphA.FollowAsync(NodeBUhid);

        var tcs = new TaskCompletionSource<MediaFeedItem>(TaskCreationOptions.RunContinuationsAsynchronously);
        aggA.ItemArrived += (_, item) => tcs.TrySetResult(item);

        streaming.RaiseStreamAnnounced(MakeLiveSession(NodeBUhid, "Live: Cape Town Session"));

        var item = await tcs.Task.WaitAsync(Timeout5s);

        Assert.NotNull(item);
        Assert.True(item.IsLive);
        Assert.Equal("Live: Cape Town Session", item.Content.Title);
        Assert.Equal(NodeBUhid, item.Content.CreatorUhid);
    }

    [Fact(DisplayName = "NodeB goes live → NodeA ignores it when NodeB is not followed")]
    public async Task StreamAnnounce_UnfollowedCreator_DoesNotArriveInFeed()
    {
        var streaming = new FakeStreamingService();
        var content   = new FakeContentService();
        var (aggA, _) = MakeNodeA(streaming, content);  // NodeA does NOT follow NodeB

        await aggA.StartAsync();

        var arrived = false;
        aggA.ItemArrived += (_, _) => { arrived = true; };

        streaming.RaiseStreamAnnounced(MakeLiveSession(NodeBUhid, "Should be ignored"));

        // Give the async void handler enough time to resolve the follow check
        await Task.Delay(500);

        Assert.False(arrived, "Feed item from unfollowed creator should not arrive.");
    }

    // ── Tests: Stream ended ────────────────────────────────────────────────

    [Fact(DisplayName = "NodeB ends stream → corresponding feed item is marked not-live")]
    public async Task StreamEnded_MarksItemAsNotLive()
    {
        var streaming = new FakeStreamingService();
        var content   = new FakeContentService();
        var (aggA, graphA) = MakeNodeA(streaming, content);

        await aggA.StartAsync();
        await graphA.FollowAsync(NodeBUhid);

        var tcs = new TaskCompletionSource<MediaFeedItem>(TaskCreationOptions.RunContinuationsAsynchronously);
        aggA.ItemArrived += (_, item) => tcs.TrySetResult(item);

        var session = MakeLiveSession(NodeBUhid, "Live Session Ending");
        streaming.RaiseStreamAnnounced(session);
        await tcs.Task.WaitAsync(Timeout5s);

        // Confirm item is live
        var feedBefore = await aggA.GetFeedAsync();
        Assert.True(feedBefore[0].IsLive);

        // NodeB ends the stream
        streaming.RaiseStreamEnded(session);

        var feedAfter = await aggA.GetFeedAsync();
        Assert.False(feedAfter[0].IsLive);
    }

    // ── Tests: SocialGraph integration ────────────────────────────────────

    [Fact(DisplayName = "FollowAsync → IsFollowingAsync returns true")]
    public async Task RealSocialGraph_Follow_IsFollowing_ReturnsTrue()
    {
        var dtn    = new FakeDtnService();
        var sender = new FakeMeshSender { LocalUhid = NodeAUhid };
        var graphA = new SocialGraph(dtn, sender);

        await graphA.FollowAsync(NodeBUhid);

        Assert.True(await graphA.IsFollowingAsync(NodeBUhid));
    }

    [Fact(DisplayName = "FollowAsync then UnfollowAsync → IsFollowingAsync returns false")]
    public async Task RealSocialGraph_FollowThenUnfollow_IsFollowingReturnsFalse()
    {
        var dtn    = new FakeDtnService();
        var sender = new FakeMeshSender { LocalUhid = NodeAUhid };
        var graphA = new SocialGraph(dtn, sender);

        await graphA.FollowAsync(NodeBUhid);
        await graphA.UnfollowAsync(NodeBUhid);

        Assert.False(await graphA.IsFollowingAsync(NodeBUhid));
    }

    [Fact(DisplayName = "FollowAsync is idempotent — following twice does not error")]
    public async Task RealSocialGraph_DoubleFollow_IsIdempotent()
    {
        var dtn    = new FakeDtnService();
        var sender = new FakeMeshSender { LocalUhid = NodeAUhid };
        var graphA = new SocialGraph(dtn, sender);

        await graphA.FollowAsync(NodeBUhid);
        await graphA.FollowAsync(NodeBUhid); // should not throw

        var following = await graphA.GetFollowingAsync();
        Assert.Single(following, u => u == NodeBUhid);
    }

    // ── Tests: Watch progress ─────────────────────────────────────────────

    [Fact(DisplayName = "MarkWatchedAsync increments WatchCount in feed")]
    public async Task MarkWatched_IncrementsCount()
    {
        var streaming = new FakeStreamingService();
        var content   = new FakeContentService();
        var (aggA, _) = MakeNodeA(streaming, content);

        await aggA.StartAsync();

        var tcs = new TaskCompletionSource<MediaFeedItem>(TaskCreationOptions.RunContinuationsAsynchronously);
        aggA.ItemArrived += (_, item) => tcs.TrySetResult(item);

        content.RaiseContentAnnounced(MakeDescriptor("watch-hash-001", "Watch Me"));
        await tcs.Task.WaitAsync(Timeout5s);

        await aggA.MarkWatchedAsync("watch-hash-001", 30_000L);

        var feed = await aggA.GetFeedAsync();
        Assert.Equal(1, feed[0].WatchCount);
    }
}
