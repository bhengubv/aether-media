// SPDX-License-Identifier: MIT

using AetherNet.Protocol;
using AetherNet.Streaming;
using AetherNet.Streaming.Models;

namespace AetherMedia.Streaming.Tests.Helpers;

/// <summary>
/// In-process fake of <see cref="IWatchTogetherService"/> for unit and integration tests.
///
/// Calling <see cref="HostAsync"/> creates a real <see cref="WatchSession"/> (Hosting state).
/// Calling <see cref="FollowAsync"/> puts the caller into Following state for that session.
/// Control methods (<see cref="PlayAsync"/>, <see cref="PauseAsync"/>, etc.) update the
/// session state and raise <see cref="SyncApplied"/> synchronously so tests can assert
/// immediately without async waits.
/// </summary>
internal sealed class FakeWatchTogetherService : IWatchTogetherService
{
    // ── Events ──────────────────────────────────────────────────────────────
    // SessionInvited is raised by the mesh layer when an invite packet arrives;
    // the fake has no mesh layer so the event is a no-op add/remove stub.
    public event EventHandler<WatchSession>? SessionInvited { add { } remove { } }
    public event EventHandler<WatchSession>?                           SyncApplied;
    public event EventHandler<WatchReactionPayload>?                   ReactionReceived;
    public event EventHandler<WatchSession>?                           SessionEnded;
    public event EventHandler<(Guid SessionId, TorrentInfo Torrent)>? TorrentReceived;
    public event EventHandler<ChipInPool>?                            ChipInUpdated;

    // ── State ────────────────────────────────────────────────────────────────
    private readonly Dictionary<Guid, WatchSession> _sessions  = new();
    private readonly Dictionary<Guid, ChipInPool>   _chipIns   = new();

    // ── Recorded calls (for verifying delegation) ────────────────────────────
    public List<(Guid SessionId, long PositionMs)> PlayCalls  { get; } = [];
    public List<(Guid SessionId, long PositionMs)> PauseCalls { get; } = [];
    public List<(Guid SessionId, long PositionMs)> SeekCalls  { get; } = [];

    // ── IWatchTogetherService ────────────────────────────────────────────────

    public Task<WatchSession> HostAsync(
        string            contentRootHash,
        string            title,
        WatchMode         mode                = WatchMode.SharedFile,
        CancellationToken cancellationToken   = default)
    {
        var session = new WatchSession
        {
            ContentRootHash = contentRootHash,
            Title           = title,
            Mode            = mode,
            State           = WatchState.Hosting,
        };
        _sessions[session.Id] = session;
        return Task.FromResult(session);
    }

    public Task FollowAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(sessionId, out var existing))
        {
            existing.State = WatchState.Following;
        }
        else
        {
            // Create a stub session for the follower
            var session = new WatchSession { Id = sessionId, State = WatchState.Following };
            _sessions[sessionId] = session;
        }
        return Task.CompletedTask;
    }

    public Task PlayAsync(Guid sessionId, long positionMs, CancellationToken cancellationToken = default)
    {
        PlayCalls.Add((sessionId, positionMs));
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.PositionMs = positionMs;
            session.IsPlaying  = true;
            SyncApplied?.Invoke(this, session);
        }
        return Task.CompletedTask;
    }

    public Task PauseAsync(Guid sessionId, long positionMs, CancellationToken cancellationToken = default)
    {
        PauseCalls.Add((sessionId, positionMs));
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.PositionMs = positionMs;
            session.IsPlaying  = false;
            SyncApplied?.Invoke(this, session);
        }
        return Task.CompletedTask;
    }

    public Task SeekAsync(Guid sessionId, long positionMs, CancellationToken cancellationToken = default)
    {
        SeekCalls.Add((sessionId, positionMs));
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.PositionMs = positionMs;
            SyncApplied?.Invoke(this, session);
        }
        return Task.CompletedTask;
    }

    public Task SetSpeedAsync(Guid sessionId, double playbackSpeed, long positionMs, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.PlaybackSpeed = playbackSpeed;
            session.PositionMs    = positionMs;
            SyncApplied?.Invoke(this, session);
        }
        return Task.CompletedTask;
    }

    public Task SendReactionAsync(Guid sessionId, string reaction, long positionMs, CancellationToken cancellationToken = default)
    {
        var payload = new WatchReactionPayload
        {
            SessionId   = sessionId,
            Reaction    = reaction,
            SenderUhid  = "test-sender",
            PositionMs  = positionMs,
        };
        ReactionReceived?.Invoke(this, payload);
        return Task.CompletedTask;
    }

    public Task EndAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.State = WatchState.Ended;
            SessionEnded?.Invoke(this, session);
            _sessions.Remove(sessionId);
        }
        return Task.CompletedTask;
    }

    public Task HandleAsync(MeshPacket packet, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public IReadOnlyList<WatchSession> GetActiveSessions() =>
        [.. _sessions.Values];

    public Task BroadcastTorrentAsync(Guid sessionId, TorrentInfo torrent, CancellationToken ct = default)
    {
        TorrentReceived?.Invoke(this, (sessionId, torrent));
        return Task.CompletedTask;
    }

    public Task<ChipInPool> StartChipInAsync(
        Guid              sessionId,
        decimal           targetAmountZar,
        string?           contentDescription,
        string?           torrentInfoHash,
        string?           magnetLink,
        CancellationToken ct = default)
    {
        var pool = new ChipInPool
        {
            SessionId          = sessionId,
            InitiatorUhid      = "host",
            TargetAmountZar    = targetAmountZar,
            ContentDescription = contentDescription,
            TorrentInfoHash    = torrentInfoHash,
            MagnetLink         = magnetLink,
            State              = ChipInState.Collecting,
        };
        _chipIns[pool.Id] = pool;
        ChipInUpdated?.Invoke(this, pool);
        return Task.FromResult(pool);
    }

    public Task<ChipInPool?> ContributeAsync(
        Guid              chipInId,
        string            contributorUhid,
        decimal           amountZar,
        CancellationToken ct = default)
    {
        if (!_chipIns.TryGetValue(chipInId, out var pool))
            return Task.FromResult<ChipInPool?>(null);

        pool.Contributions.Add(new ChipInContribution
        {
            ContributorUhid = contributorUhid,
            AmountZar       = amountZar,
        });
        pool.CollectedAmountZar += amountZar;

        if (pool.IsFunded)
            pool.State = ChipInState.Funded;

        ChipInUpdated?.Invoke(this, pool);
        return Task.FromResult<ChipInPool?>(pool);
    }

    public ChipInPool? GetChipIn(Guid chipInId) =>
        _chipIns.GetValueOrDefault(chipInId);

    // ── Test helpers ─────────────────────────────────────────────────────────

    /// <summary>Simulate a participant joining an existing session (updates Participants list and fires SyncApplied).</summary>
    public void AddParticipant(Guid sessionId, string participantUhid)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return;
        var participants = new List<string>(session.Participants) { participantUhid };
        session.Participants = participants;
        SyncApplied?.Invoke(this, session);
    }

    /// <summary>Simulate a participant leaving an existing session.</summary>
    public void RemoveParticipant(Guid sessionId, string participantUhid)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return;
        var participants = session.Participants.Where(u => u != participantUhid).ToList();
        session.Participants = participants;
        SyncApplied?.Invoke(this, session);
    }
}
