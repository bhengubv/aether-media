# Aether Media — Swift-Implementierung

[English](../../../../swift/README.md) · [Français](../../fr/swift/README.md) · [Español](../../es/swift/README.md) · [العربية](../../ar/swift/README.md) · [中文简体](../../zh-CN/swift/README.md) · [日本語](../../ja/swift/README.md) · [Deutsch](README.md) · [Português (BR)](../../pt-BR/swift/README.md) · [Русский](../../ru/swift/README.md) · [فارسی](../../fa/swift/README.md) · [한국어](../../ko/swift/README.md)

Eine umfassende Swift-Implementierung von Aether Media für iOS und macOS. Integriert AVFoundation für die Wiedergabe, SwiftUI für die Benutzeroberfläche und das Aether-Mesh-Protokoll für die Peer-to-Peer-Inhaltsentdeckung und das Live-Streaming — ganz ohne zentralen Server.

---

## Voraussetzungen

- Swift 5.9+
- Xcode 15+
- iOS 16+ oder macOS 13+

---

## Zum Projekt hinzufügen

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

## Bauen und testen

```bash
swift build
swift test
```

---

## Module

| Modul | Beschreibung |
|--------|-------------|
| `Models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `Feed` | `FeedAggregator` — auf 500 Einträge begrenzt, Combine-basiert |
| `Social` | `SocialGraph` — Folgen/Entfolgen, actor-isoliert |
| `Player` | AVFoundation-Integration — Wiedergabe, Suche, Geschwindigkeit, PiP |
| `Streaming` | Aether-Live-Stream-Abonnement und Segment-Zusammensetzung |
| `Content` | P2P-Inhalts-Chunk-Download und Cache-Verwaltung |
| `UI` | SwiftUI-Views — Feed-Liste, Player-Overlay, Profilkarte |

---

## Schnellstart

### Modelle

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

### Sozialer Graph

```swift
import AetherMedia

let graph = SocialGraph()
await graph.follow("peer-uhid-abc123")
await graph.follow("peer-uhid-def456")

print(await graph.followingCount)              // 2
print(await graph.isFollowing("peer-uhid-abc123")) // true

await graph.unfollow("peer-uhid-abc123")
```

### Feed-Aggregator

```swift
import AetherMedia
import Combine

let aggregator = FeedAggregator(capacity: 500)

// Feed-Updates abonnieren
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

// Wiedergabezustand beobachten
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

### Live-Streaming

```swift
import AetherMedia

let client = StreamSubscriber()
try await client.subscribe(to: hostUhid)

for await segment in client.segments {
    player.enqueue(segment)
}
```

---

## SwiftUI-Integration

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

Testziele:

| Ziel | Beschreibung |
|--------|-------------|
| `FeedAggregatorTests` | Kapazitätsbegrenzung, Deduplizierung, Thread-Sicherheit |
| `MediaContentTests` | Dauerformatierung, berechnete Eigenschaften |
| `SocialGraphTests` | Folgen/Entfolgen, Actor-Isolation |

---

## Projektstruktur

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

## Lizenz

MIT
