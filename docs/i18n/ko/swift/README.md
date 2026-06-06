# Aether Media — Swift 구현

[English](../../../../swift/README.md) · [Français](../../fr/swift/README.md) · [Español](../../es/swift/README.md) · [العربية](../../ar/swift/README.md) · [中文简体](../../zh-CN/swift/README.md) · [日本語](../../ja/swift/README.md) · [Deutsch](../../de/swift/README.md) · [Português (BR)](../../pt-BR/swift/README.md) · [Русский](../../ru/swift/README.md) · [فارسی](../../fa/swift/README.md) · [한국어](README.md)

iOS 및 macOS를 위한 포괄적인 Swift 구현입니다. 재생을 위해 AVFoundation, 인터페이스를 위해 SwiftUI를 통합하며, 중앙 서버 없이 P2P 콘텐츠 발견 및 라이브 스트리밍을 위한 Aether 메시 프로토콜을 사용합니다.

---

## 요구 사항

- Swift 5.9+
- Xcode 15+
- iOS 16+ 또는 macOS 13+

---

## 프로젝트에 추가하기

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

## 빌드 및 테스트

```bash
swift build
swift test
```

---

## 모듈

| 모듈 | 설명 |
|--------|-------------|
| `Models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `Feed` | `FeedAggregator` — 최대 500개 항목, Combine 기반 |
| `Social` | `SocialGraph` — 팔로우/언팔로우, 액터 격리 |
| `Player` | AVFoundation 통합 — 재생, 탐색, 속도, PiP |
| `Streaming` | Aether 라이브 스트림 구독 및 세그먼트 조립 |
| `Content` | P2P 콘텐츠 청크 다운로드 및 캐시 관리 |
| `UI` | SwiftUI 뷰 — 피드 목록, 플레이어 오버레이, 프로필 카드 |

---

## 빠른 시작

### 모델

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

### 소셜 그래프

```swift
import AetherMeshMedia

let graph = SocialGraph()
await graph.follow("peer-uhid-abc123")
await graph.follow("peer-uhid-def456")

print(await graph.followingCount)              // 2
print(await graph.isFollowing("peer-uhid-abc123")) // true

await graph.unfollow("peer-uhid-abc123")
```

### 피드 집계기

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

### 플레이어

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

### 라이브 스트리밍

```swift
import AetherMeshMedia

let client = StreamSubscriber()
try await client.subscribe(to: hostUhid)

for await segment in client.segments {
    player.enqueue(segment)
}
```

---

## SwiftUI 통합

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

## 테스트

```bash
swift test
```

테스트 타깃:

| 타깃 | 설명 |
|--------|-------------|
| `FeedAggregatorTests` | 용량 제한, 중복 제거, 스레드 안전성 |
| `MediaContentTests` | 재생 시간 형식화, 계산 프로퍼티 |
| `SocialGraphTests` | 팔로우/언팔로우, 액터 격리 |

---

## 프로젝트 구조

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

## 라이선스

MIT
