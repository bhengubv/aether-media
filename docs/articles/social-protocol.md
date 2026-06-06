# Aether Media — Social Layer Wire Protocol

The social layer has no server. Every follow, reaction, profile sync, and content announcement is a signed message that travels over whatever radio is currently available — BLE, Wi-Fi Direct, NearLink, LoRa, or HTTP relay. This document specifies the packet types, wire payloads, and delivery guarantees for each operation.

All JSON payloads follow the canonical wire format defined in `wire-format.md`: field names in `snake_case`, timestamps as Unix milliseconds (integer, `_ms` suffix).

---

## Follow

Follows use the Delay-Tolerant Networking (DTN) layer so they survive offline peers.

**Sender side (`SocialGraph.FollowAsync`).** When the local node follows a creator, a `FollowIntentPayload` is serialised to UTF-8 JSON and passed to `IDtnService.CreateBundleAsync` addressed to the target UHID:

```json
{
  "kind":       "follow",
  "from_uhid":  "uhid-alice-0001",
  "target_uhid": "uhid-bob-0002",
  "sent_at_ms": 1716249600000
}
```

The DTN bundle waits in the local store — for up to 72 hours — and is delivered the moment any route to the target opens. No push notification infrastructure is involved.

**Receiver side.** When the bundle arrives, the target node increments its follower count. Because the payload is encrypted with the Signal Protocol session key (X3DH + Double Ratchet), intermediate relay nodes cannot inspect the content; they can only observe that a bundle addressed to a given UHID was delivered and increment a relay-based follower estimate.

**Unfollow.** Unfollows are best-effort and do not use DTN. The serialised payload (same structure, `"kind": "unfollow"`) is wrapped in a `PacketType.WatchReaction` packet and broadcast to all connected peers. If the target is unreachable, the unfollow is reconciled on the next direct encounter.

---

## Reaction

Reactions travel as best-effort `MeshPacket` messages, not DTN bundles, because low latency matters more than guaranteed delivery for live engagement.

**Packet type.** `PacketType.WatchReaction` (the same packet type used for unfollow, distinguished by payload structure). The packet is addressed to the content creator's UHID and routed by AODV with Ed25519-signed route replies.

**Wire payload.** The reaction is serialised to UTF-8 JSON inside the `MeshPacket.Payload` field. All fields follow `wire-format.md`'s `MediaReaction` schema:

```json
{
  "reaction_id":  "550e8400-e29b-41d4-a716-446655440000",
  "content_hash": "a3f9ee2c84b1d6f4230c5d987654e32a1b0c9d8e7f6a5b4c3d2e1f0a9b8c7d6",
  "from_uhid":    "uhid-bob-0002",
  "type":         "comment",
  "position_ms":  42000,
  "message":      "This is incredible!",
  "sent_at_ms":   1716249900000
}
```

| Field | Notes |
|-------|-------|
| `type` | One of `"like"`, `"share"`, `"comment"`, `"super_react"`. Lowercase string — never an integer on the wire. |
| `position_ms` | Playback position when the reaction was sent. `0` means not anchored to a position. |
| `message` | Required for `"comment"`; `null` for all other types. |

**Routing fallback.** `ReactionService.SendReactionAsync` first attempts a targeted send to the creator's peer entry. If the peer is not in the current connected-peer list it falls back to broadcast with TTL = 7. During live streams the creator's device is usually one or two hops away, so targeted delivery succeeds in practice.

**Receiving.** Inbound packets are pumped through `ReactionService.HandlePacketAsync`. The service rejects packets whose `PacketType` is not `WatchReaction`, silently drops malformed JSON, validates the payload, stores the reaction in an in-memory list per content hash (newest first, capped at 200 per item), and fires the `ReactionReceived` event.

---

## Profile Sync

Profile sync uses `PacketType.ProfileSync` (numeric value 23). It runs on two triggers: on handshake completion (so nearby peers always have a current profile) and whenever the local profile is created or updated.

