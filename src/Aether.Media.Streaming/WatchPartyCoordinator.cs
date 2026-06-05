// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Aether.Media.Core.Models;
using AetherMesh.Protocol;
using AetherMesh.Routing;
using AetherMesh.Streaming;
using AetherMesh.Streaming.Models;

namespace Aether.Media.Streaming;

/// <summary>
/// Bridges the Aether.Media domain to <see cref="IWatchTogetherService"/> for
/// synchronised watch-together sessions ("watch parties").
///
/// <para>
/// All session control (play/pause/seek/reaction) is delegated to
/// <see cref="IWatchTogetherService"/>, which handles mesh packet encoding,
/// RTT compensation, and participant list management.  This coordinator translates
/// those service events to the <see cref="IWatchPartyCoordinator"/> surface.
/// </para>
/// <para>
/// <see cref="InviteAsync"/> sends a lightweight JSON invite packet directly to
/// the target UHID via <see cref="IMeshSender"/> rather than going through the
/// watch-together service (which only signals people already in the session).
/// </para>
/// </summary>
public sealed class WatchPartyCoordinator : IWatchPartyCoordinator
{
    // ── Events ─────────────────────────────────────────────────────────────
    public event EventHandler<string>? ParticipantJoined;
    public event EventHandler<string>? ParticipantLeft;
    public event EventHandler<MediaReaction>? ReactionReceived;
    public event EventHandler<long>? SyncPositionChanged;

    // ── State ──────────────────────────────────────────────────────────────
    public Guid? ActiveSessionId { get; private set; }
    public bool IsHost { get; private set; }

    public IReadOnlyList<string> ParticipantUhids
    {
        get
        {
            if (ActiveSessionId is null) return Array.Empty<string>();
            var session = _watchTogether.GetActiveSessions()
                .FirstOrDefault(s => s.Id == ActiveSessionId.Value);
            return session?.Participants ?? Array.Empty<string>();
        }
    }

    private bool _disposed;

    // Track previous participant set for join/leave diffing
    private readonly ConcurrentDictionary<string, byte> _knownParticipants =
        new(StringComparer.Ordinal);

