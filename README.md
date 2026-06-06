```
  ╔═╗ ╔═╗ ╔╦╗ ╦ ╦ ╔═╗ ╦═╗   ╔╦╗ ╔═╗ ╔╦╗ ╦ ╔═╗
  ╠═╣ ║╣   ║  ╠═╣ ║╣  ╠╦╝   ║║║ ║╣   ║║ ║ ╠═╣
  ╩ ╩ ╚═╝  ╩  ╩ ╩ ╚═╝ ╩╚═   ╩ ╩ ╚═╝ ═╩╝ ╩ ╩ ╩
  decentralised social media · no internet required
```

[![MIT License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![npm version](https://img.shields.io/npm/v/@bhengubv/aether-media.svg)](https://www.npmjs.com/package/@bhengubv/aether-media)
[![Build](https://github.com/bhengubv/aether-media/actions/workflows/ci.yml/badge.svg)](https://github.com/bhengubv/aether-media/actions/workflows/ci.yml)

[English](README.md) · [Français](docs/i18n/fr/README.md) · [Español](docs/i18n/es/README.md) · [العربية](docs/i18n/ar/README.md) · [中文简体](docs/i18n/zh-CN/README.md) · [日本語](docs/i18n/ja/README.md) · [Deutsch](docs/i18n/de/README.md) · [Português (BR)](docs/i18n/pt-BR/README.md) · [Русский](docs/i18n/ru/README.md) · [فارسی](docs/i18n/fa/README.md) · [한국어](docs/i18n/ko/README.md)

> A decentralised social media network and player built on the Aether mesh protocol.
> No internet required. No central server. No corporate owner.

Two phones in the same room — no Wi-Fi, no mobile data — can discover each other,
share media, stream live video, and react socially over BLE, Wi-Fi Direct, NearLink,
LoRa, or HTTP relay.

---

## Why Aether Media?

**Stream a live concert to the audience's phones — no internet, no CDN, no streaming fee.**

The performer's device broadcasts over Wi-Fi Direct. Every phone in range receives the stream and relays it further over BLE. Reactions (likes, super-reacts, comments at the exact playback position) travel back the same way. No buffering from a data centre 10,000 km away. No account required for the audience.

```
  [Performer] ──WiFi Direct──▶ [Row 1] ──BLE──▶ [Row 2] ──NearLink──▶ [Row 3]
                 1080p live           relayed, encrypted        relayed, encrypted
```

**Follow a creator who is offline. Receive their content when they come back into range.**

Follows are delivered over Aether's DTN (Delay-Tolerant Networking) store-and-forward layer. If the creator's device is not reachable now, the follow intent waits — up to 72 hours — and delivers the moment a route opens. No push-notification infrastructure, no app server.

**Watch a film together across the mesh.**

Someone on the mesh has the file. The Watch Party coordinator synchronises play, pause, and seek across every device with RTT compensation. Reactions fire in real time at the exact timestamp in the video. If the host's device goes offline mid-film, the session migrates to the next available peer automatically.

---

### Comparison with existing players and networks

| Capability | VLC | Spotify | Streambert | Aether Media |
|---|:---:|:---:|:---:|:---:|
| Works offline (no internet) | Playback only | No | No | **Yes — discover, stream, react** |
| Works without app account | Yes | No | No | **Yes** |
| Mesh relay (hop through devices) | No | No | No | **Yes** |
| Live streaming, no CDN | No | No | Partial | **Yes — BLE / Wi-Fi Direct / NearLink** |
| Social reactions on timeline | No | No | No | **Yes — at exact playback position** |
| Follow graph without server | No | No | No | **Yes — DTN-delivered** |
| Sub-second latency in same room | N/A | No | No | **Yes — NearLink 20 µs** |
| Runs on microcontroller / C11 | No | No | No | **Yes — C implementation** |
| 8-language SDK | No | No | No | **Yes** |

---

## How It Works

**Step 1 — Mesh discovery.** Devices broadcast their Aether Tag (a short identity handle like `@sam.5jk2`) over BLE advertisements and Wi-Fi Direct probe responses. No IP address or account is needed. The transport layer automatically promotes NearLink (600 m, 12 Mbps, 20 µs) on devices that have it, falls back to Wi-Fi Direct (200 m, 250 Mbps), then BLE (100 m, 1 Mbps), then LoRa-over-BLE (1.3 km), and finally HTTP relay as a last resort.

**Step 2 — Content addressing.** Every piece of media is identified by its SHA-256 content hash — not by a URL or a server path. A `ContentDescriptor` (hash + name + MIME type + chunk manifest) is broadcast over the mesh. Any device that has the file can serve chunks to any device that needs them. There is no origin server. Files can be assembled from fragments held by different peers simultaneously, BitTorrent-style.

**Step 3 — Social layer.** Follows, reactions, and profile updates are encoded as signed JSON payloads and sent as either DTN bundles (for offline-tolerant delivery) or best-effort `MeshPacket`s (for low-latency reactions during live streams). The `SocialGraph` tracks who you follow. The `FeedAggregator` listens for `StreamAnnounce` and `ContentAnnounce` packets from followed creators and assembles a chronological feed — entirely from mesh events, with no feed server.

---

## What Aether Features Are Used

Aether Media is built on top of [aether-protocol](https://github.com/bhengubv/aether-protocol) and uses these interfaces:

| Aether Interface | Package | How Aether Media Uses It |
|---|---|---|
| `ITransportService` | `AetherNet.Transport` | Sends encoded video/audio frames, reactions, and follow intents over the mesh (BLE / Wi-Fi Direct / NearLink / LoRa / HTTP relay) |
| `IStreamingService` | `AetherNet.Streaming` | Broadcasts `StreamAnnounce` when going live; `FeedAggregator` subscribes to `StreamAnnounced` and `StreamEnded` events to maintain the live-stream feed |
| `IContentService` | `AetherNet.Content` | Publishes `ContentDescriptor`s for uploaded media; `FeedAggregator` subscribes to `ContentAnnounced` for VOD discovery |
| `IDtnService` | `AetherNet.Dtn` | Delivers follow intents durably to offline creators; bundles wait up to 72 h for a route |
| `IMeshSender` | `AetherNet.Messaging` | Sends best-effort unfollow packets and live reactions over the mesh without DTN overhead |
| `IRoutingService` | `AetherNet.Routing` | Route-aware delivery of social packets; AODV-style RREQ/RREP with Ed25519-signed route replies |
| `SignalProtocolService` | `AetherNet.Security` | End-to-end encrypts direct messages, profile sync payloads, and private channel content with X3DH + Double Ratchet |
| `IAdaptiveBitrateController` | `AetherNet.Streaming` | Selects the highest sustainable quality rung (H.264 / H.265 / VP8) based on live bandwidth estimates from the active transport |

---

## 8-Language SDK

Aether Media ships implementations in 8 languages so it runs on every platform in the ecosystem.

| Language | Directory | Platform | Media Engine | Role |
|---|---|---|---|---|
| C# (.NET 10) | `src/` | Windows · Linux · macOS | LibVLC (LibVLCSharp) | Reference implementation, full DI, NuGet packages |
| TypeScript | `typescript/` | Browser · Node 20 | HLS.js · Shaka Player | Web player, feed client, social SDK |
| Python | `python/` | Any Python 3.11+ | mutagen (metadata) | Plugin engine, scripting, metadata processing |
| Rust | `rust/` | Any Rust target | `rodio` (audio) | High-performance feed engine, benches |
| Go | `go/` | Any Go 1.22 target | — | Social graph library |
| Kotlin | `kotlin/` | JVM 21 · Android | media3 / ExoPlayer (Android) | Android player; JVM social graph for server-side use |
| Swift | `swift/` | macOS 13+ · iOS 16+ | AVFoundation | Apple platform player |
| C | `c/` | Any C11 target | — | Embedded / microcontroller feed and social models |

All 8 implementations share the same wire format as `aether-protocol` and produce
interoperable social packets verified by cross-language fixtures in CI.

---

## Quick Start

### C# Desktop (Windows / Linux / macOS)

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media
dotnet run --project samples/AetherMedia.Demo.Console
```

Register all subsystems:

```csharp
services.AddAetherNetMedia(media =>
    media.AddIdentity()
         .AddContent()
         .AddSocial()
         .AddStreaming()
         .AddAI());
```

Resolve and use:

```csharp
var library = provider.GetRequiredService<IMediaLibrary>();
var graph   = provider.GetRequiredService<ISocialGraph>();
var feed    = provider.GetRequiredService<IFeedAggregator>();

var content = new MediaContent(
    ContentHash:  "a3f9...",
    Title:        "Sample Video",
    DurationMs:   125_000,
    Codec:        "H.264",
    ContentType:  "video/mp4",
    CreatorUhid:  "KXJB7-MN2P4",
    SizeBytes:    15_728_640,
    CreatedAt:    DateTime.UtcNow,
    ThumbnailHash: null,
    Tags:         ["demo", "sample"]);

await library.AddAsync(content);
await graph.FollowAsync("KXJB7-MN2P4");
await feed.StartAsync();
```

### TypeScript (Browser)

```typescript
import { AetherNetMediaPlayer } from '@bhengubv/aether-media';

const video  = document.querySelector('video') as HTMLVideoElement;
const player = new AetherNetMediaPlayer(video);

// Load an HLS stream published by a peer on the mesh
await player.load('aether://stream/KXJB7-MN2P4');
await player.play();

// Feed raw mesh segments directly into the MSE pipeline
player.feedSegment(encodedBytes, 'video/mp4; codecs="avc1.42E01E"');
```

Feed client with local storage cache:

```typescript
import { FeedClient } from '@bhengubv/aether-media';

const client = new FeedClient('https://relay.aethernet.network/media');
const items  = await client.getFeed(20, 0);   // limit, offset

for (const item of items) {
    console.log(item.content.title, item.likeCount);
}

await client.markWatched('a3f9...', 45_000);  // contentHash, ms watched
```

### Python (Plugin)

```python
from aethermedia.plugins.base import AetherNetMediaPlugin
from aethermedia.models import MediaContent, MediaReaction

class MyPlugin(AetherNetMediaPlugin):
    @property
    def name(self) -> str:
        return "My Plugin"

    @property
    def version(self) -> str:
        return "1.0.0"

    def on_content_loaded(self, content: MediaContent) -> None:
        print(f"Loaded: {content.title}  ({content.formatted_duration})")

    def on_reaction_received(self, reaction: MediaReaction) -> None:
        print(f"Reaction: {reaction.type.name} at {reaction.position_ms} ms")
```

### Kotlin (Android / JVM)

```kotlin
import aethermedia.social.SocialGraph

val graph = SocialGraph()
graph.follow("KXJB7-MN2P4")
println(graph.isFollowing("KXJB7-MN2P4"))  // true
println(graph.getFollowing())               // ["KXJB7-MN2P4"]
graph.unfollow("KXJB7-MN2P4")
println(graph.count)                        // 0
```

### Rust

```rust
use aethermedia::feed::{FeedStore, FeedEntry};

let mut store = FeedStore::new(500);
let entry = FeedEntry {
    content_hash: "a3f9ee...".to_string(),
    title:        "Sample".to_string(),
    creator_uhid: "KXJB7-MN2P4".to_string(),
    duration_ms:  125_000,
    like_count:   0,
    is_live:      false,
};
store.add(entry);
println!("Feed has {} item(s)", store.len());
```

### Go

```go
import "github.com/bhengubv/aether-media/go/social"

g := social.NewSocialGraph()
g.Follow("KXJB7-MN2P4")
fmt.Println(g.IsFollowing("KXJB7-MN2P4")) // true
fmt.Println(g.Following())                 // [KXJB7-MN2P4]
```

### Swift

```swift
import AetherNetMedia

let graph = SocialGraph()
try await graph.follow(uhid: "KXJB7-MN2P4")
let following = try await graph.following()
print(following) // ["KXJB7-MN2P4"]
```

### C

```c
#include "aethermedia/social.h"

aethernet_social_graph_t *graph = aethernet_social_graph_create();
aethernet_social_graph_follow(graph, "KXJB7-MN2P4");
printf("Following: %d\n", aethernet_social_graph_is_following(graph, "KXJB7-MN2P4")); // 1
aethernet_social_graph_destroy(graph);
```

---

## Social Protocol

The social layer has no server. Every follow, unfollow, content announcement, and
reaction is a signed `MeshPacket` or `DtnBundle` that travels over whatever radio is
available.

**Follows** are wrapped in a `FollowIntentPayload` (UTF-8 JSON), encrypted with the
target creator's Signal Protocol session key (X3DH + Double Ratchet), and committed
as a `DtnBundle` addressed to the target UHID. The DTN layer stores the bundle locally
and delivers it over the mesh whenever a path to the target opens — even if that takes
hours. The creator's device receives the bundle, verifies the signature, and increments
their follower count. All of this happens without any central server knowing about the
relationship.

**Content announcements** are `ContentDescriptor` packets broadcast by the publishing
device. Every device that receives the descriptor caches it and re-broadcasts it to
nearby peers (mesh flood with dedup). The `FeedAggregator` on each device listens for
these broadcasts and surfaces new content from followed creators in the local feed.

**Reactions** (like, share, super-react, comment) carry the content hash, the reaction
type, and the exact playback position in milliseconds. They travel as best-effort
`MeshPacket`s addressed to the creator's UHID — routed by AODV with signed route
replies, so no fake destination can intercept them. During a live stream, reactions
are aggregated and displayed in real time on the publisher's device without leaving
the mesh.

**Profile sync** uses the same DTN mechanism as follows. When a creator updates their
display name, avatar, or bio, the new `MediaProfile` is signed with their Ed25519
identity key, serialised, and broadcast as a DTN bundle. Any device that receives
it — directly or via relay — verifies the signature and updates its local cache. A
profile update made while offline reaches followers the next time any of them come
within radio range.

---

## Repository Structure

```
aether-media/
  src/
    AetherMedia.Core/            Domain models and interfaces (MediaContent, IMediaLibrary, etc.)
    AetherMedia.Identity/        Profile management, avatar, profile sync
    AetherMedia.Content/         Media library scanner, metadata resolver, LRU cache, thumbnails
    AetherMedia.Social/          SocialGraph, FeedAggregator, ReactionService, DiscoveryService
    AetherMedia.Streaming/       LiveStreamPublisher, WatchPartyCoordinator, AbrController
    AetherMedia.AI/              ContentRanker, ContentModerator, CreatorReputationView
    AetherMedia.DependencyInjection/  AddAetherNetMedia() extension + AetherNetMediaBuilder fluent API
    AetherMedia.Desktop/         LibVLCSharp integration for Windows / Linux / macOS
  samples/
    AetherMedia.Demo.Console/    Interactive console demo showing all subsystems
    AetherMedia.RelayTest/       HTTP relay round-trip test (requires AetherNet.RelayServer)
  tests/
    AetherMedia.Core.Tests/      Unit tests for domain models and InMemoryMediaLibrary
    AetherMedia.Social.Tests/    Unit tests for SocialGraph and FeedAggregator
  typescript/                     TypeScript web player and social SDK (@bhengubv/aether-media)
    src/
      player/   AetherNetMediaPlayer (HLS.js + Shaka Player + native MSE)
      social/   FeedClient, ReactionClient
      identity/ ProfileClient
      streaming/ AetherNetStreamClient
      models/   TypeScript mirrors of the C# domain models
  python/                         Python plugin engine and metadata library (aether-media on PyPI)
    aethermedia/
      plugins/  AetherNetMediaPlugin base class, PluginHost
      metadata/ Tag reader/writer (mutagen wrapper)
      cli/      Command-line entry points
  rust/                           Rust feed engine (aether-media on crates.io)
    src/
      feed/     FeedStore, FeedEntry
      social/   SocialGraph, follow/unfollow
      streaming/ StreamAnnounce, segment models
      player/   Audio playback via rodio
  go/                             Go social graph library (github.com/bhengubv/aether-media/go)
    social/     SocialGraph
    player/     Player models
    feed/       Feed models
    streaming/  Stream models
  kotlin/                         Kotlin/JVM social graph + Android ExoPlayer integration
    src/main/kotlin/
      social/   SocialGraph (ConcurrentHashMap-backed, JVM and Android)
      feed/     Feed models
      player/   ExoPlayer integration (Android; JVM tests use core only)
      content/  Content descriptor models
      streaming/ Stream session models
    android/    Gradle Android module with media3-exoplayer dependency
  swift/                          Swift / Apple platform player (SwiftPM package)
    Sources/AetherNetMedia/
      social/   SocialGraph (actor-based, Swift Concurrency)
      player/   AVFoundation player
      feed/     Feed models
      streaming/ Stream models
  c/                              C11 feed and social models for embedded targets
    include/aethermedia/         Public headers
    src/                          Implementations
    tests/                        CTest-based test suite
  android/                        Android Gradle modules
    media/      Main media module (Kotlin + Jetpack)
    media-tv/   Android TV variant
  docs/                           Architecture notes and design decisions
```

---

## Building

### C#

```bash
dotnet build AetherNetMedia.slnx
dotnet test
```

### TypeScript

```bash
cd typescript && npm install && npm run build && npm test
```

### Python

```bash
cd python && pip install -e ".[dev]" && pytest
```

### Rust

```bash
cd rust && cargo build && cargo test
```

### Go

```bash
cd go && go build ./... && go test ./...
```

### Kotlin

```bash
cd kotlin && ./gradlew build test
```

### Swift

```bash
cd swift && swift build && swift test
```

### C

```bash
cd c && cmake -B build && cmake --build build && ctest --test-dir build
```

### Android

```bash
cd android/media && ./gradlew assembleDebug
```

---

## Licence

MIT — free forever. Codec engine (LibVLC / FFmpeg / AVFoundation / media3 / ExoPlayer)
is used via LGPL and Apache 2.0 respectively, unchanged. See [LICENSE](LICENSE).
