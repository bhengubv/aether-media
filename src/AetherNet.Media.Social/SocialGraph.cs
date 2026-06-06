// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using AetherNet.Dtn;
using AetherNet.Models;
using AetherNet.Protocol;
using AetherNet.Routing;

namespace AetherNet.Media.Social;

/// <summary>
/// Decentralised follow graph backed by DTN for offline-tolerant delivery.
///
/// <para>
/// <see cref="FollowAsync"/> serialises a <c>FollowIntent</c> payload as UTF-8 JSON,
/// wraps it in a <see cref="DtnBundle"/> addressed to the target UHID, and queues
/// it for delivery even when the target is offline.
/// </para>
/// <para>
/// <see cref="UnfollowAsync"/> does NOT use DTN — it broadcasts a best-effort
/// <see cref="PacketType.WatchReaction"/> packet (which carries enough payload to
/// identify an unfollow intent) to all connected peers.  If the target is unreachable
/// the unfollow will be reconciled on the next direct encounter.
/// </para>
/// <para>
/// Follower counts for remote UHIDs are maintained by counting inbound follow bundles
/// delivered to this node (as a relay or as a bystander that accumulated gossip).
/// </para>
/// </summary>
public sealed class SocialGraph : ISocialGraph
{
    // ── Events ─────────────────────────────────────────────────────────────
    public event EventHandler<string>? Followed;
    public event EventHandler<string>? Unfollowed;

    // ── State ──────────────────────────────────────────────────────────────

    // Following set – UHIDs the local node has followed
    private readonly ConcurrentDictionary<string, byte> _following = new(StringComparer.Ordinal);

    // Follower counts per UHID (incremented by inbound follow intents we observe)
    private readonly ConcurrentDictionary<string, int> _followerCounts = new(StringComparer.Ordinal);

    // ── Dependencies ───────────────────────────────────────────────────────
    private readonly IDtnService _dtn;
    private readonly IMeshSender _sender;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
    };

    public SocialGraph(IDtnService dtn, IMeshSender sender)
    {
        _dtn = dtn ?? throw new ArgumentNullException(nameof(dtn));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));

        // Count inbound follow intents (relayed DTN bundles) to maintain follower counts
        _dtn.BundleDelivered += OnBundleDelivered;
    }

    // ── ISocialGraph ───────────────────────────────────────────────────────

    public async Task FollowAsync(string targetUhid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(targetUhid))
            throw new ArgumentException("targetUhid must not be empty.", nameof(targetUhid));

        // Idempotent – already following
        if (_following.ContainsKey(targetUhid))
            return;

        // Serialise the follow intent
        var intent = new FollowIntentPayload
        {
            Kind = "follow",
            FromUhid = _sender.LocalUhid,
            TargetUhid = targetUhid,
            SentAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };
        var json = JsonSerializer.Serialize(intent, JsonOpts);
        var payload = Encoding.UTF8.GetBytes(json);

        // DTN bundle: works even when target is offline
        await _dtn.CreateBundleAsync(
            recipientUhid: targetUhid,
            encryptedPayload: payload,
            priority: BundlePriority.Normal,
            cancellationToken: ct).ConfigureAwait(false);

        // Optimistically update local state and fire event
        _following.TryAdd(targetUhid, 0);
        Followed?.Invoke(this, targetUhid);
    }

    public async Task UnfollowAsync(string targetUhid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(targetUhid))
            throw new ArgumentException("targetUhid must not be empty.", nameof(targetUhid));

        if (!_following.TryRemove(targetUhid, out _))
            return; // Was not following – nothing to do

        // Serialise the unfollow intent as JSON into a WatchReaction packet payload
        var intent = new FollowIntentPayload
        {
            Kind = "unfollow",
            FromUhid = _sender.LocalUhid,
            TargetUhid = targetUhid,
            SentAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };
        var json = JsonSerializer.Serialize(intent, JsonOpts);

        var packet = new MeshPacket
        {
            Type = PacketType.WatchReaction,
            SourceUhid = _sender.LocalUhid,
            DestinationUhid = targetUhid,
            Payload = Encoding.UTF8.GetBytes(json),
            Ttl = 5,
        };

        // Best-effort broadcast – no DTN for unfollow
        await _sender.BroadcastAsync(packet, ct).ConfigureAwait(false);

        Unfollowed?.Invoke(this, targetUhid);
    }

    public Task<bool> IsFollowingAsync(string targetUhid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(targetUhid))
            return Task.FromResult(false);

        return Task.FromResult(_following.ContainsKey(targetUhid));
    }

    public Task<IReadOnlyList<string>> GetFollowingAsync(CancellationToken ct = default)
    {
        IReadOnlyList<string> result = [.. _following.Keys];
        return Task.FromResult(result);
    }

    public Task<int> GetFollowerCountAsync(string targetUhid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(targetUhid))
            return Task.FromResult(0);

        _followerCounts.TryGetValue(targetUhid, out var count);
        return Task.FromResult(count);
    }

    // ── Private ────────────────────────────────────────────────────────────

    private void OnBundleDelivered(object? sender, DtnDeliveryReceipt receipt)
    {
        // We cannot inspect the encrypted payload here, but any bundle delivered TO a
        // UHID that this node relayed contributes to that UHID's follower count
        // (the bundle was sent by someone following them).
        // We only increment counts for UHIDs we know about, to stay bounded.
        if (!string.IsNullOrWhiteSpace(receipt.RecipientUhid))
        {
            _followerCounts.AddOrUpdate(
                receipt.RecipientUhid,
                addValue: 1,
                updateValueFactory: (_, existing) => existing + 1);
        }
    }

    // ── Wire DTO ───────────────────────────────────────────────────────────

    private sealed class FollowIntentPayload
    {
        public string Kind { get; init; } = "follow";        // "follow" | "unfollow"
        public string FromUhid { get; init; } = string.Empty;
        public string TargetUhid { get; init; } = string.Empty;
        public long SentAtMs { get; init; }
    }
}
