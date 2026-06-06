<div dir="rtl">

# Aether Media — پیاده‌سازی Swift

[English](../../../../swift/README.md) · [Français](../../fr/swift/README.md) · [Español](../../es/swift/README.md) · [العربية](../../ar/swift/README.md) · [中文简体](../../zh-CN/swift/README.md) · [日本語](../../ja/swift/README.md) · [Deutsch](../../de/swift/README.md) · [Português (BR)](../../pt-BR/swift/README.md) · [Русский](../../ru/swift/README.md) · [فارسی](README.md) · [한국어](../../ko/swift/README.md)

یک پیاده‌سازی جامع Swift از Aether Media برای iOS و macOS. AVFoundation را برای پخش، SwiftUI را برای رابط کاربری، و پروتکل mesh Aether را برای کشف محتوای همتا-به-همتا و پخش زنده — همه بدون سرور مرکزی — یکپارچه می‌کند.

---

## پیش‌نیازها

- Swift 5.9+
- Xcode 15+
- iOS 16+ یا macOS 13+

---

## افزودن به پروژه

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

## ساخت و تست

```bash
swift build
swift test
```

---

## ماژول‌ها

| ماژول | توضیحات |
|--------|-------------|
| `Models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `Feed` | `FeedAggregator` — با حداکثر ۵۰۰ آیتم، پشتیبانی Combine |
| `Social` | `SocialGraph` — فالو/آنفالو، ایزوله‌شده با actor |
| `Player` | یکپارچه‌سازی AVFoundation — پخش، پیش‌بردن، سرعت، PiP |
| `Streaming` | اشتراک در پخش زنده Aether و مونتاژ قطعات |
| `Content` | دانلود قطعه‌ای محتوا P2P و مدیریت کش |
| `UI` | نماهای SwiftUI — فهرست فید، پوشش پخش‌کننده، کارت پروفایل |

---

## شروع سریع

### مدل‌ها

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

### گراف اجتماعی

```swift
import AetherNetMedia

let graph = SocialGraph()
await graph.follow("peer-uhid-abc123")
await graph.follow("peer-uhid-def456")

print(await graph.followingCount)              // 2
print(await graph.isFollowing("peer-uhid-abc123")) // true

await graph.unfollow("peer-uhid-abc123")
```

### تجمیع‌دهنده فید

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

### پخش‌کننده

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

### پخش زنده

```swift
import AetherNetMedia

let client = StreamSubscriber()
try await client.subscribe(to: hostUhid)

for await segment in client.segments {
    player.enqueue(segment)
}
```

---

## یکپارچه‌سازی SwiftUI

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

## تست‌ها

```bash
swift test
```

اهداف تست:

| هدف | توضیحات |
|--------|-------------|
| `FeedAggregatorTests` | محدودیت ظرفیت، حذف تکراری‌ها، ایمنی نخ |
| `MediaContentTests` | قالب‌بندی مدت زمان، ویژگی‌های محاسبه‌شده |
| `SocialGraphTests` | فالو/آنفالو، ایزوله‌سازی actor |

---

## ساختار پروژه

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

## مجوز

MIT

</div>
