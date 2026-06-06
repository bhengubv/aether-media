# Aether Media — System Architecture

Aether Media is a decentralised social media platform for audio and video. It requires no internet connection, no central server, and no corporate intermediary. Two devices in the same room discover each other over BLE or Wi-Fi Direct, exchange content addressed by SHA-256 hash, stream live video peer-to-peer, and exchange social reactions in real time — all without touching a data centre.

---

## The Three Layers

```
┌────────────────────────────────────────────────────────────────┐
│  Application Layer                                             │
│  Desktop (Avalonia/LibVLCSharp) · Mobile (iOS/Android) ·      │
│  Web (TypeScript HLS.js / Shaka Player)                        │
├────────────────────────────────────────────────────────────────┤
│  SDK Layer                                                     │
│  C# · Kotlin · Swift · Rust · TypeScript · Go · Python · C    │
├────────────────────────────────────────────────────────────────┤
│  Transport Layer (Aether mesh protocol)                        │
│  BLE · Wi-Fi Direct · NearLink · LoRa · HTTP relay             │
└────────────────────────────────────────────────────────────────┘
```

**Transport layer.** The `aether-protocol` library provides the mesh fabric. Devices broadcast their Aether Tag (a short handle like `@alice.5jk2`) over BLE advertisements and Wi-Fi Direct probe responses. The transport promotes the fastest available radio: NearLink (600 m, 12 Mbps, 20 µs RTT) when present, falling back to Wi-Fi Direct (200 m, 250 Mbps), BLE (100 m, 1 Mbps), LoRa-over-BLE (1.3 km), and finally an HTTP relay for internet-connected edge cases. All frames are Ed25519-signed and encrypted with X3DH + Double Ratchet.

**SDK layer.** Eight language implementations share an identical wire format (defined in `docs/articles/wire-format.md`). The C# project is the reference implementation; the others are first-class citizens used in production on Android, iOS, browser, server-side JVM, embedded microcontrollers, and scripting environments.

**Application layer.** The C# Desktop application uses Avalonia UI with LibVLCSharp for media decoding. The TypeScript web player supports HLS (via HLS.js), DASH (via Shaka Player), and raw MSE segment feeding for direct mesh delivery. Mobile is delivered through Kotlin/media3 on Android and AVFoundation on Apple platforms.

---

## Content Addressing

Every piece of media is identified by its SHA-256 hash — not by a URL or a server path. This is the cornerstone of the content layer.

When a creator adds a file to their library, `MediaLibraryScanner` reads the raw encoded bytes, computes their SHA-256 digest, and builds a `ContentDescriptor` that records the root hash, total byte count, chunk manifest, and MIME type. The descriptor is published to the mesh via `IContentService.AnnounceAsync`. Any device that receives the descriptor can request individual chunks from any peer that holds them — simultaneously, BitTorrent-style.

The primary model, `MediaContent`, carries the content hash as its primary key:

```csharp
var content = new MediaContent(
    ContentHash:  "a3f9ee2c84b1d6f4230c5d987654e32a1b0c9d8e7f6a5b4c3d2e1f0a9b8c7d6",
    Title:        "Aether Launch Keynote",
    DurationMs:   5_025_000,
    Codec:        "H.264",
    ContentType:  "video/mp4",
    CreatorUhid:  "uhid-alice-0001",
    SizeBytes:    150_000_000,
    CreatedAtMs:  DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    ThumbnailHash: null,
    Tags:         ["aether", "launch"]);
```

The thumbnail is a separately addressed chunk. When the scanner extracts embedded artwork it hashes it independently and sets `ThumbnailHash` on the `MediaContent` record. A viewer fetching the thumbnail requests it by that hash alone, without re-downloading the full media file.

---

## The Social Layer

The social layer has no server. Every follow, unfollow, reaction, and profile update is a signed message routed over the mesh.

**SocialGraph** manages the local follow graph. `FollowAsync` serialises a `FollowIntentPayload` to UTF-8 JSON and commits it to `IDtnService` (Delay-Tolerant Networking). The DTN layer stores the bundle locally and delivers it whenever a route to the target UHID opens — even if that takes hours. Unfollows are best-effort: a `PacketType.WatchReaction` packet is broadcast immediately, with reconciliation on next encounter if the target is unreachable at the time.

**ReactionService** sends `PacketType.WatchReaction` packets directly to the content creator's UHID via `IMeshSender`. Each reaction carries the content hash, reaction type (`"like"`, `"share"`, `"comment"`, `"super_react"`), and the exact playback position in milliseconds so it can be anchored on the creator's timeline.

