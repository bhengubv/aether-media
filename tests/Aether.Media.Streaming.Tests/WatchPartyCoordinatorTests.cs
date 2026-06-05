// SPDX-License-Identifier: MIT

using Aether.Media.Core.Models;
using Aether.Media.Streaming.Tests.Helpers;

namespace Aether.Media.Streaming.Tests;

/// <summary>
/// Unit tests for <see cref="WatchPartyCoordinator"/>.
///
/// <para>
/// All tests use an in-process <see cref="FakeWatchTogetherService"/> and
/// <see cref="FakeMeshSender"/> so no network I/O occurs.
/// </para>
/// </summary>
public sealed class WatchPartyCoordinatorTests : IAsyncDisposable
{
    // ── Factory ────────────────────────────────────────────────────────────

    private readonly List<WatchPartyCoordinator> _coordinators = [];

    private (WatchPartyCoordinator Coordinator,
             FakeWatchTogetherService WatchTogether,
             FakeMeshSender Sender)
        Make()
    {
        var wt     = new FakeWatchTogetherService();
        var sender = new FakeMeshSender();
        var coord  = new WatchPartyCoordinator(wt, sender);
        _coordinators.Add(coord);
        return (coord, wt, sender);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var c in _coordinators)
            await c.DisposeAsync();
    }

    // ── Constructor ────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullWatchTogether_Throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            new WatchPartyCoordinator(null!, new FakeMeshSender()));

    [Fact]
    public void Constructor_NullSender_Throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            new WatchPartyCoordinator(new FakeWatchTogetherService(), null!));

    [Fact]
    public void Constructor_ActiveSessionId_IsNull()
    {
        var (coord, _, _) = Make();
        Assert.Null(coord.ActiveSessionId);
    }

    [Fact]
    public void Constructor_IsHost_IsFalse()
    {
        var (coord, _, _) = Make();
        Assert.False(coord.IsHost);
    }

    // ── HostWatchPartyAsync ────────────────────────────────────────────────

    [Fact]
    public async Task HostWatchPartyAsync_ReturnsNonEmptySessionId()
    {
        var (coord, _, _) = Make();
        var id = await coord.HostWatchPartyAsync("hash-abc", "Test Session");
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task HostWatchPartyAsync_SetsActiveSessionId()
    {
        var (coord, _, _) = Make();
        var id = await coord.HostWatchPartyAsync("hash-abc", "Test Session");
        Assert.Equal(id, coord.ActiveSessionId);
    }

    [Fact]
    public async Task HostWatchPartyAsync_SetsIsHost_True()
    {
        var (coord, _, _) = Make();
        await coord.HostWatchPartyAsync("hash-abc", "Test Session");
        Assert.True(coord.IsHost);
    }

    [Fact]
    public async Task HostWatchPartyAsync_WhenAlreadyInSession_Throws()
    {
        var (coord, _, _) = Make();
        await coord.HostWatchPartyAsync("hash-abc", "First");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coord.HostWatchPartyAsync("hash-xyz", "Second"));
    }

    [Fact]
    public async Task HostWatchPartyAsync_EmptyContentHash_Throws()
    {
        var (coord, _, _) = Make();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            coord.HostWatchPartyAsync("", "Test Session"));
    }

    // ── JoinWatchPartyAsync ────────────────────────────────────────────────

    [Fact]
    public async Task JoinWatchPartyAsync_SetsActiveSessionId()
    {
        var sessionId = Guid.NewGuid();
        var (coord, _, _) = Make();
        await coord.JoinWatchPartyAsync(sessionId);
        Assert.Equal(sessionId, coord.ActiveSessionId);
    }

    [Fact]
    public async Task JoinWatchPartyAsync_SetsIsHost_False()
    {
        var (coord, _, _) = Make();
        await coord.JoinWatchPartyAsync(Guid.NewGuid());
        Assert.False(coord.IsHost);
    }

    [Fact]
    public async Task JoinWatchPartyAsync_WhenAlreadyInSession_Throws()
    {
        var (coord, _, _) = Make();
        await coord.JoinWatchPartyAsync(Guid.NewGuid());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coord.JoinWatchPartyAsync(Guid.NewGuid()));
    }

    // ── PlayAsync / PauseAsync / SeekAsync — delegation ───────────────────

    [Fact]
    public async Task PlayAsync_DelegatesToWatchTogetherService()
    {
        var (coord, wt, _) = Make();
        var id = await coord.HostWatchPartyAsync("hash", "T");

        await coord.PlayAsync(10_000);

        Assert.Single(wt.PlayCalls, c => c.SessionId == id && c.PositionMs == 10_000);
    }

    [Fact]
    public async Task PauseAsync_DelegatesToWatchTogetherService()
    {
        var (coord, wt, _) = Make();
        var id = await coord.HostWatchPartyAsync("hash", "T");

        await coord.PauseAsync(5_000);

        Assert.Single(wt.PauseCalls, c => c.SessionId == id && c.PositionMs == 5_000);
    }

    [Fact]
    public async Task SeekAsync_DelegatesToWatchTogetherService()
    {
        var (coord, wt, _) = Make();
        var id = await coord.HostWatchPartyAsync("hash", "T");

        await coord.SeekAsync(20_000);

        Assert.Single(wt.SeekCalls, c => c.SessionId == id && c.PositionMs == 20_000);
    }

    [Fact]
    public async Task PlayAsync_WithoutSession_Throws()
    {
        var (coord, _, _) = Make();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coord.PlayAsync(0));
    }

    // ── SyncPositionChanged ────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_RaisesSyncPositionChanged()
    {
        var (coord, _, _) = Make();
        await coord.HostWatchPartyAsync("hash", "T");

        long? received = null;
        coord.SyncPositionChanged += (_, pos) => received = pos;

        await coord.PlayAsync(30_000);

        Assert.Equal(30_000, received);
    }

    [Fact]
    public async Task PauseAsync_RaisesSyncPositionChanged()
    {
        var (coord, _, _) = Make();
        await coord.HostWatchPartyAsync("hash", "T");

        long? received = null;
        coord.SyncPositionChanged += (_, pos) => received = pos;

        await coord.PauseAsync(15_000);

        Assert.Equal(15_000, received);
    }

    [Fact]
    public async Task SeekAsync_RaisesSyncPositionChanged()
    {
        var (coord, _, _) = Make();
        await coord.HostWatchPartyAsync("hash", "T");

        long? received = null;
        coord.SyncPositionChanged += (_, pos) => received = pos;

        await coord.SeekAsync(60_000);

        Assert.Equal(60_000, received);
    }

    // ── Participant join / leave ───────────────────────────────────────────

    [Fact]
    public async Task AddParticipant_RaisesParticipantJoined()
    {
        var (coord, wt, _) = Make();
        var sessionId = await coord.HostWatchPartyAsync("hash", "T");

        string? joined = null;
        coord.ParticipantJoined += (_, uhid) => joined = uhid;

        wt.AddParticipant(sessionId, "peer-abc");

        Assert.Equal("peer-abc", joined);
    }

    [Fact]
    public async Task RemoveParticipant_RaisesParticipantLeft()
    {
        var (coord, wt, _) = Make();
        var sessionId = await coord.HostWatchPartyAsync("hash", "T");

        // Add first so there is someone to remove
        wt.AddParticipant(sessionId, "peer-abc");

        string? left = null;
        coord.ParticipantLeft += (_, uhid) => left = uhid;

        wt.RemoveParticipant(sessionId, "peer-abc");

        Assert.Equal("peer-abc", left);
    }

    [Fact]
    public async Task SyncFromOtherSession_DoesNotFireParticipantEvents()
    {
        var (coord, wt, _) = Make();
        await coord.HostWatchPartyAsync("hash", "T");

        var joinedCount = 0;
        coord.ParticipantJoined += (_, _) => joinedCount++;

        // Simulate activity on a different session ID
        wt.AddParticipant(Guid.NewGuid(), "stranger");

        Assert.Equal(0, joinedCount);
    }

    // ── ReactionReceived ───────────────────────────────────────────────────

    [Fact]
    public async Task SendReactionAsync_RaisesReactionReceived()
    {
        var (coord, _, _) = Make();
        await coord.HostWatchPartyAsync("hash", "T");

        MediaReaction? received = null;
        coord.ReactionReceived += (_, r) => received = r;

        await coord.SendReactionAsync("❤️", positionMs: 5_000);

        Assert.NotNull(received);
        Assert.Equal(5_000, received.PositionMs);
    }

    // ── InviteAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task InviteAsync_SendsMeshPacketToTargetUhid()
    {
        var (coord, _, sender) = Make();
        await coord.HostWatchPartyAsync("hash", "T");

        await coord.InviteAsync("target-uhid");

        Assert.Contains(sender.SentPackets, t => t.NextHop == "target-uhid");
    }

    [Fact]
    public async Task InviteAsync_WithoutSession_Throws()
    {
        var (coord, _, _) = Make();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coord.InviteAsync("some-uhid"));
    }

    [Fact]
    public async Task InviteAsync_EmptyTargetUhid_Throws()
    {
        var (coord, _, _) = Make();
        await coord.HostWatchPartyAsync("hash", "T");
        await Assert.ThrowsAsync<ArgumentException>(() =>
            coord.InviteAsync(""));
    }

    // ── LeaveAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LeaveAsync_ClearsActiveSessionId()
    {
        var (coord, _, _) = Make();
        await coord.HostWatchPartyAsync("hash", "T");
        await coord.LeaveAsync();
        Assert.Null(coord.ActiveSessionId);
    }

    [Fact]
    public async Task LeaveAsync_ClearsIsHost()
    {
        var (coord, _, _) = Make();
        await coord.HostWatchPartyAsync("hash", "T");
        await coord.LeaveAsync();
        Assert.False(coord.IsHost);
    }

    [Fact]
    public async Task LeaveAsync_WhenNotInSession_DoesNotThrow()
    {
        var (coord, _, _) = Make();
        // Should silently succeed
        await coord.LeaveAsync();
    }

    // ── StartChipInAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task StartChipInAsync_DelegatesToWatchTogetherService()
    {
        var (coord, wt, _) = Make();
        await coord.HostWatchPartyAsync("hash", "T");

        AetherMesh.Streaming.Models.ChipInPool? pool = null;
        wt.ChipInUpdated += (_, p) => pool = p;

        await coord.StartChipInAsync(targetAmountZar: 100m, description: "Test ChipIn");

        Assert.NotNull(pool);
        Assert.Equal(100m, pool.TargetAmountZar);
        Assert.Equal("Test ChipIn", pool.ContentDescription);
    }

    [Fact]
    public async Task StartChipInAsync_WithoutSession_Throws()
    {
        var (coord, _, _) = Make();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coord.StartChipInAsync(50m, null));
    }

    // ── DisposeAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DisposeAsync_PreventsSubsequentHostCall()
    {
        var (coord, _, _) = Make();
        await coord.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            coord.HostWatchPartyAsync("hash", "T"));
    }

    [Fact]
    public async Task DisposeAsync_WhenInSession_LeavesSession()
    {
        var (coord, _, _) = Make();
        await coord.HostWatchPartyAsync("hash", "T");

        // DisposeAsync leaves the session; confirm no exception and no active session
        await coord.DisposeAsync();

        // Should be cleared; further calls throw ObjectDisposedException, not InvalidOperationException
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            coord.PlayAsync(0));
    }
}
