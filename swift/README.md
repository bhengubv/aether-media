# Aether Media — Swift Implementation

[English](README.md) · [Français](../docs/i18n/fr/swift/README.md) · [Español](../docs/i18n/es/swift/README.md) · [العربية](../docs/i18n/ar/swift/README.md) · [中文简体](../docs/i18n/zh-CN/swift/README.md) · [日本語](../docs/i18n/ja/swift/README.md) · [Deutsch](../docs/i18n/de/swift/README.md) · [Português (BR)](../docs/i18n/pt-BR/swift/README.md) · [Русский](../docs/i18n/ru/swift/README.md) · [فارسی](../docs/i18n/fa/swift/README.md) · [한국어](../docs/i18n/ko/swift/README.md)

A comprehensive Swift implementation of Aether Media for iOS and macOS. Integrates AVFoundation for playback, SwiftUI for the interface, and the Aether mesh protocol for peer-to-peer content discovery and live streaming — all without a central server.

---

## Requirements

- Swift 5.9+
- Xcode 15+
- iOS 16+ or macOS 13+

---

## Add to your project

```swift
// Package.swift
dependencies: [
    .package(url: "https://github.com/bhengubv/aether-media.git", from: "1.0.0"),
],
targets: [
    .target(
        name: "MyApp",
        dependencies: [
            .product(name: "AetherMedia", package: "aether-media")
        ]
    ),
]
```

---

## Build and test

```bash
swift build
swift test
```

---

## Modules

| Module | Description |
|--------|-------------|
| `Models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `Feed` | `FeedAggregator` — capped at 500 items, Combine-backed |
| `Social` | `SocialGraph` — follow/unfollow, actor-isolated |
| `Player` | AVFoundation integration — playback, seek, speed, PiP |
| `Streaming` | Aether live-stream subscription and segment assembly |
| `Content` | P2P content chunk download and cache management |
| `UI` | SwiftUI views — feed list, player overlay, profile card |

---

## Quick start

### Models

```swift
import AetherMedia

let content = MediaContent(
    contentHash: "sha256abc",
    title: "My Video",
    durationMs: 180_000,
    codec: "h264",
    contentType: "video/mp4",
    creatorUhid: "uhid-xyz",
    sizeBytes: 52_428_800
)

print(content.formattedDuration)  // "3:00"
print(content.isVideo)             // true
```

### Social graph

```swift
import AetherMedia

let graph = SocialGraph()
await graph.follow("peer-uhid-abc123")
await graph.follow("peer-uhid-def456")

print(await graph.followingCount)              // 2
print(await graph.isFollowing("peer-uhid-abc123")) // true

await graph.unfollow("peer-uhid-abc123")
```

### Feed aggregator

```swift
import AetherMedia
import Combine

let aggregator = FeedAggregator(capacity: 500)

// Subscribe to feed updates
let cancellable = aggregator.feedPublisher
    .receive(on: DispatchQueue.main)
    .sink { items in
        print("Feed updated: \(items.count) items")
    }

aggregator.push(feedItem)
```

### Player

```swift
import AetherMedia
import AVFoundation

let player = AetherMediaPlayer()

try await player.load(contentHash: "sha256abc")
player.play()

// Observe playback state
player.$state
    .sink { state in
        switch state {
        case .playing: print("Playing")
        case .paused:  print("Paused")
        case .ended:   print("Ended")
        default: break
        }
    }
    .store(in: &cancellables)
```

### Live streaming

```swift
import AetherMedia

let client = StreamSubscriber()
try await client.subscribe(to: hostUhid)

for await segment in client.segments {
    player.enqueue(segment)
}
```

---

## SwiftUI integration

```swift
import SwiftUI
import AetherMedia

struct ContentView: View {
    @StateObject private var feedVM = FeedViewModel()

    var body: some View {
        NavigationStack {
            FeedView(viewModel: feedVM)
                .navigationTitle("Aether Media")
        }
        .task { await feedVM.load() }
    }
}
```

---

## Tests

```bash
swift test
```

Test targets:

| Target | Description |
|--------|-------------|
| `FeedAggregatorTests` | Capacity cap, deduplication, thread safety |
| `MediaContentTests` | Duration formatting, computed properties |
| `SocialGraphTests` | Follow/unfollow, actor isolation |

---

## Project layout

```
swift/
├── Sources/
│   └── AetherMedia/
│       ├── Content/     # P2P chunk download and cache
│       ├── Feed/        # FeedAggregator + Combine publisher
│       ├── Models/      # Domain structs (Codable, Sendable)
│       ├── Player/      # AVFoundation wrapper
│       ├── Social/      # SocialGraph (Swift actors)
│       ├── Streaming/   # Live stream subscription
│       └── UI/          # SwiftUI views and view models
├── Tests/
│   └── AetherMediaTests/
└── Package.swift
```

---

## License

MIT
