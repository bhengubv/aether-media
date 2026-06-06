// SPDX-License-Identifier: MIT

using AetherMedia.Core.Models;

namespace AetherMedia.Streaming;

/// <summary>
/// Coordinates a synchronized watch-together session on top of
/// <c>AetherNet.Streaming.IWatchTogetherService</c>.
///
/// <para>
/// The host calls <see cref="HostWatchPartyAsync"/> to create a session;
/// participants call <see cref="JoinWatchPartyAsync"/> after receiving an invitation.
/// Only the host can call <see cref="PlayAsync"/>, <see cref="PauseAsync"/>,
/// and <see cref="SeekAsync"/>.
/// </para>
/// </summary>
public interface IWatchPartyCoordinator : IAsyncDisposable
{
    /// <summary>The active session id, or null when no session is in progress.</summary>
    Guid? ActiveSessionId { get; }

    /// <summary>True when this node is the session host.</summary>
    bool IsHost { get; }

    /// <summary>Snapshot of participant UHIDs currently in the session.</summary>
    IReadOnlyList<string> ParticipantUhids { get; }

    /// <summary>Raised when a participant joins. Argument is the participant's UHID.</summary>
    event EventHandler<string>? ParticipantJoined;

    /// <summary>Raised when a participant leaves. Argument is the participant's UHID.</summary>
    event EventHandler<string>? ParticipantLeft;

    /// <summary>Raised when a reaction arrives from any participant.</summary>
    event EventHandler<MediaReaction>? ReactionReceived;

    /// <summary>
    /// Raised when the host changes playback position (play, pause, or seek).
    /// Argument is the authoritative position in milliseconds.
    /// </summary>
    event EventHandler<long>? SyncPositionChanged;

    /// <summary>Create a new watch party for <paramref name="contentRootHash"/> and become the host.</summary>
    Task<Guid> HostWatchPartyAsync(
        string contentRootHash,
        string title,
        CancellationToken ct = default);

    /// <summary>Join an existing watch party as a follower.</summary>
    Task JoinWatchPartyAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>Invite <paramref name="targetUhid"/> to the active session.</summary>
    Task InviteAsync(string targetUhid, CancellationToken ct = default);

    /// <summary>Leave the active session (or end it if this node is the host).</summary>
    Task LeaveAsync(CancellationToken ct = default);

    /// <summary>Host-only: resume playback at <paramref name="positionMs"/>.</summary>
    Task PlayAsync(long positionMs, CancellationToken ct = default);

    /// <summary>Host-only: pause playback at <paramref name="positionMs"/>.</summary>
    Task PauseAsync(long positionMs, CancellationToken ct = default);

    /// <summary>Host-only: seek to <paramref name="positionMs"/>.</summary>
    Task SeekAsync(long positionMs, CancellationToken ct = default);

    /// <summary>Send a reaction emoji/tag at <paramref name="positionMs"/> to all participants.</summary>
    Task SendReactionAsync(string reaction, long positionMs, CancellationToken ct = default);

    /// <summary>Start a ChipIn group-funding pool for content acquisition within the active session.</summary>
    Task StartChipInAsync(
        decimal targetAmountZar,
        string? description,
        CancellationToken ct = default);
}
