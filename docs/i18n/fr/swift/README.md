# Aether Media — Implémentation Swift

[English](../../../../swift/README.md) · [Français](README.md) · [Español](../../es/swift/README.md) · [العربية](../../ar/swift/README.md) · [中文简体](../../zh-CN/swift/README.md) · [日本語](../../ja/swift/README.md) · [Deutsch](../../de/swift/README.md) · [Português (BR)](../../pt-BR/swift/README.md) · [Русский](../../ru/swift/README.md) · [فارسی](../../fa/swift/README.md) · [한국어](../../ko/swift/README.md)

Une implémentation Swift complète d'Aether Media pour iOS et macOS. Intègre AVFoundation pour la lecture, SwiftUI pour l'interface, et le protocole de maillage Aether pour la découverte de contenu pair-à-pair et le streaming en direct — sans serveur central.

---

## Prérequis

- Swift 5.9+
- Xcode 15+
- iOS 16+ ou macOS 13+

---

## Ajouter à votre projet

```swift
// Package.swift
dependencies: [
    .package(url: "https://github.com/bhengubv/aether-media.git", from: "1.0.0"),
],
targets: [
    .target(
        name: "MyApp",
        dependencies: [
            .product(name: "AetherNetMedia", package: "aether-media")
        ]
    ),
]
```

---

## Compiler et tester

```bash
swift build
swift test
```

---

## Modules

| Module | Description |
|--------|-------------|
| `Models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `Feed` | `FeedAggregator` — limité à 500 éléments, basé sur Combine |
| `Social` | `SocialGraph` — suivi/désabonnement, isolé par acteur |
| `Player` | Intégration AVFoundation — lecture, navigation, vitesse, PiP |
| `Streaming` | Abonnement aux flux en direct Aether et assemblage de segments |
| `Content` | Téléchargement de fragments de contenu P2P et gestion du cache |
| `UI` | Vues SwiftUI — liste de flux, superposition du lecteur, carte de profil |

---

## Démarrage rapide

### Modèles

```swift
import AetherNetMedia

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

### Graphe social

```swift
import AetherNetMedia

let graph = SocialGraph()
await graph.follow("peer-uhid-abc123")
await graph.follow("peer-uhid-def456")

print(await graph.followingCount)              // 2
print(await graph.isFollowing("peer-uhid-abc123")) // true

await graph.unfollow("peer-uhid-abc123")
```

### Agrégateur de flux

```swift
import AetherNetMedia
import Combine

let aggregator = FeedAggregator(capacity: 500)

// S'abonner aux mises à jour du flux
let cancellable = aggregator.feedPublisher
    .receive(on: DispatchQueue.main)
    .sink { items in
        print("Feed updated: \(items.count) items")
    }

aggregator.push(feedItem)
```

### Lecteur

```swift
import AetherNetMedia
import AVFoundation

let player = AetherNetMediaPlayer()

try await player.load(contentHash: "sha256abc")
player.play()

// Observer l'état de lecture
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

### Streaming en direct

```swift
import AetherNetMedia

let client = StreamSubscriber()
try await client.subscribe(to: hostUhid)

for await segment in client.segments {
    player.enqueue(segment)
}
```

---

## Intégration SwiftUI

```swift
import SwiftUI
import AetherNetMedia

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

Cibles de test :

| Cible | Description |
|--------|-------------|
| `FeedAggregatorTests` | Limite de capacité, déduplication, sécurité des threads |
| `MediaContentTests` | Formatage de la durée, propriétés calculées |
| `SocialGraphTests` | Suivi/désabonnement, isolation par acteur |

---

## Structure du projet

```
swift/
├── Sources/
│   └── AetherNetMedia/
│       ├── Content/     # P2P chunk download and cache
│       ├── Feed/        # FeedAggregator + Combine publisher
│       ├── Models/      # Domain structs (Codable, Sendable)
│       ├── Player/      # AVFoundation wrapper
│       ├── Social/      # SocialGraph (Swift actors)
│       ├── Streaming/   # Live stream subscription
│       └── UI/          # SwiftUI views and view models
├── Tests/
│   └── AetherNetMediaTests/
└── Package.swift
```

---

## Licence

MIT
