# Aether Media — Developer Quickstart

This guide gets you from a fresh clone to a running demo in each language. No internet connection is required after cloning — all integration tests run against in-process fakes of the mesh transport.

---

## Prerequisites

| Toolchain | Minimum version | Used by |
|-----------|----------------|---------|
| .NET SDK | 10.0 | C# reference implementation, all C# tests |
| Node.js | 22 LTS | TypeScript web player |
| Go | 1.23 | Go social graph library |
| Rust | 1.79 (stable) | Rust feed engine |
| Python | 3.12 | Python plugin engine and metadata library |
| Kotlin / Gradle | JVM 21, Gradle 8 | Kotlin/Android social graph |
| Swift | 5.10 (Xcode 15+) | Apple platform player |
| C toolchain | C11 compiler + CMake 3.25 | Embedded / microcontroller targets |

---

## Clone and Build

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media
```

### C# (reference implementation)

Build all projects in the solution and run the full unit-test suite:

```bash
dotnet build AetherNetMedia.slnx
dotnet test
```

The solution includes all `src/`, `tests/`, and `samples/` projects. Build output goes to `bin/` inside each project directory.

### TypeScript

```bash
cd typescript
npm install
npm run build   # compiles to dist/
npm test        # Vitest unit tests
```

### Rust

```bash
cd rust
cargo build
cargo test
```

### Go

```bash
cd go
go build ./...
go test ./...
```

### Python

```bash
cd python
pip install -e ".[dev]"
python -m pytest
```

### Kotlin / JVM

```bash
cd kotlin
./gradlew build test
```

### Swift

```bash
cd swift
swift build
swift test
```

### C

```bash
cd c
cmake -B build
cmake --build build
ctest --test-dir build
```

---

## DI Registration (C#)

All Aether Media subsystems are registered through a single extension method on `IServiceCollection`. Each subsystem is opt-in; use only what your application needs.

```csharp
using AetherNet.Media.DependencyInjection;

services.AddAetherNetMedia(media =>
    media.AddIdentity()    // IProfileService, IProfileSyncService, IAvatarService
         .AddContent()     // IMediaLibrary, IContentCache, IMetadataResolver,
                           //   IThumbnailService, IMediaLibraryScanner
         .AddSocial()      // ISocialGraph, IFeedAggregator,
                           //   IReactionService, IDiscoveryService
         .AddStreaming()   // ILiveStreamPublisher, IWatchPartyCoordinator, IAbrController
         .AddAI());        // IContentRanker, ICreatorReputationView, IContentModerator
```

`AddSocial()` depends on `IDtnService` and `IMeshSender` from the `aether-protocol` library. In production, wire these from `AetherNet.DependencyInjection`. For demos and tests, use the no-op stubs shown below.

All registrations use `TryAddSingleton`, so you can override any service by registering your own implementation before calling `AddAetherNetMedia`.

---

## Running the C# Console Demo

The interactive console demo exercises all five subsystems without a live mesh. It uses no-op stubs for `IDtnService` and `IMeshSender`:

```bash
dotnet run --project samples/AetherNet.Media.Demo.Console
```

Expected output:

```
────────────────────────────────────────────────────────────
  Aether Media — Console Demo
────────────────────────────────────────────────────────────

  Library
    Items in library:  1
    Title:             Sample Video
    Duration:          2:05
    Codec / MIME:      H.264  (video/mp4)
    Creator UHID:      KXJB7-MN2P4
    Is video:          True
    Tags:              [demo, sample, aether]

  Social Graph
    Following "KXJB7-MN2P4": True
    Following list (1):
      • KXJB7-MN2P4

  Feed
    Items in feed:    0

  ...

  Done. Mesh not required — demo ran fully offline.
────────────────────────────────────────────────────────────
```

The feed is empty in the console demo because no `ContentAnnounced` events are raised by the no-op stubs. In a real deployment, the feed populates as the mesh delivers descriptors from followed creators.

---

## Running the TypeScript Web Player

```bash
cd typescript
npm run dev
```

This starts the Vite dev server (default port 5173). Open `http://localhost:5173` in a browser to see the player. To load a stream:

```typescript
import { AetherNetMediaPlayer } from '@bhengubv/aether-media';

const video  = document.querySelector('video') as HTMLVideoElement;
const player = new AetherNetMediaPlayer(video);

// HLS stream via HTTP relay
await player.load('https://relay.aethernet.network/media/stream/uhid-alice-0001.m3u8');
await player.play();

// Feed raw mesh segments directly into MSE (used when mesh is available)
player.feedSegment(encodedBytes, 'video/mp4; codecs="avc1.42E01E"');
```

To consume the social feed:

```typescript
import { FeedClient } from '@bhengubv/aether-media';

const client = new FeedClient('https://relay.aethernet.network/media');
const items  = await client.getFeed(20, 0);   // limit=20, offset=0

for (const item of items) {
    console.log(item.content.content_hash, item.content.title);
}
```

---

## Running the Social Protocol Integration Test

The `AetherNet.Media.Social.Tests` project contains a full end-to-end integration test that verifies the follow → publish → feed flow without any network access. All mesh transport is simulated by in-process fakes.

```bash
dotnet test tests/AetherNet.Media.Social.Tests
```

The integration test (`SocialProtocolIntegrationTests`) covers:

- Node A follows Node B → `IsFollowingAsync` returns `true`
- Node B publishes a `ContentDescriptor` → Node A's feed receives the item within 5 seconds
- Node B goes live → Node A's feed receives a live item within 5 seconds (only when following)
- Unfollowed creator's stream is ignored by Node A's feed
- Duplicate content announcements are deduplicated (one feed item per hash)
- Stream ending marks the feed item `IsLive = false`
- `MarkWatchedAsync` increments `WatchCount` on the correct feed item

Run a specific test by name:

```bash
dotnet test tests/AetherNet.Media.Social.Tests \
  --filter "DisplayName~NodeB publishes content"
```

---

## Wiring the Real Mesh (Production)

Replace the no-op stubs with implementations from `aether-protocol`:

```csharp
// Instead of:
services.AddSingleton<IDtnService, NoOpDtnService>();
services.AddSingleton<IMeshSender, NoOpMeshSender>();

// Use:
services.AddAetherNetProtocol(aether =>
    aethernet.AddDtn()
          .AddMesh()
          .AddHandshake()
          .AddStreaming()
          .AddContent());

services.AddAetherNetMedia(media =>
    media.AddIdentity()
         .AddContent()
         .AddSocial()
         .AddStreaming());
```

Once wired to the real transport, `SocialGraph.FollowAsync` sends DTN bundles over BLE/Wi-Fi Direct, `FeedAggregator` populates from real mesh events, and live streams flow from a publisher device to every subscribed device in radio range.

---

## Cross-Language Interop

All 8 SDKs exchange data in the canonical wire format documented in `docs/articles/wire-format.md`. Field names are `snake_case`, timestamps are Unix milliseconds (`integer`, `_ms` suffix), and the reaction `type` field is a lowercase string (`"like"`, `"share"`, `"comment"`, `"super_react"`).

Cross-language fixture files live in `tests/cross-language/` and are validated in CI. If you add a new field to a wire model, update all 8 implementations and the fixture files together.

---

## Next Steps

- `docs/articles/architecture.md` — full system architecture and layer overview
- `docs/articles/social-protocol.md` — social wire protocol: follow, reaction, profile sync, feed construction
- `docs/articles/content-addressing.md` — SHA-256 hashing, chunking, DTN delivery, LRU cache
- `docs/articles/wire-format.md` — canonical JSON wire format with per-language serialisation notes
