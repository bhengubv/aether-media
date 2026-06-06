# Aether Media — Swift 实现

[English](../../../../swift/README.md) · [Français](../../fr/swift/README.md) · [Español](../../es/swift/README.md) · [العربية](../../ar/swift/README.md) · [中文简体](README.md) · [日本語](../../ja/swift/README.md) · [Deutsch](../../de/swift/README.md) · [Português (BR)](../../pt-BR/swift/README.md) · [Русский](../../ru/swift/README.md) · [فارسی](../../fa/swift/README.md) · [한국어](../../ko/swift/README.md)

面向 iOS 和 macOS 的全功能 Swift 版 Aether Media 实现。集成 AVFoundation 用于播放，SwiftUI 用于界面，以及 Aether 网状协议——实现点对点内容发现与直播，全程无需中央服务器。

---

## 环境要求

- Swift 5.9+
- Xcode 15+
- iOS 16+ 或 macOS 13+

---

## 添加到项目

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

## 构建与测试

```bash
swift build
swift test
```

---

## 模块

| 模块 | 说明 |
|--------|-------------|
| `Models` | `MediaContent`、`MediaProfile`、`MediaFeedItem`、`MediaReaction` |
| `Feed` | `FeedAggregator`——上限 500 条，基于 Combine |
| `Social` | `SocialGraph`——关注/取消关注，Actor 隔离 |
| `Player` | AVFoundation 集成——播放、跳转、倍速、画中画 |
| `Streaming` | Aether 直播订阅与分片组装 |
| `Content` | P2P 内容块下载与缓存管理 |
| `UI` | SwiftUI 视图——信息流列表、播放器叠加层、个人资料卡片 |

---

## 快速入门

### 模型

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

### 社交关系图谱

```swift
import AetherMeshMedia

let graph = SocialGraph()
await graph.follow("peer-uhid-abc123")
await graph.follow("peer-uhid-def456")

print(await graph.followingCount)              // 2
print(await graph.isFollowing("peer-uhid-abc123")) // true

await graph.unfollow("peer-uhid-abc123")
```

### 信息流聚合器

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

### 播放器

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

### 直播

```swift
import AetherMeshMedia

let client = StreamSubscriber()
try await client.subscribe(to: hostUhid)

for await segment in client.segments {
    player.enqueue(segment)
}
```

---

## SwiftUI 集成

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

## 测试

```bash
swift test
```

测试目标：

| 目标 | 说明 |
|--------|-------------|
| `FeedAggregatorTests` | 容量上限、去重、线程安全 |
| `MediaContentTests` | 时长格式化、计算属性 |
| `SocialGraphTests` | 关注/取消关注、Actor 隔离 |

---

## 项目结构

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

## 许可证

MIT