    // ── Dependencies ───────────────────────────────────────────────────────
    private readonly IWatchTogetherService _watchTogether;
    private readonly IMeshSender _sender;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
    };

    public WatchPartyCoordinator(IWatchTogetherService watchTogether, IMeshSender sender)
    {
        _watchTogether = watchTogether ?? throw new ArgumentNullException(nameof(watchTogether));
        _sender        = sender        ?? throw new ArgumentNullException(nameof(sender));

        _watchTogether.SyncApplied       += OnSyncApplied;
        _watchTogether.ReactionReceived  += OnWatchReactionReceived;
        _watchTogether.SessionEnded      += OnSessionEnded;
    }

    // ── IWatchPartyCoordinator ─────────────────────────────────────────────

    public async Task<Guid> HostWatchPartyAsync(
        string contentRootHash,
        string title,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(contentRootHash))
            throw new ArgumentException("contentRootHash must not be empty.", nameof(contentRootHash));

        if (ActiveSessionId is not null)
            throw new InvalidOperationException("Already in a session. Call LeaveAsync first.");

        var session = await _watchTogether.HostAsync(
            contentRootHash: contentRootHash,
            title: title,
            mode: WatchMode.SharedFile,
            cancellationToken: ct).ConfigureAwait(false);

        ActiveSessionId = session.Id;
        IsHost = true;
        _knownParticipants.Clear();

        return session.Id;
    }

    public async Task JoinWatchPartyAsync(Guid sessionId, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (ActiveSessionId is not null)
            throw new InvalidOperationException("Already in a session. Call LeaveAsync first.");

        await _watchTogether.FollowAsync(sessionId, ct).ConfigureAwait(false);

        ActiveSessionId = sessionId;
        IsHost = false;
        _knownParticipants.Clear();
    }

    public async Task InviteAsync(string targetUhid, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(targetUhid))
            throw new ArgumentException("targetUhid must not be empty.", nameof(targetUhid));

        if (ActiveSessionId is null)
            throw new InvalidOperationException("No active session to invite into.");

        // Look up session details to populate the invite packet
        var session = _watchTogether.GetActiveSessions()
            .FirstOrDefault(s => s.Id == ActiveSessionId.Value);

        var invite = new WatchPartyInvitePayload
        {
            SessionId = ActiveSessionId.Value,
            HostUhid = _sender.LocalUhid,
            ContentRootHash = session?.ContentRootHash ?? string.Empty,
            Title = session?.Title ?? string.Empty,
        };

        var json = JsonSerializer.Serialize(invite, JsonOpts);
        var payload = Encoding.UTF8.GetBytes(json);

        var packet = new MeshPacket
        {
            Type = PacketType.WatchSync,
            SourceUhid = _sender.LocalUhid,
            DestinationUhid = targetUhid,
            Payload = payload,
            Ttl = 7,
        };

        await _sender.SendAsync(packet, targetUhid, ct).ConfigureAwait(false);
    }

    public async Task LeaveAsync(CancellationToken ct = default)
    {
        if (ActiveSessionId is null) return;

        var sessionId = ActiveSessionId.Value;
        ActiveSessionId = null;
        IsHost = false;
        _knownParticipants.Clear();

        await _watchTogether.EndAsync(sessionId, ct).ConfigureAwait(false);
    }

    public async Task PlayAsync(long positionMs, CancellationToken ct = default)
    {
        EnsureActiveSession();
        await _watchTogether.PlayAsync(ActiveSessionId!.Value, positionMs, ct).ConfigureAwait(false);
    }

    public async Task PauseAsync(long positionMs, CancellationToken ct = default)
    {
        EnsureActiveSession();
        await _watchTogether.PauseAsync(ActiveSessionId!.Value, positionMs, ct).ConfigureAwait(false);
    }

    public async Task SeekAsync(long positionMs, CancellationToken ct = default)
    {
        EnsureActiveSession();
        await _watchTogether.SeekAsync(ActiveSessionId!.Value, positionMs, ct).ConfigureAwait(false);
    }

    public async Task SendReactionAsync(string reaction, long positionMs, CancellationToken ct = default)
    {
        EnsureActiveSession();
        await _watchTogether.SendReactionAsync(
            sessionId: ActiveSessionId!.Value,
            reaction: reaction,
            positionMs: positionMs,
            cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task StartChipInAsync(
        decimal targetAmountZar,
        string? description,
        CancellationToken ct = default)
    {
        EnsureActiveSession();

        await _watchTogether.StartChipInAsync(
            sessionId: ActiveSessionId!.Value,
            targetAmountZar: targetAmountZar,
            contentDescription: description,
            torrentInfoHash: null,
            magnetLink: null,
            ct: ct).ConfigureAwait(false);
    }

    // ── IAsyncDisposable ───────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _watchTogether.SyncApplied      -= OnSyncApplied;
        _watchTogether.ReactionReceived -= OnWatchReactionReceived;
        _watchTogether.SessionEnded     -= OnSessionEnded;

        if (ActiveSessionId is not null)
        {
            try { await LeaveAsync().ConfigureAwait(false); }
            catch { /* Dispose must not throw */ }
        }
    }

    // ── Private ────────────────────────────────────────────────────────────

    private void EnsureActiveSession()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (ActiveSessionId is null)
            throw new InvalidOperationException("No active session.");
    }

    private void OnSyncApplied(object? sender, WatchSession session)
    {
        if (session.Id != ActiveSessionId)
            return;

        // Fire position sync event
        SyncPositionChanged?.Invoke(this, session.PositionMs);

        // Diff participants for join/leave events
        DiffParticipants(session.Participants);
    }

    private void DiffParticipants(IReadOnlyList<string> currentParticipants)
    {
        var currentSet = new HashSet<string>(currentParticipants, StringComparer.Ordinal);

        // Detect joins
        foreach (var uhid in currentSet)
        {
            if (_knownParticipants.TryAdd(uhid, 0))
                ParticipantJoined?.Invoke(this, uhid);
        }

        // Detect leaves
        foreach (var uhid in _knownParticipants.Keys.ToList())
        {
            if (!currentSet.Contains(uhid) && _knownParticipants.TryRemove(uhid, out _))
                ParticipantLeft?.Invoke(this, uhid);
        }
    }

    private void OnWatchReactionReceived(object? sender, WatchReactionPayload payload)
    {
        if (ActiveSessionId is null || payload.SessionId != ActiveSessionId.Value)
            return;

        // Map WatchReactionPayload → MediaReaction (Like type as default for emoji/text reactions)
        try
        {
            var reaction = new MediaReaction(
                reactionId: Guid.NewGuid(),
                contentHash: payload.SessionId.ToString("N"),
                fromUhid: payload.SenderUhid,
                type: MediaReactionType.Like,
                positionMs: Math.Max(0, payload.PositionMs),
                message: null,
                sentAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            ReactionReceived?.Invoke(this, reaction);
        }
        catch
        {
            // Swallow — reaction events are non-critical
        }
    }

    private void OnSessionEnded(object? sender, WatchSession session)
    {
        if (session.Id != ActiveSessionId)
            return;

        ActiveSessionId = null;
        IsHost = false;
        _knownParticipants.Clear();
    }

    // ── Wire DTO ───────────────────────────────────────────────────────────

    private sealed class WatchPartyInvitePayload
    {
        public Guid SessionId { get; init; }
        public string HostUhid { get; init; } = string.Empty;
        public string ContentRootHash { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
    }
}
