# Changelog

All notable changes to `aether-media` are documented in this file.

Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning: [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.1.0] — 2026-05-22

Phase 2 CircleAI integration — watch-history personalisation, velocity burst
detection, and per-segment AI transport-bias in the ABR controller.

### Added

**AI layer (`Aether.Media.AI`)**
- `IWatchHistoryStore` — interface for recording and retrieving per-viewer
  content completion rates.
- `InMemoryWatchHistoryStore` — thread-safe, insertion-ordered EWMA
  implementation (α = 0.4); evicts oldest entry at 1 000 items per viewer.
- `ContentRanker` now accepts `IWatchHistoryStore` as a required fourth
  dependency; watch-history signal (15 %) blended into five-signal composite
  score alongside reputation (30 %), AI bias (20 %), recency (20 %), and
  engagement (15 %). Neutral 0.5 when no viewer history exists.
- `IContentModerator.AssessSocialPacketAsync` — new interface method combining
  an AI assessment with a sliding-window velocity burst detector; returns the
  higher of the two signals and never throws.
- `ContentModerator` velocity burst detection: `WatchReaction` threshold
  20 events / 30 s; all other social packet types threshold 5 events / 60 s.
  Operates independently of AI availability.

**Streaming layer (`Aether.Media.Streaming`)**
- `AbrController` now accepts an optional `IAetherAiProvider` dependency.
  Transport biases are fetched at most once every 5 s, averaged, clamped to
  [0.5, 1.5], and applied to the raw bandwidth sample before the EMA update.
  Falls back to neutral (1.0) when AI is unavailable or throws.

**DI (`Aether.Media.DependencyInjection`)**
- `AddAI()` registers `IWatchHistoryStore` → `InMemoryWatchHistoryStore` and
  wires the updated `ContentRanker` constructor.
- `AddStreaming()` passes `IAetherAiProvider` (optional) into `AbrController`.

### Tests
- `WatchHistoryStoreTests` (17 tests) — record/retrieve, EWMA blend, live-
  stream handling, blank-input no-ops, cap eviction, viewer isolation.
- `ContentRankerWatchHistoryTests` (4 tests) — high completion boosts rank,
  skip history suppresses rank, no history is neutral, viewer isolation.
- `ContentModeratorSocialTests` (10 tests) — null packet, AI unavailable,
  AI threat propagation, reaction burst, social burst, combined signals,
  source isolation.
- `AbrControllerAiTests` (10 tests) — high/low bias rung changes, bias
  clamping, empty dictionary neutral, multi-transport averaging, throwing
  provider fallback.

---

## [1.0.0] — 2026-05-21

First stable release. All eight language implementations (C#, Kotlin, Swift,
Rust, TypeScript, Go, Python, C) are wire-compatible and publish to their
respective package registries from a single CI/CD pipeline.

### Added

**Core domain (`Aether.Media.Core`)**
- `MediaContent` — immutable record keyed by SHA-256 `ContentHash`; computed
  `IsVideo`, `IsAudio`, `FormattedDuration` properties
- `MediaFeedItem` — content + reaction counts + watch count
- `MediaReaction` — Like / Share / Comment / SuperReact with `PositionMs`
  playback marker
- `MediaProfile` — AetherTag identity, display name, avatar hash, bio
- `WatchSession` / `LiveStream` — thin wrappers over aether-protocol session types
- `IMediaLibrary`, `IMediaFeed`, `IMediaPlayer`, `IContentNode`,
  `ICreatorChannel` — core interface contracts

**Social layer (`Aether.Media.Social`)**
- `FeedAggregator` — thread-safe, capped at 500 items, deduplicates by
  `ContentHash`, subscribes to `IStreamingService.StreamAnnounced` and
  `IContentService.ContentAnnounced`
- `ISocialGraph` — follow/unfollow by AetherTag; resolves to UHID
- `IReactionService` — sends/receives reactions mapped to `WatchReactionPayload`
- `IDiscoveryService` — surfaces nearby creators from mesh peer list via
  `IHandshakeService.PeerNegotiated`
- DTN-backed follow gossip via `IDtnService.CreateBundleAsync`

**Streaming layer (`Aether.Media.Streaming`)**
- `ILiveStreamPublisher` — captures encoded frames, feeds `IStreamingService`
- `IWatchPartyCoordinator` — manages invite flow, latency compensation,
  reaction overlay
- `IAbrController` — monitors bandwidth, selects bitrate rung
  (200 / 400 / 800 / 1200 / 2500 / 5000 Kbps), requests keyframes
- Full leverage of `IStreamingService`, `IWatchTogetherService`,
  `IVideoCallService`, `IGroupVideoService`

**Content layer (`Aether.Media.Content`)**
- `IMediaLibraryScanner` — indexes local files into `ContentDescriptor` records
- `IContentCache` — LRU cache with 500 MiB default capacity
- `IThumbnailService` — extracts and distributes video thumbnails by hash
- `IMetadataResolver` — ID3/MP4 tags + NFO files resolved to `MediaContent`
- Hash-verified P2P distribution via `IContentService`; BitTorrent metadata
  via `IWatchTogetherService.BroadcastTorrentAsync`

