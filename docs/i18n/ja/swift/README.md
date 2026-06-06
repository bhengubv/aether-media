# Aether Media — Swift 実装

[English](../../../../swift/README.md) · [Français](../../fr/swift/README.md) · [Español](../../es/swift/README.md) · [العربية](../../ar/swift/README.md) · [中文简体](../../zh-CN/swift/README.md) · [日本語](README.md) · [Deutsch](../../de/swift/README.md) · [Português (BR)](../../pt-BR/swift/README.md) · [Русский](../../ru/swift/README.md) · [فارسی](../../fa/swift/README.md) · [한국어](../../ko/swift/README.md)

iOS および macOS 向けの包括的な Aether Media Swift 実装です。再生に AVFoundation、インターフェースに SwiftUI を統合し、中央サーバーなしでピアツーピアのコンテンツ探索およびライブストリーミングを実現する Aether メッシュプロトコルに接続します。

---

## 要件

- Swift 5.9+
- Xcode 15+
- iOS 16+ または macOS 13+

---

## プロジェクトへの追加

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

## ビルドとテスト

```bash
swift build
swift test
```

---

## モジュール

| モジュール | 説明 |
|--------|-------------|
| `Models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `Feed` | `FeedAggregator` — 最大 500 件、Combine バックエンド |
| `Social` | `SocialGraph` — フォロー/アンフォロー、アクター分離 |
| `Player` | AVFoundation 統合 — 再生、シーク、速度、PiP |
| `Streaming` | Aether ライブストリーム購読とセグメントアセンブリ |
| `Content` | P2P コンテンツチャンクのダウンロードとキャッシュ管理 |
| `UI` | SwiftUI ビュー — フィードリスト、プレイヤーオーバーレイ、プロフィールカード |

---

## クイックスタート

### モデル

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

### ソーシャルグラフ

```swift
import AetherMeshMedia

let graph = SocialGraph()
await graph.follow("peer-uhid-abc123")
await graph.follow("peer-uhid-def456")

print(await graph.followingCount)              // 2
print(await graph.isFollowing("peer-uhid-abc123")) // true

await graph.unfollow("peer-uhid-abc123")
```

### フィードアグリゲーター

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

### プレイヤー

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

### ライブストリーミング

```swift
import AetherMeshMedia

let client = StreamSubscriber()
try await client.subscribe(to: hostUhid)

for await segment in client.segments {
    player.enqueue(segment)
}
```

---

## SwiftUI 統合

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

## テスト

```bash
swift test
```

テストターゲット:

| ターゲット | 説明 |
|--------|-------------|
| `FeedAggregatorTests` | 容量上限、重複排除、スレッドセーフ性 |
| `MediaContentTests` | 再生時間のフォーマット、計算プロパティ |
| `SocialGraphTests` | フォロー/アンフォロー、アクター分離 |

---

## プロジェクト構成

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

## ライセンス

MIT
