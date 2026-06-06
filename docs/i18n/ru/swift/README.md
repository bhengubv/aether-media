# Aether Media — Реализация на Swift

[English](../../../../swift/README.md) · [Français](../../fr/swift/README.md) · [Español](../../es/swift/README.md) · [العربية](../../ar/swift/README.md) · [中文简体](../../zh-CN/swift/README.md) · [日本語](../../ja/swift/README.md) · [Deutsch](../../de/swift/README.md) · [Português (BR)](../../pt-BR/swift/README.md) · [Русский](README.md) · [فارسی](../../fa/swift/README.md) · [한국어](../../ko/swift/README.md)

Полноценная реализация Aether Media на Swift для iOS и macOS. Интегрирует AVFoundation для воспроизведения, SwiftUI для интерфейса, а также mesh-протокол Aether для одноранговых обнаружения контента и трансляции в реальном времени — всё без центрального сервера.

---

## Требования

- Swift 5.9+
- Xcode 15+
- iOS 16+ или macOS 13+

---

## Добавление в проект

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

## Сборка и тестирование

```bash
swift build
swift test
```

---

## Модули

| Модуль | Описание |
|--------|-------------|
| `Models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `Feed` | `FeedAggregator` — ограничен 500 элементами, поддерживается Combine |
| `Social` | `SocialGraph` — подписки/отписки, изолирован с использованием акторов |
| `Player` | Интеграция с AVFoundation — воспроизведение, перемотка, скорость, PiP |
| `Streaming` | Подписка на трансляцию Aether в реальном времени и сборка сегментов |
| `Content` | Одноранговая загрузка фрагментов контента и управление кэшем |
| `UI` | Представления SwiftUI — список ленты, наложение проигрывателя, карточка профиля |

---

## Быстрый старт

### Модели

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

### Граф социальных связей

```swift
import AetherMeshMedia

let graph = SocialGraph()
await graph.follow("peer-uhid-abc123")
await graph.follow("peer-uhid-def456")

print(await graph.followingCount)              // 2
print(await graph.isFollowing("peer-uhid-abc123")) // true

await graph.unfollow("peer-uhid-abc123")
```

### Агрегатор ленты

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

### Проигрыватель

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

### Трансляция в реальном времени

```swift
import AetherMeshMedia

let client = StreamSubscriber()
try await client.subscribe(to: hostUhid)

for await segment in client.segments {
    player.enqueue(segment)
}
```

---

## Интеграция с SwiftUI

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

## Тесты

```bash
swift test
```

Тестовые цели:

| Цель | Описание |
|--------|-------------|
| `FeedAggregatorTests` | Ограничение ёмкости, дедупликация, потокобезопасность |
| `MediaContentTests` | Форматирование длительности, вычисляемые свойства |
| `SocialGraphTests` | Подписки/отписки, изоляция акторов |

---

## Структура проекта

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

## Лицензия

MIT