**Outbound.** `ProfileSyncService.SyncLocalProfileAsync` retrieves the local `MediaProfile`, serialises it to UTF-8 JSON using snake_case naming, and broadcasts a packet with an empty `DestinationUhid` (broadcast to all connected peers), TTL = 7:

```csharp
var packet = new MeshPacket
{
    Type            = PacketType.ProfileSync,
    SourceUhid      = _sender.LocalUhid,
    DestinationUhid = string.Empty,   // broadcast
    Ttl             = 7,
    Payload         = payload,
};
await _sender.BroadcastAsync(packet, ct);
```

**Inbound.** `HandleSyncPacketAsync` deserialises the payload, validates the UHID, ignores packets that originated from the local node (echo suppression), and stores the profile in the remote profile cache. The `ProfileReceived` event is fired so the UI can update immediately.

**Profile wire format.** The payload is a `MediaProfile` object matching the schema in `wire-format.md`. Key identity fields:

```json
{
  "uhid":           "uhid-alice-0001",
  "display_name":   "Alice Aether",
  "avatar_hash":    "sha256-of-avatar-bytes",
  "aethermesh_tag":     "@alice",
  "is_verified":    true,
  "joined_at_ms":   1700000000000
}
```

`avatar_hash` is the SHA-256 hex digest of the avatar image bytes. Peers that want to display the avatar request that content chunk by hash via `IContentService`.

---

## Content Announce

When a creator publishes media, `IContentService.AnnounceAsync` broadcasts a `ContentDescriptor` to the mesh. Every device that receives it caches and re-broadcasts it (flood with deduplication).

**FeedAggregator** subscribes to `IContentService.ContentAnnounced` and, on receipt, creates a `MediaFeedItem` and inserts it at the head of the local feed (newest-first, deduplicated by `content_hash`):

```csharp
_content.ContentAnnounced += OnContentAnnounced;
```

The descriptor carries the root hash, MIME type, total byte count, chunk count, chunk manifest, and creation timestamp. The `FeedAggregator` maps these fields to a `MediaContent` surrogate; the full `MediaContent` record (with title, codec, tags, and duration) is resolved once a chunk containing the embedded metadata has been fetched.

---

## Feed Construction

The local feed is assembled from two sources with no server involvement:

1. **Live streams.** `IStreamingService.StreamAnnounced` fires whenever a nearby peer starts broadcasting. `FeedAggregator.OnStreamAnnounced` checks `ISocialGraph.IsFollowingAsync` for the session's `PublisherUhid` and, if the creator is followed, inserts a live `MediaFeedItem` (`IsLive = true`, `DurationMs = 0`). When the stream ends (`StreamEnded` event) the item is updated in-place to `IsLive = false`.

2. **VOD content.** `IContentService.ContentAnnounced` fires for every content descriptor received from the mesh. All descriptors are surfaced in the feed (filtering by followed creator happens when the UHID is resolvable from the announcing peer's handshake identity).

Feed items are deduplicated by `content_hash`. The feed is capped at 500 items; oldest entries are evicted when the cap is reached. All state is in-memory and local — no server query is ever issued.

---

## Nearby Creator Discovery

`DiscoveryService` hooks into `IHandshakeService.PeerNegotiated`. Each time a peer completes a handshake it advertises its `NodeCapabilities`. If the peer advertises the `"streaming"` capability tag, `DiscoveryService` resolves the peer's `MediaProfile` via `IProfileService` and fires `CreatorDiscovered`. If the profile is not yet cached (the peer has not synced it yet) a synthetic minimal profile is created so the UI can react immediately.

```csharp
var hasStreaming =
    caps.Capabilities.Contains("streaming") ||
    caps.Capabilities.Contains("Streaming");
```

The resulting list of nearby creators is available via `IDiscoveryService.GetNearbyCreatorsAsync()` and is used to populate the "Nearby" feed tab without any network round-trip.

---

## Cross-Language Interoperability

All 8 SDK implementations produce and consume the same JSON wire format. Cross-language compatibility is verified in CI by shared fixture files in `tests/cross-language/`. See `wire-format.md` for per-language serialisation notes (field naming, timestamp types, enum representation).
