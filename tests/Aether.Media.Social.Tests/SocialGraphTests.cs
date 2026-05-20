// SPDX-License-Identifier: MIT

using Aether.Dtn;
using Aether.Media.Social;
using Aether.Models;
using Aether.Protocol;
using Aether.Routing;

namespace Aether.Media.Social.Tests;

/// <summary>
/// Unit tests for <see cref="SocialGraph"/>: follow, unfollow, and
/// <see cref="ISocialGraph.IsFollowingAsync"/> state transitions.
///
/// All network dependencies are replaced by lightweight inline fakes that
/// complete immediately without throwing.
/// </summary>
public sealed class SocialGraphTests
{
    // ── Fakes ──────────────────────────────────────────────────────────────

    /// <summary>
    /// No-op implementation of <see cref="IDtnService"/> that records
    /// <see cref="BundleDelivered"/> subscribers and satisfies every call
    /// with a minimal valid return value.
    /// </summary>
    private sealed class FakeDtnService : IDtnService
    {
        public event EventHandler<DtnDeliveryReceipt>? BundleDelivered;

        // Expose a helper so tests can simulate a delivery receipt if needed.
        public void RaiseDelivered(DtnDeliveryReceipt receipt) =>
            BundleDelivered?.Invoke(this, receipt);

        public Task<DtnBundle> CreateBundleAsync(
            string         recipientUhid,
            byte[]         encryptedPayload,
            BundlePriority priority                 = BundlePriority.Normal,
            string?        recipientLastGeohash     = null,
            CancellationToken cancellationToken     = default)
        {
            var bundle = new DtnBundle
            {
                RecipientUhid    = recipientUhid,
                EncryptedPayload = encryptedPayload,
                Priority         = priority,
            };
            return Task.FromResult(bundle);
        }

        public Task HandleAsync(MeshPacket packet, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RunDeliveryScanAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> ExpireStaleAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyList<DtnBundle>> GetActiveBundlesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DtnBundle>>(Array.Empty<DtnBundle>());
    }

    /// <summary>
    /// No-op implementation of <see cref="IMeshSender"/> that discards every
    /// packet and reports zero peers.
    /// </summary>
    private sealed class FakeMeshSender : IMeshSender
    {
        public string LocalUhid { get; } = "local-test-node";
        public string? LocalGeohash => null;

        public IReadOnlyList<PeerInfo> GetConnectedPeers() =>
            Array.Empty<PeerInfo>();

        public Task<bool> SendAsync(
            MeshPacket packet,
            string nextHopUhid,
            CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<int> BroadcastAsync(
            MeshPacket packet,
            CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }

    // ── Factory ────────────────────────────────────────────────────────────

    private static SocialGraph MakeGraph(out FakeDtnService dtn, out FakeMeshSender sender)
    {
        dtn    = new FakeDtnService();
        sender = new FakeMeshSender();
        return new SocialGraph(dtn, sender);
    }

    // ── Tests ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task FollowAsync_AddsToFollowing()
    {
        var graph = MakeGraph(out _, out _);

        await graph.FollowAsync("creator-abc");

        var following = await graph.GetFollowingAsync();
        Assert.Contains("creator-abc", following);
    }

    [Fact]
    public async Task UnfollowAsync_RemovesFromFollowing()
    {
        var graph = MakeGraph(out _, out _);

        await graph.FollowAsync("creator-xyz");
        await graph.UnfollowAsync("creator-xyz");

        var following = await graph.GetFollowingAsync();
        Assert.DoesNotContain("creator-xyz", following);
    }

    [Fact]
    public async Task IsFollowingAsync_ReturnsTrueAfterFollow()
    {
        var graph = MakeGraph(out _, out _);

        await graph.FollowAsync("creator-follow-test");
        var result = await graph.IsFollowingAsync("creator-follow-test");

        Assert.True(result);
    }

    [Fact]
    public async Task IsFollowingAsync_ReturnsFalseAfterUnfollow()
    {
        var graph = MakeGraph(out _, out _);

        await graph.FollowAsync("creator-unfollow-test");
        await graph.UnfollowAsync("creator-unfollow-test");
        var result = await graph.IsFollowingAsync("creator-unfollow-test");

        Assert.False(result);
    }
}