**ProfileSyncService** broadcasts `PacketType.ProfileSync` (type 23) packets containing the local `MediaProfile` as signed JSON. Any peer that receives the packet updates its local cache. Profile updates made offline propagate automatically the next time any nearby device relays the packet forward.

**FeedAggregator** listens to two mesh event streams: `IStreamingService.StreamAnnounced` for live content and `IContentService.ContentAnnounced` for VOD. It assembles a chronological feed (capped at 500 items, newest first, deduplicated by content hash) entirely from mesh events — no feed server, no algorithmic injection from a third party.

---

## Live Streaming

`ILiveStreamPublisher` wraps `IStreamingService` to manage the publisher lifecycle: `StartPublishingAsync` opens a `StreamSession`, `PublishFrameAsync` pushes encoded segments as they are captured, and `StopPublishingAsync` signals the end to all subscribers.

`AbrController` tracks available bandwidth with an exponential moving average (α = 0.3) and selects the highest bitrate rung (200 / 400 / 800 / 1200 / 2500 / 5000 Kbps) that fits within 80% of the current estimate. When the rung changes it notifies `IStreamingService.UpdateBandwidthEstimate`. If the bitrate drops so severely that a keyframe is needed, the controller cycles the subscription (unsubscribe + re-subscribe) to force the publisher to emit a new keyframe.

---

## C# Project Responsibilities

| Project | Responsibility |
|---------|----------------|
| `AetherMesh.Media.Core` | Domain models (`MediaContent`, `MediaFeedItem`, `MediaReaction`, `MediaProfile`, `LiveStream`) and core interfaces (`IMediaLibrary`, `IFeedAggregator`, `ISocialGraph`) |
| `AetherMesh.Media.Identity` | Local profile store, avatar management, `ProfileSyncService` for mesh broadcast of profile updates |
| `AetherMesh.Media.Content` | Library scanner (SHA-256 hashing, metadata resolution, thumbnail extraction), `LruContentCache` (500 MiB default, O(1) eviction) |
| `AetherMesh.Media.Social` | `SocialGraph` (DTN-backed follow), `FeedAggregator` (mesh event aggregation), `ReactionService` (live reaction routing), `DiscoveryService` (nearby creator detection) |
| `AetherMesh.Media.Streaming` | `LiveStreamPublisher`, `WatchPartyCoordinator`, `AbrController` |
| `AetherMesh.Media.AI` | `ContentRanker`, `ContentModerator`, `CreatorReputationView` — on-device AI curation with no data leaving the device |
| `AetherMesh.Media.Desktop` | Avalonia UI host with LibVLCSharp media engine |
| `AetherMesh.Media.DependencyInjection` | `AddAetherMeshMedia()` extension and `AetherMeshMediaBuilder` fluent API |

---

## DI Builder Pattern

All subsystems are opt-in. `TryAddSingleton` ensures host applications can substitute their own implementations simply by registering them first:

```csharp
services.AddAetherMeshMedia(media =>
    media.AddIdentity()
         .AddContent()
         .AddSocial()
         .AddStreaming()
         .AddAI());
```

Optional subsystems include `.AddDistribution()` (mesh-first app distribution with QR share), `.AddLocalLibrary()` (privacy-first local media management with subtitle download), and `.AddReel(localUhid)` (short-video platform with on-device For You feed).

---

## TypeScript Web Player

The TypeScript package (`@bhengubv/aether-media`) connects through two paths. For peers running an HTTP relay, the `FeedClient` fetches feed items and marks watch progress over standard REST. For direct mesh delivery, `AetherMeshMediaPlayer.feedSegment()` accepts raw encoded bytes and pipes them into the browser's Media Source Extensions (MSE) pipeline:

```typescript
const player = new AetherMeshMediaPlayer(document.querySelector('video')!);
await player.load('aether://stream/uhid-alice-0001'); // HLS relay fallback

// Or feed raw mesh segments directly:
player.feedSegment(encodedBytes, 'video/mp4; codecs="avc1.42E01E"');
```

When a mesh peer is not reachable via relay, the player degrades gracefully to native `<video src>` assignment so existing MP4/WebM files still play.

---

## Further Reading

- `docs/articles/wire-format.md` — canonical JSON wire format shared by all 8 SDKs
- `docs/articles/social-protocol.md` — social layer wire protocol in depth
- `docs/articles/content-addressing.md` — content hash, chunking, and DTN delivery
- `docs/articles/getting-started.md` — quickstart for every language
