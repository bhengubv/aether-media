# Aether Media — Implementación en Swift

[English](../../../../swift/README.md) · [Français](../../fr/swift/README.md) · [Español](README.md) · [العربية](../../ar/swift/README.md) · [中文简体](../../zh-CN/swift/README.md) · [日本語](../../ja/swift/README.md) · [Deutsch](../../de/swift/README.md) · [Português (BR)](../../pt-BR/swift/README.md) · [Русский](../../ru/swift/README.md) · [فارسی](../../fa/swift/README.md) · [한국어](../../ko/swift/README.md)

Una implementación completa en Swift de Aether Media para iOS y macOS. Integra AVFoundation para la reproducción, SwiftUI para la interfaz, y el protocolo de malla Aether para el descubrimiento de contenido entre pares y la transmisión en directo — todo sin un servidor central.

---

## Requisitos

- Swift 5.9+
- Xcode 15+
- iOS 16+ o macOS 13+

---

## Añadir a tu proyecto

```swift
// Package.swift
dependencies: [
    .package(url: "https://github.com/bhengubv/aether-media.git", from: "1.0.0"),
],
targets: [
    .target(
        name: "MyApp",
        dependencies: [
            .product(name: "AetherMeshMedia", package: "aether-media")
        ]
    ),
]
```

---

## Compilar y probar

```bash
swift build
swift test
```

---

## Módulos

| Módulo | Descripción |
|--------|-------------|
| `Models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `Feed` | `FeedAggregator` — limitado a 500 elementos, respaldado por Combine |
| `Social` | `SocialGraph` — seguir/dejar de seguir, aislado por actor |
| `Player` | Integración con AVFoundation — reproducción, búsqueda, velocidad, PiP |
| `Streaming` | Suscripción a stream en directo de Aether y ensamblaje de segmentos |
| `Content` | Descarga de fragmentos de contenido P2P y gestión de caché |
| `UI` | Vistas SwiftUI — lista de feed, superposición del reproductor, tarjeta de perfil |

---

## Inicio rápido

### Modelos

```swift
import AetherMeshMedia

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
import AetherMeshMedia

let graph = SocialGraph()
await graph.follow("peer-uhid-abc123")
await graph.follow("peer-uhid-def456")

print(await graph.followingCount)              // 2
print(await graph.isFollowing("peer-uhid-abc123")) // true

await graph.unfollow("peer-uhid-abc123")
```

### Agregador de feed

```swift
import AetherMeshMedia
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

### Reproductor

```swift
import AetherMeshMedia
import AVFoundation

let player = AetherMeshMediaPlayer()

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

### Transmisión en directo

```swift
import AetherMeshMedia

let client = StreamSubscriber()
try await client.subscribe(to: hostUhid)

for await segment in client.segments {
    player.enqueue(segment)
}
```

---

## Integración con SwiftUI

```swift
import SwiftUI
import AetherMeshMedia

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

## Pruebas

```bash
swift test
```

Objetivos de prueba:

| Objetivo | Descripción |
|--------|-------------|
| `FeedAggregatorTests` | Límite de capacidad, deduplicación, seguridad para hilos |
| `MediaContentTests` | Formato de duración, propiedades calculadas |
| `SocialGraphTests` | Seguir/dejar de seguir, aislamiento de actores |

---

## Estructura del proyecto

```
swift/
├── Sources/
│   └── AetherMeshMedia/
│       ├── Content/     # P2P chunk download and cache
│       ├── Feed/        # FeedAggregator + Combine publisher
│       ├── Models/      # Domain structs (Codable, Sendable)
│       ├── Player/      # AVFoundation wrapper
│       ├── Social/      # SocialGraph (Swift actors)
│       ├── Streaming/   # Live stream subscription
│       └── UI/          # SwiftUI views and view models
├── Tests/
│   └── AetherMeshMediaTests/
└── Package.swift
```

---

## Licencia

MIT