**Identity layer (`Aether.Media.Identity`)**
- `IProfileService` — create / update / fetch `MediaProfile`
- `IProfileSyncService` — gossips profile updates via `ProfileSync(23)` packet
- `IAvatarService` — distributes avatars as `IContentService` chunks

**AI layer (`Aether.Media.AI`)**
- `IContentRanker` — scores feed items using reputation + CircleAI bias +
  watch history; degrades gracefully to reputation-only when
  `IAetherAiProvider.IsAvailable` is `false`
- `ICreatorReputationView` — surfaces `INodeReputationService` scores as
  creator trust signals
- `IContentModerator` — flags content from low-reputation or high-threat nodes
  via `IAetherAiProvider.AssessThreatAsync`

**Local library (`Aether.Media.LocalLibrary`)**
- Scanner, watcher, and LRU cache for the local media library

**Reel layer (`Aether.Media.Reel`)**
- Short-form vertical video feed (TikTok-style) built on `FeedAggregator`

**Distribution layer (`Aether.Media.Distribution`)**
- P2P chunk scheduling and reassembly coordinator

**DI wiring (`Aether.Media.DependencyInjection`)**
- `services.AddAetherMedia()` fluent builder:
  ```csharp
  services.AddAetherMedia(aether =>
  {
      aether
          .AddSocial()
          .AddStreaming()
          .AddContent()
          .AddIdentity()
          .AddAI()       // no-op if IAetherAiProvider not registered
          .AddDesktop(); // Avalonia + LibVLCSharp
  });
  ```

**Android apps**
- `android/media/` — main Aether Media Android app (Jetpack Compose,
  media3/ExoPlayer); 3 unit tests (FeedItemUiTest, HomeViewModelTest,
  PlayerViewModelTest)
- `android/media-tv/` — Android TV lean-back variant (D-pad navigation)

**Platform applications**
- `Aether.Media.Desktop` — Avalonia MVVM desktop (Windows, Linux, macOS)
  with LibVLCSharp VideoView, NativeControlHost overlay pattern,
  MeshStatusBar showing transport / peer count / bandwidth / AetherTag
- `Aether.Media.Mobile` — .NET MAUI cross-platform mobile shell
- `Aether.Media.Web` — Blazor web player

**TypeScript web player**
- HLS.js + Shaka Player integration
- `AetherMediaPlayer`, `FeedClient`, `ReactionClient`, `ProfileClient`,
  `AetherStreamClient`
- Strict mode, declaration maps, NodeNext modules; 9 test files

**Python plugin engine**
- Plugin host (VLC extension model), M3U/XSPF playlist parsers,
  ID3/MP4 metadata, NFO scraping, command-line interface
- `pyproject.toml` with OIDC-based PyPI publish

**Rust desktop fallback**
- LibVLC FFI bindings, Iced/Slint UI, `FeedStore` (500-item cap),
  social graph, streaming client

**Go daemon + CLI**
- `aether-media-daemon` — background media node
- `aether-media-cli` — CLI player and library manager
- LibVLC cgo bindings, social graph, feed aggregation

**Kotlin JVM**
- JVM 21 library; media3/ExoPlayer integration, Compose Multiplatform UI
- Published to GitHub Packages

**Swift iOS/macOS**
- AVFoundation integration, SwiftUI, `FeedAggregatorTests`,
  `MediaContentTests`, `SocialGraphTests`
- Swift Package Manager distribution

**C embedded**
- Headless player via LibVLC C API; streaming and social modules
- CMake build; optional LibVLC dependency

**Infrastructure**
- Full CI across all 8 languages on every push to `main` / `develop`
- Cross-language wire-format interop validated via `tests/cross-language/`
  golden fixtures
- Coverage gate (80%) with `[ExcludeFromCodeCoverage]` and `*.g.cs`
  exclusions via `Directory.Build.props`
- Single-command publish to NuGet, npm, PyPI, crates.io, GitHub Packages
  (Kotlin), Go sub-module tag on semver tag

**Governance**
- `LICENSE` (MIT), `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`

### Architecture

Aether Media is a decentralised social media network and player. Two devices
with no internet connection can discover each other, share content, stream live
video, and interact socially over BLE, Wi-Fi Direct, NearLink, NFC, LoRa, or
HTTP relay — all without a central server.

Every Aether Protocol service is leveraged:

| Aether Interface | How Aether Media Uses It |
|---|---|
| `IStreamingService` | Live broadcast + subscription + ABR |
| `IWatchTogetherService` | Watch parties, sync, reactions, ChipIn, BitTorrent |
| `IVideoCallService` / `IGroupVideoService` | 1-to-1 and multi-party video calls |
| `IContentService` | P2P media file distribution (chunked, hash-verified) |
| `IDtnService` | Follow / content-announce delivery to offline peers |
| `IMessagingService` | In-app chat alongside watch sessions |
| `IReputationGossipService` | Creator trust scores gossiped across mesh |
| `IAetherAiProvider` | Feed ranking, route pre-seeding, threat assessment |
| `ISosBroadcastService` | Emergency interrupt of any stream / watch session |
| `IHandshakeService` | Discover `NodeCapabilities.Streaming` peers on connect |
| `AetherTag` | Human-readable creator identity (e.g. `KXJB7-MN2P4`) |
