<div dir="rtl">

# Aether Media — تنفيذ Swift

[English](../../../../swift/README.md) · [Français](../../fr/swift/README.md) · [Español](../../es/swift/README.md) · [العربية](README.md) · [中文简体](../../zh-CN/swift/README.md) · [日本語](../../ja/swift/README.md) · [Deutsch](../../de/swift/README.md) · [Português (BR)](../../pt-BR/swift/README.md) · [Русский](../../ru/swift/README.md) · [فارسی](../../fa/swift/README.md) · [한국어](../../ko/swift/README.md)

تنفيذ شامل بلغة Swift لـ Aether Media لنظامَي iOS وmacOS. يدمج AVFoundation للتشغيل، وSwiftUI للواجهة، وبروتوكول شبكة Aether للاكتشاف الشبكي للمحتوى من نظير إلى نظير والبث المباشر — كل ذلك دون خادم مركزي.

---

## المتطلبات

- Swift 5.9+
- Xcode 15+
- iOS 16+ أو macOS 13+

---

## إضافة المكتبة إلى مشروعك

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

## البناء والاختبار

```bash
swift build
swift test
```

---

## الوحدات

| الوحدة | الوصف |
|--------|-------------|
| `Models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `Feed` | `FeedAggregator` — محدود بـ 500 عنصر، مدعوم بـ Combine |
| `Social` | `SocialGraph` — متابعة/إلغاء متابعة، معزول بـ actor |
| `Player` | تكامل AVFoundation — التشغيل والانتقال والسرعة وPiP |
| `Streaming` | اشتراك البث المباشر عبر Aether وتجميع الشرائح |
| `Content` | تنزيل قطع المحتوى P2P وإدارة ذاكرة التخزين المؤقت |
| `UI` | مشاهدات SwiftUI — قائمة الخلاصة، تراكب المشغّل، بطاقة الملف الشخصي |

---

## البدء السريع

### النماذج

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

### الرسم البياني الاجتماعي

```swift
import AetherMedia

let graph = SocialGraph()
await graph.follow("peer-uhid-abc123")
await graph.follow("peer-uhid-def456")

print(await graph.followingCount)              // 2
print(await graph.isFollowing("peer-uhid-abc123")) // true

await graph.unfollow("peer-uhid-abc123")
```

### مجمّع الخلاصة

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

### المشغّل

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

### البث المباشر

```swift
import AetherMedia

let client = StreamSubscriber()
try await client.subscribe(to: hostUhid)

for await segment in client.segments {
    player.enqueue(segment)
}
```

---

## التكامل مع SwiftUI

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

## الاختبارات

```bash
swift test
```

أهداف الاختبار:

| الهدف | الوصف |
|--------|-------------|
| `FeedAggregatorTests` | حد السعة، إزالة التكرارات، أمان الخيوط |
| `MediaContentTests` | تنسيق المدة، الخصائص المحسوبة |
| `SocialGraphTests` | المتابعة/إلغاء المتابعة، عزل الـ actor |

---

## تخطيط المشروع

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

## الرخصة

MIT

</div>
