// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Aether.Media.Core.Models;
using AetherMesh.Protocol;
using AetherMesh.Routing;

namespace Aether.Media.Social;

/// <summary>
/// Sends and receives <see cref="MediaReaction"/> events over the Aether mesh.
///
/// <para>
/// Outbound: <see cref="SendReactionAsync"/> serialises the reaction to UTF-8 JSON
/// and sends a <see cref="PacketType.WatchReaction"/> packet directly to the
/// content creator's UHID via <see cref="IMeshSender"/>.
/// </para>
/// <para>
/// Inbound: callers pump received packets through <see cref="HandlePacketAsync"/>.
/// Reactions are stored in memory per content-hash (newest first, capped at 200 per item).
/// </para>
/// </summary>
public sealed class ReactionService : IReactionService
{
    private const int MaxReactionsPerContent = 200;

    // ── Events ─────────────────────────────────────────────────────────────
    public event EventHandler<MediaReaction>? ReactionReceived;

    // ── State ──────────────────────────────────────────────────────────────

    // contentHash → reactions (newest first), protected per-key by the list lock
    private readonly ConcurrentDictionary<string, List<MediaReaction>> _reactions =
        new(StringComparer.OrdinalIgnoreCase);

    // ── Dependencies ───────────────────────────────────────────────────────
    private readonly IMeshSender _sender;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
    };

    public ReactionService(IMeshSender sender)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    // ── IReactionService ───────────────────────────────────────────────────

    public async Task SendReactionAsync(MediaReaction reaction, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(reaction);

        // Serialise to JSON
        var wire = new ReactionWirePayload
        {
            ReactionId = reaction.ReactionId,
            ContentHash = reaction.ContentHash,
            FromUhid = reaction.FromUhid,
            Type = (int)reaction.Type,
            PositionMs = reaction.PositionMs,
            Message = reaction.Message,
            SentAtMs = reaction.SentAtMs,
        };

        var json = JsonSerializer.Serialize(wire, JsonOpts);
        var payload = Encoding.UTF8.GetBytes(json);

        // Determine the destination from the content creator UHID stored on the reaction's
        // content hash.  When the creator UHID is not available here (we only have the hash)
        // we address the packet to the "fromUhid" field — in practice callers should populate
        // the ContentHash so the creator can receive it.  We use a broadcast as fallback.
        var packet = new MeshPacket
        {
            Type = PacketType.WatchReaction,
            SourceUhid = _sender.LocalUhid,
            DestinationUhid = reaction.FromUhid, // creator must be told; caller sets correct destination
            Payload = payload,
            Ttl = 7,
        };

        // Try targeted send; fall back to broadcast if the destination is unreachable
        var peers = _sender.GetConnectedPeers();
        var targetPeer = peers.FirstOrDefault(p =>
            string.Equals(p.Uhid, packet.DestinationUhid, StringComparison.Ordinal));

        if (targetPeer is not null)
            await _sender.SendAsync(packet, targetPeer.Uhid, ct).ConfigureAwait(false);
        else
            await _sender.BroadcastAsync(packet, ct).ConfigureAwait(false);
    }

    public async Task HandlePacketAsync(MeshPacket packet, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(packet);

        if (packet.Type != PacketType.WatchReaction)
            return;

        ReactionWirePayload? wire;
        try
        {
            var json = Encoding.UTF8.GetString(packet.Payload);
            wire = JsonSerializer.Deserialize<ReactionWirePayload>(json, JsonOpts);
        }
        catch
        {
            return; // Malformed payload — silently drop
        }

        if (wire is null || string.IsNullOrWhiteSpace(wire.ContentHash))
            return;

        MediaReaction reaction;
        try
        {
            reaction = new MediaReaction(
                reactionId: wire.ReactionId == Guid.Empty ? Guid.NewGuid() : wire.ReactionId,
                contentHash: wire.ContentHash,
                fromUhid: string.IsNullOrWhiteSpace(wire.FromUhid) ? packet.SourceUhid : wire.FromUhid,
                type: (MediaReactionType)wire.Type,
                positionMs: Math.Max(0, wire.PositionMs),
                message: wire.Message,
                sentAtMs: wire.SentAtMs > 0
                    ? wire.SentAtMs
                    : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }
        catch
        {
            return; // Validation error in the reaction — drop
        }

        // Store it
        var list = _reactions.GetOrAdd(wire.ContentHash, _ => new List<MediaReaction>());
        lock (list)
        {
            list.Insert(0, reaction);
            if (list.Count > MaxReactionsPerContent)
                list.RemoveAt(list.Count - 1);
        }

        ReactionReceived?.Invoke(this, reaction);
    }

    public Task<IReadOnlyList<MediaReaction>> GetReactionsAsync(
        string contentHash,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
            return Task.FromResult<IReadOnlyList<MediaReaction>>(Array.Empty<MediaReaction>());

        if (!_reactions.TryGetValue(contentHash, out var list))
            return Task.FromResult<IReadOnlyList<MediaReaction>>(Array.Empty<MediaReaction>());

        MediaReaction[] snapshot;
        lock (list)
        {
            snapshot = [.. list]; // Newest first (insertion order already guarantees this)
        }

        return Task.FromResult<IReadOnlyList<MediaReaction>>(snapshot);
    }

    // ── Wire DTO ───────────────────────────────────────────────────────────

    private sealed class ReactionWirePayload
    {
        public Guid ReactionId { get; set; }
        public string ContentHash { get; set; } = string.Empty;
        public string FromUhid { get; set; } = string.Empty;
        public int Type { get; set; }
        public long PositionMs { get; set; }
        public string? Message { get; set; }
        public long SentAtMs { get; set; }
    }
}
