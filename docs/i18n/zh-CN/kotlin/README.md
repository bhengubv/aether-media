# Aether Media — Kotlin 实现

[English](../../../../kotlin/README.md) · [Français](../../fr/kotlin/README.md) · [Español](../../es/kotlin/README.md) · [العربية](../../ar/kotlin/README.md) · [中文简体](README.md) · [日本語](../../ja/kotlin/README.md) · [Deutsch](../../de/kotlin/README.md) · [Português (BR)](../../pt-BR/kotlin/README.md) · [Русский](../../ru/kotlin/README.md) · [فارسی](../../fa/kotlin/README.md) · [한국어](../../ko/kotlin/README.md)

一个 Kotlin/JVM 库，提供 Aether Media 的核心领域模型、社交关系图谱和信息流聚合逻辑。在 Android 应用（位于 `android/`）与 JVM 服务端/桌面端目标之间共享。线格式与 C# 参考实现完全兼容。

> **Android 应用**位于 [`../android/`](../android/README.md)。本模块包含可跨 JVM 移植的业务逻辑；ExoPlayer 集成仅限 Android，位于该目录中。

---

## 环境要求

- JVM 17+
- Kotlin 1.9.22
- Gradle 8.5+

---

## 添加到项目

```kotlin
// build.gradle.kts
implementation("dev.aether:aether-media:1.0.0")
```

或从源码构建：

```bash
./gradlew build
```

---

## 运行测试

```bash
./gradlew test
```

---

## 核心类

### `MediaContent`

不可变值类型，代表一个以 SHA-256 哈希寻址的媒体内容片段。

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

与 AetherNetTag 关联的创作者身份。

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

将内容与社交信号结合的信息流条目。

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

线程安全的关注/取消关注存储。

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

上限为 500 条，按 `contentHash` 去重，线程安全。

```kotlin
val agg = FeedAggregator(capacity = 500)

agg.push(item)
println(agg.size)   // 1

val snapshot = agg.snapshot()
agg.clear()
```

---

## 时长格式化

`MediaContent.formattedDuration` 在所有平台上产生一致的输出：

| `durationMs` | 输出 |
|-------------|--------|
| `0` | `"Live"` |
| `90_000` | `"1:30"` |
| `3_661_000` | `"1:01:01"` |

---

## 协程

所有 I/O 密集型操作均为由 `kotlinx.coroutines` 支持的 `suspend` 函数。本库不自行创建 `CoroutineScope`，由调用方提供作用域。

```kotlin
scope.launch {
    val feed = feedRepository.fetch(viewerUhid)
    feedAggregator.pushAll(feed)
}
```

---

## 线格式兼容性

序列化使用 `kotlinx.serialization`，字段名与 C# 参考实现相同。运行跨语言固定数据测试：

```bash
./gradlew test --tests "*.WireCompatibilityTest"
```

---

## 项目结构

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

## 许可证

MIT
