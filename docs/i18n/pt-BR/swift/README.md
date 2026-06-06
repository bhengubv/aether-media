# Aether Media — Implementação em Swift

[English](../../../../swift/README.md) · [Français](../../fr/swift/README.md) · [Español](../../es/swift/README.md) · [العربية](../../ar/swift/README.md) · [中文简体](../../zh-CN/swift/README.md) · [日本語](../../ja/swift/README.md) · [Deutsch](../../de/swift/README.md) · [Português (BR)](README.md) · [Русский](../../ru/swift/README.md) · [فارسی](../../fa/swift/README.md) · [한국어](../../ko/swift/README.md)

Uma implementação abrangente do Aether Media em Swift para iOS e macOS. Integra AVFoundation para reprodução, SwiftUI para a interface e o protocolo de mesh Aether para descoberta de conteúdo peer-to-peer e transmissão ao vivo — tudo sem um servidor central.

---

## Requisitos

- Swift 5.9+
- Xcode 15+
- iOS 16+ ou macOS 13+

---

## Adicionar ao seu projeto

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

## Compilar e testar

```bash
swift build
swift test
```

---

## Módulos

| Módulo | Descrição |
|--------|-------------|
| `Models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `Feed` | `FeedAggregator` — limitado a 500 itens, baseado em Combine |
| `Social` | `SocialGraph` — seguir/deixar de seguir, isolado com actor |
| `Player` | Integração com AVFoundation — reprodução, busca, velocidade, PiP |
| `Streaming` | Assinatura de transmissão ao vivo e montagem de segmentos do Aether |
| `Content` | Download de fragmentos de conteúdo P2P e gerenciamento de cache |
| `UI` | Views SwiftUI — lista do feed, sobreposição do player, cartão de perfil |

---

## Início rápido

### Modelos

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

### Grafo social

```swift
import AetherNetMedia

let graph = SocialGraph()
await graph.follow("peer-uhid-abc123")
await graph.follow("peer-uhid-def456")

print(await graph.followingCount)              // 2
print(await graph.isFollowing("peer-uhid-abc123")) // true

await graph.unfollow("peer-uhid-abc123")
```

### Agregador de feed

```swift
import AetherNetMedia
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
import AetherNetMedia
import AVFoundation

let player = AetherNetMediaPlayer()

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

### Transmissão ao vivo

```swift
import AetherNetMedia

let client = StreamSubscriber()
try await client.subscribe(to: hostUhid)

for await segment in client.segments {
    player.enqueue(segment)
}
```

---

## Integração com SwiftUI

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

## Testes

```bash
swift test
```

Alvos de teste:

| Alvo | Descrição |
|--------|-------------|
| `FeedAggregatorTests` | Limite de capacidade, deduplicação, segurança para threads |
| `MediaContentTests` | Formatação de duração, propriedades computadas |
| `SocialGraphTests` | Seguir/deixar de seguir, isolamento de actor |

---

## Estrutura do projeto

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

## Licença

MIT
