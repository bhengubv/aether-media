# Aether Media — Kotlin Implementation

[English](README.md) · [Français](../docs/i18n/fr/kotlin/README.md) · [Español](../docs/i18n/es/kotlin/README.md) · [العربية](../docs/i18n/ar/kotlin/README.md) · [中文简体](../docs/i18n/zh-CN/kotlin/README.md) · [日本語](../docs/i18n/ja/kotlin/README.md) · [Deutsch](../docs/i18n/de/kotlin/README.md) · [Português (BR)](../docs/i18n/pt-BR/kotlin/README.md) · [Русский](../docs/i18n/ru/kotlin/README.md) · [فارسی](../docs/i18n/fa/kotlin/README.md) · [한국어](../docs/i18n/ko/kotlin/README.md)

A Kotlin/JVM library providing the core domain models, social graph, and feed aggregation logic for Aether Media. Shared between the Android app (in `android/`) and JVM server/desktop targets. Wire-format compatible with the C# reference implementation.

> **Android apps** live in [`../android/`](../android/README.md). This module contains the JVM-portable business logic; ExoPlayer integration is Android-only and resides there.

---

## Requirements

- JVM 17+
- Kotlin 1.9.22
- Gradle 8.5+

---

## Add to your project

```kotlin
// build.gradle.kts
implementation("dev.aether:aether-media:1.0.0")
```

Or build from source:

```bash
./gradlew build
```

---

## Run tests

```bash
./gradlew test
```

---

## Key classes

### `MediaContent`

Immutable value type representing a piece of media content addressed by SHA-256 hash.

```kotlin
data class MediaContent(
    val contentHash: String,
    val title: String,
    val durationMs: Long,
    val codec: String,
    val contentType: String,
    val creatorUhid: String,
    val sizeBytes: Long,
) {
    val isVideo: Boolean get() = contentType.startsWith("video/")
    val isAudio: Boolean get() = contentType.startsWith("audio/")
    val formattedDuration: String get() = formatDuration(durationMs)
}
```

### `MediaProfile`

Creator identity linked to an AetherNetTag.

```kotlin
data class MediaProfile(
    val uhid: String,
    val displayName: String,
    val aetherTag: String,
    val avatarHash: String?,
    val bio: String,
    val followerCount: Int,
    val contentCount: Int,
)
```

### `MediaFeedItem`

Feed entry combining content with social signals.

```kotlin
data class MediaFeedItem(
    val content: MediaContent,
    val likeCount: Int,
    val shareCount: Int,
    val commentCount: Int,
    val watchCount: Int,
)
```

### `SocialGraph`

Thread-safe follow/unfollow store.

```kotlin
val graph = SocialGraph()

graph.follow("peer-uhid-abc123")
graph.follow("peer-uhid-def456")

println(graph.followingCount)          // 2
println(graph.isFollowing("peer-uhid-abc123")) // true

graph.unfollow("peer-uhid-abc123")
println(graph.followingCount)          // 1
```

### `FeedAggregator`

Capped at 500 items, deduplicates by `contentHash`, thread-safe.

```kotlin
val agg = FeedAggregator(capacity = 500)

agg.push(item)
println(agg.size)   // 1

val snapshot = agg.snapshot()
agg.clear()
```

---

## Duration formatting

`MediaContent.formattedDuration` produces consistent output across all platforms:

| `durationMs` | Output |
|-------------|--------|
| `0` | `"Live"` |
| `90_000` | `"1:30"` |
| `3_661_000` | `"1:01:01"` |

---

## Coroutines

All I/O-bound operations are `suspend` functions backed by `kotlinx.coroutines`. The library does not create its own `CoroutineScope`; callers supply the scope.

```kotlin
scope.launch {
    val feed = feedRepository.fetch(viewerUhid)
    feedAggregator.pushAll(feed)
}
```

---

## Wire compatibility

Serialisation uses `kotlinx.serialization` with the same field names as the C# reference. Run the cross-language fixture tests:

```bash
./gradlew test --tests "*.WireCompatibilityTest"
```

---

## Project layout

```
kotlin/
├── src/
│   ├── main/kotlin/
│   │   ├── feed/          # FeedAggregator
│   │   ├── models/        # MediaContent, MediaProfile, MediaFeedItem, MediaReaction
│   │   └── social/        # SocialGraph
│   └── test/kotlin/
│       ├── feed/
│       ├── MediaContentTest.kt
│       └── SocialGraphTest.kt
└── build.gradle.kts
```

---

## License

MIT
