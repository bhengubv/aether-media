// SPDX-License-Identifier: MIT

using Aether.Media.Streaming.Tests.Helpers;
using Aether.Streaming.Models;

namespace Aether.Media.Streaming.Tests;

/// <summary>
/// In-process integration tests covering plan verification items 5 and 6.
///
/// <list type="bullet">
///   <item>
///     <b>Item 5 — Watch together:</b> Host + 2 guests — <c>PlayAsync</c> issued, all 3
///     receive the same <c>SyncPositionChanged</c> value.
///   </item>
///   <item>
///     <b>Item 6 — ChipIn:</b> Host starts ChipIn at 50 ZAR, 2 guests contribute 25 ZAR
///     each — pool reaches target, <c>ChipInUpdated</c> fires for each state change.
///   </item>
/// </list>
///
/// <para>
/// All nodes share a single <see cref="FakeWatchTogetherService"/> instance, which
/// propagates events synchronously so no await-with-delays are necessary.
/// </para>
/// </summary>
public sealed class StreamingIntegrationTests : IAsyncDisposable
{
    private readonly List<WatchPartyCoordinator> _coordinators = [];

    private WatchPartyCoordinator MakeCoordinator(FakeWatchTogetherService wt)
    {
        var coord = new WatchPartyCoordinator(wt, new FakeMeshSender());
        _coordinators.Add(coord);
        return coord;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var c in _coordinators)
            await c.DisposeAsync();
    }

    // ── Plan item 5 — Watch together ──────────────────────────────────────

    /// <summary>
    /// Host + 2 guests all share one in-process <see cref="FakeWatchTogetherService"/>.
    /// When the host calls <see cref="WatchPartyCoordinator.PlayAsync"/>, each coordinator's
    /// <c>SyncPositionChanged</c> fires with the same <c>positionMs</c>.
    /// </summary>
    [Fact]
    public async Task WatchTogether_HostPlays_AllThreeNodesReceiveSamePosition()
    {
        // ── Arrange ───────────────────────────────────────────────────────
        var wt    = new FakeWatchTogetherService();
        var host  = MakeCoordinator(wt);
        var g1    = MakeCoordinator(wt);
        var g2    = MakeCoordinator(wt);

        var sessionId = await host.HostWatchPartyAsync("sha256:abc123", "Watch Party Test");
        await g1.JoinWatchPartyAsync(sessionId);
        await g2.JoinWatchPartyAsync(sessionId);

        const long ExpectedPositionMs = 30_000L;
        var positions = new List<long>();

        host.SyncPositionChanged += (_, pos) => positions.Add(pos);
        g1.SyncPositionChanged   += (_, pos) => positions.Add(pos);
        g2.SyncPositionChanged   += (_, pos) => positions.Add(pos);

        // ── Act ───────────────────────────────────────────────────────────
        await host.PlayAsync(ExpectedPositionMs);

        // ── Assert ────────────────────────────────────────────────────────
        // All 3 nodes received the sync notification
        Assert.Equal(3, positions.Count);

        // Every node received the exact same authoritative position
        Assert.All(positions, pos => Assert.Equal(ExpectedPositionMs, pos));
    }

    /// <summary>
    /// Play → Pause → Seek sequence: each state change fires on all nodes.
    /// </summary>
    [Fact]
    public async Task WatchTogether_PlayPauseSeek_AllNodesReceiveEachSync()
    {
        var wt    = new FakeWatchTogetherService();
        var host  = MakeCoordinator(wt);
        var guest = MakeCoordinator(wt);

        var sessionId = await host.HostWatchPartyAsync("sha256:def456", "Multi-Sync Test");
        await guest.JoinWatchPartyAsync(sessionId);

        var hostPositions  = new List<long>();
        var guestPositions = new List<long>();

        host.SyncPositionChanged  += (_, p) => hostPositions.Add(p);
        guest.SyncPositionChanged += (_, p) => guestPositions.Add(p);

        await host.PlayAsync(0L);
        await host.PauseAsync(5_000L);
        await host.SeekAsync(90_000L);

        // Host and guest each received 3 position updates
        Assert.Equal(3, hostPositions.Count);
        Assert.Equal(3, guestPositions.Count);

        // All updates match between host and guest
        for (var i = 0; i < 3; i++)
            Assert.Equal(hostPositions[i], guestPositions[i]);

        // Sequence is play=0, pause=5000, seek=90000
        Assert.Equal([0L, 5_000L, 90_000L], hostPositions);
    }

    /// <summary>
    /// A second host attempt while a session is active must be rejected; the original
    /// session must remain intact and continue working.
    /// </summary>
    [Fact]
    public async Task WatchTogether_DoubleHost_IsRejectedAndOriginalSessionSurvives()
    {
        var wt   = new FakeWatchTogetherService();
        var host = MakeCoordinator(wt);

        await host.HostWatchPartyAsync("sha256:abc", "Session 1");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.HostWatchPartyAsync("sha256:xyz", "Session 2"));

        // Original session still active
        Assert.NotNull(host.ActiveSessionId);
        Assert.True(host.IsHost);
    }

    // ── Plan item 6 — ChipIn ──────────────────────────────────────────────

    /// <summary>
    /// Host starts a 50 ZAR ChipIn; 2 guests contribute 25 ZAR each.
    /// The pool transitions to <see cref="ChipInState.Funded"/> and
    /// <c>ChipInUpdated</c> fires for every state change (start + 2 contributions).
    /// </summary>
    [Fact]
    public async Task ChipIn_TwoContributions_PoolReachesFundedState_ChipInUpdatedFires()
    {
        // ── Arrange ───────────────────────────────────────────────────────
        var wt   = new FakeWatchTogetherService();
        var host = MakeCoordinator(wt);

        await host.HostWatchPartyAsync("sha256:content", "ChipIn Session");

        var chipInEvents = new List<ChipInPool>();
        wt.ChipInUpdated += (_, pool) => chipInEvents.Add(pool);

        // ── Act — host starts the pool ─────────────────────────────────
        await host.StartChipInAsync(targetAmountZar: 50m, description: "Buy the full album");

        Assert.Single(chipInEvents); // StartChipIn fires one event
        var poolId = chipInEvents[0].Id;
        Assert.Equal(ChipInState.Collecting, chipInEvents[0].State);

        // ── Act — 2 guests contribute ──────────────────────────────────
        await wt.ContributeAsync(poolId, "guest-uhid-1", 25m);
        await wt.ContributeAsync(poolId, "guest-uhid-2", 25m);

        // ── Assert ────────────────────────────────────────────────────────

        // ChipInUpdated fired 3 times total (start + 2 contributions)
        Assert.Equal(3, chipInEvents.Count);

        // Final state is Funded
        Assert.Equal(ChipInState.Funded, chipInEvents[^1].State);

        // Verify the pool directly
        var finalPool = wt.GetChipIn(poolId);
        Assert.NotNull(finalPool);
        Assert.True(finalPool.IsFunded);
        Assert.Equal(50m, finalPool.CollectedAmountZar);
        Assert.Equal(2,   finalPool.Contributions.Count);
    }

    /// <summary>
    /// ChipIn that is partially funded does not transition to Funded prematurely.
    /// </summary>
    [Fact]
    public async Task ChipIn_PartialContribution_RemainsCollecting()
    {
        var wt   = new FakeWatchTogetherService();
        var host = MakeCoordinator(wt);

        await host.HostWatchPartyAsync("sha256:content", "Partial ChipIn");

        ChipInPool? lastPool = null;
        wt.ChipInUpdated += (_, pool) => lastPool = pool;

        await host.StartChipInAsync(targetAmountZar: 100m, description: null);
        var poolId = lastPool!.Id;

        // Only one contributor, only 40 ZAR of 100
        await wt.ContributeAsync(poolId, "guest-1", 40m);

        Assert.NotNull(lastPool);
        Assert.Equal(ChipInState.Collecting, lastPool.State);
        Assert.False(lastPool.IsFunded);
        Assert.Equal(40m, lastPool.CollectedAmountZar);
    }

    /// <summary>
    /// Verifies that reactions are propagated to all nodes in the session.
    /// </summary>
    [Fact]
    public async Task WatchTogether_ReactionSent_AllNodesReceiveIt()
    {
        var wt    = new FakeWatchTogetherService();
        var host  = MakeCoordinator(wt);
        var guest = MakeCoordinator(wt);

        var sessionId = await host.HostWatchPartyAsync("sha256:abc", "Reaction Test");
        await guest.JoinWatchPartyAsync(sessionId);

        var hostReactions  = 0;
        var guestReactions = 0;

        host.ReactionReceived  += (_, _) => hostReactions++;
        guest.ReactionReceived += (_, _) => guestReactions++;

        await host.SendReactionAsync("🔥", positionMs: 12_000);

        // Both nodes receive the reaction event
        Assert.Equal(1, hostReactions);
        Assert.Equal(1, guestReactions);
    }
}
