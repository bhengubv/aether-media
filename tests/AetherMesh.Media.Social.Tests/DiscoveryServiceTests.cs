// SPDX-License-Identifier: MIT

using AetherMesh.Handshake;
using AetherMesh.Media.Core.Models;
using AetherMesh.Media.Social.Tests.Helpers;
using AetherMesh.Streaming.Models;

namespace AetherMesh.Media.Social.Tests;

/// <summary>
/// Unit tests for <see cref="DiscoveryService"/>: peer discovery via handshake events,
/// capability filtering, profile resolution, deduplication, and live-stream enumeration.
/// </summary>
public sealed class DiscoveryServiceTests
{
    // ── Factory ────────────────────────────────────────────────────────────

    private static (DiscoveryService svc, FakeHandshakeService handshake,
                    FakeStreamingService streaming, FakeProfileService profiles)
        Make()
    {
        var handshake = new FakeHandshakeService();
        var streaming = new FakeStreamingService();
        var profiles  = new FakeProfileService();
        var svc       = new DiscoveryService(handshake, streaming, profiles);
        return (svc, handshake, streaming, profiles);
    }

    private static PeerCapabilities StreamingPeer(string uhid, string impl = "test/1.0") =>
        new PeerCapabilities(
            PeerUhid:             uhid,
            NegotiatedVersion:    1,
            Capabilities:         new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "streaming" },
            ImplementationVersion: impl,
            NegotiatedAt:         DateTimeOffset.UtcNow);

    private static PeerCapabilities NonStreamingPeer(string uhid) =>
        new PeerCapabilities(
            PeerUhid:             uhid,
            NegotiatedVersion:    1,
            Capabilities:         new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "messaging" },
            ImplementationVersion: "test/1.0",
            NegotiatedAt:         DateTimeOffset.UtcNow);

    // ── Lifecycle ──────────────────────────────────────────────────────────

    [Fact]
    public async Task StartAsync_SubscribesToPeerNegotiatedEvent()
    {
        var (svc, handshake, _, _) = Make();
        await svc.StartAsync();

        // Raise after start — should be seen
        var tcs = new TaskCompletionSource<MediaProfile>(TaskCreationOptions.RunContinuationsAsynchronously);
        svc.CreatorDiscovered += (_, p) => tcs.TrySetResult(p);

        handshake.RaisePeerNegotiated(StreamingPeer("peer-subscribe-test"));

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var creators = await svc.GetNearbyCreatorsAsync();
        Assert.Single(creators);
    }

    [Fact]
    public async Task StopAsync_UnsubscribesFromPeerNegotiatedEvent()
    {
        var (svc, handshake, _, _) = Make();
        await svc.StartAsync();
        await svc.StopAsync();

        var fired = false;
        svc.CreatorDiscovered += (_, _) => fired = true;

        handshake.RaisePeerNegotiated(StreamingPeer("peer-after-stop"));
        await Task.Delay(200);

        Assert.False(fired);
        Assert.Empty(await svc.GetNearbyCreatorsAsync());
    }

    [Fact]
    public async Task StartAsync_IsIdempotent()
    {
        var (svc, handshake, _, _) = Make();
        await svc.StartAsync();
        await svc.StartAsync(); // second call must not double-subscribe

        var tcs = new TaskCompletionSource<MediaProfile>(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        svc.CreatorDiscovered += (_, _) =>
        {
            Interlocked.Increment(ref count);
            tcs.TrySetResult(null!);
        };

        handshake.RaisePeerNegotiated(StreamingPeer("peer-idempotent"));
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await Task.Delay(100); // allow any errant second fire

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task StopAsync_IsIdempotent()
    {
        var (svc, _, _, _) = Make();
        await svc.StopAsync(); // stop before start — must not throw
        await svc.StopAsync();
    }

    // ── Seeding from already-negotiated peers ──────────────────────────────

    [Fact]
    public async Task StartAsync_SeedsFromAlreadyNegotiatedPeers()
    {
        var (svc, handshake, _, _) = Make();

        // Add peers BEFORE start
        handshake.NegotiatedPeers.Add(StreamingPeer("peer-pre-A"));
        handshake.NegotiatedPeers.Add(StreamingPeer("peer-pre-B"));
        handshake.NegotiatedPeers.Add(NonStreamingPeer("peer-pre-no-stream"));

        var discovered = new List<MediaProfile>();
        svc.CreatorDiscovered += (_, p) => { lock (discovered) discovered.Add(p); };

        await svc.StartAsync();

        // Wait for both async profile resolutions
        var deadline = DateTimeOffset.UtcNow.AddSeconds(2);
        while (discovered.Count < 2 && DateTimeOffset.UtcNow < deadline)
            await Task.Delay(50);

        Assert.Equal(2, discovered.Count);
        var creators = await svc.GetNearbyCreatorsAsync();
        Assert.Equal(2, creators.Count);
    }

    // ── PeerNegotiated event ───────────────────────────────────────────────

    [Fact]
    public async Task PeerNegotiated_StreamingCapable_DiscoveredAndEventFired()
    {
        var (svc, handshake, _, profiles) = Make();
        await svc.StartAsync();

        profiles.Profiles["creator-stream"] = FakeProfileService.MakeProfile(
            "creator-stream", "Test Creator");

        var tcs = new TaskCompletionSource<MediaProfile>(TaskCreationOptions.RunContinuationsAsynchronously);
        svc.CreatorDiscovered += (_, p) => tcs.TrySetResult(p);

        handshake.RaisePeerNegotiated(StreamingPeer("creator-stream"));

        var profile = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal("creator-stream", profile.Uhid);
        Assert.Equal("Test Creator",   profile.DisplayName);
    }

    [Fact]
    public async Task PeerNegotiated_NonStreamingCapable_Ignored()
    {
        var (svc, handshake, _, _) = Make();
        await svc.StartAsync();

        var fired = false;
        svc.CreatorDiscovered += (_, _) => fired = true;

        handshake.RaisePeerNegotiated(NonStreamingPeer("peer-no-stream"));
        await Task.Delay(200);

        Assert.False(fired);
        Assert.Empty(await svc.GetNearbyCreatorsAsync());
    }

    [Fact]
    public async Task PeerNegotiated_SamePeerTwice_FiresEventOnce()
    {
        var (svc, handshake, _, _) = Make();
        await svc.StartAsync();

        var count = 0;
        svc.CreatorDiscovered += (_, _) => Interlocked.Increment(ref count);

        handshake.RaisePeerNegotiated(StreamingPeer("peer-dup"));
        handshake.RaisePeerNegotiated(StreamingPeer("peer-dup")); // second announce
        await Task.Delay(300);

        Assert.Equal(1, count);

        var creators = await svc.GetNearbyCreatorsAsync();
        Assert.Single(creators);
    }

    [Fact]
    public async Task PeerNegotiated_ProfileServiceThrows_SynthesisesProfile()
    {
        var (svc, handshake, _, profiles) = Make();
        profiles.ThrowOnGet = new InvalidOperationException("Profile service down");

        await svc.StartAsync();

        var tcs = new TaskCompletionSource<MediaProfile>(TaskCreationOptions.RunContinuationsAsynchronously);
        svc.CreatorDiscovered += (_, p) => tcs.TrySetResult(p);

        handshake.RaisePeerNegotiated(StreamingPeer("peer-no-profile"));

        var profile = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal("peer-no-profile", profile.Uhid);
        Assert.False(string.IsNullOrEmpty(profile.DisplayName));
    }

    [Fact]
    public async Task PeerNegotiated_ProfileServiceReturnsNull_SynthesisesProfile()
    {
        var (svc, handshake, _, _) = Make();
        // profiles has no entry → returns null

        await svc.StartAsync();

        var tcs = new TaskCompletionSource<MediaProfile>(TaskCreationOptions.RunContinuationsAsynchronously);
        svc.CreatorDiscovered += (_, p) => tcs.TrySetResult(p);

        handshake.RaisePeerNegotiated(StreamingPeer("peer-null-profile"));

        var profile = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal("peer-null-profile", profile.Uhid);
    }

    // ── GetNearbyCreatorsAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetNearbyCreatorsAsync_ReturnsAllDiscoveredCreators()
    {
        var (svc, handshake, _, _) = Make();
        await svc.StartAsync();

        var discoveredAll = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        svc.CreatorDiscovered += (_, _) =>
        {
            if (Interlocked.Increment(ref count) == 3) discoveredAll.TrySetResult(true);
        };

        handshake.RaisePeerNegotiated(StreamingPeer("creator-1"));
        handshake.RaisePeerNegotiated(StreamingPeer("creator-2"));
        handshake.RaisePeerNegotiated(StreamingPeer("creator-3"));

        await discoveredAll.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var creators = await svc.GetNearbyCreatorsAsync();
        Assert.Equal(3, creators.Count);
    }

    // ── GetActiveStreamsAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetActiveStreamsAsync_ReturnsOnlyLiveSessions()
    {
        var (svc, _, streaming, _) = Make();
        await svc.StartAsync();

        streaming.ActiveStreams.Add(new StreamSession
        {
            PublisherUhid = "creator-live",
            Title         = "My Stream",
            State         = StreamState.Live,
            Codec         = "h264",
        });
        streaming.ActiveStreams.Add(new StreamSession
        {
            PublisherUhid = "creator-ended",
            State         = StreamState.Ended,
        });

        var streams = await svc.GetActiveStreamsAsync();
        Assert.Single(streams);
        Assert.Equal("creator-live", streams[0].CreatorUhid);
        Assert.True(streams[0].IsActive);
    }

    [Fact]
    public async Task GetActiveStreamsAsync_ReturnsEmpty_WhenNoLiveSessions()
    {
        var (svc, _, _, _) = Make();
        await svc.StartAsync();

        var streams = await svc.GetActiveStreamsAsync();
        Assert.Empty(streams);
    }

    [Fact]
    public async Task GetActiveStreamsAsync_MapsSessionFieldsCorrectly()
    {
        var (svc, _, streaming, _) = Make();
        await svc.StartAsync();

        var session = new StreamSession
        {
            Id                = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            PublisherUhid     = "creator-map",
            Title             = "Mapping Test Stream",
            State             = StreamState.Live,
            Codec             = "av1",
            SegmentDurationMs = 3_000,
            StartedAt = new DateTime(2025, 1, 15, 10, 0, 0, DateTimeKind.Utc),
        };
        streaming.ActiveStreams.Add(session);

        var streams = await svc.GetActiveStreamsAsync();
        Assert.Single(streams);

        var s = streams[0];
        Assert.Equal(session.Id,              s.StreamId);
        Assert.Equal("creator-map",           s.CreatorUhid);
        Assert.Equal("Mapping Test Stream",   s.Title);
        Assert.Equal("av1",                   s.Codec);
        Assert.Equal(3_000,                   s.SegmentDurationMs);
        Assert.Equal(new DateTimeOffset(session.StartedAt).ToUnixTimeMilliseconds(), s.StartedAtMs);
        Assert.True(s.IsActive);
    }
}
