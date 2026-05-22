# Aether Media — Kotlin 구현

[English](../../../../kotlin/README.md) · [Français](../../fr/kotlin/README.md) · [Español](../../es/kotlin/README.md) · [العربية](../../ar/kotlin/README.md) · [中文简体](../../zh-CN/kotlin/README.md) · [日本語](../../ja/kotlin/README.md) · [Deutsch](../../de/kotlin/README.md) · [Português (BR)](../../pt-BR/kotlin/README.md) · [Русский](../../ru/kotlin/README.md) · [فارسی](../../fa/kotlin/README.md) · [한국어](README.md)

Aether Media의 핵심 도메인 모델, 소셜 그래프, 피드 집계 로직을 제공하는 Kotlin/JVM 라이브러리입니다. Android 앱(`android/`)과 JVM 서버/데스크톱 타깃 간에 공유됩니다. C# 참조 구현과 와이어 포맷 호환성을 유지합니다.

> **Android 앱**은 [`../android/`](../android/README.md)에 있습니다. 이 모듈은 JVM 이식 가능한 비즈니스 로직을 포함하며, ExoPlayer 통합은 Android 전용으로 해당 디렉터리에 위치합니다.

---

## 요구 사항

- JVM 17+
- Kotlin 1.9.22
- Gradle 8.5+

---

## 프로젝트에 추가하기

```kotlin
// build.gradle.kts
implementation("dev.aether:aether-media:1.0.0")
```

또는 소스에서 빌드:

```bash
./gradlew build
```

---

## 테스트 실행

```bash
./gradlew test
```

---

## 주요 클래스

### `MediaContent`

SHA-256 해시로 주소가 지정된 미디어 콘텐츠를 나타내는 불변 값 타입입니다.

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

AetherTag에 연결된 크리에이터 신원 정보입니다.

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

소셜 신호와 콘텐츠를 결합한 피드 항목입니다.

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

스레드 안전한 팔로우/언팔로우 저장소입니다.

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

최대 500개 항목으로 제한되며, `contentHash`로 중복을 제거하고 스레드 안전합니다.

```kotlin
val agg = FeedAggregator(capacity = 500)

agg.push(item)
println(agg.size)   // 1

val snapshot = agg.snapshot()
agg.clear()
```

---

## 재생 시간 형식화

`MediaContent.formattedDuration`은 모든 플랫폼에서 일관된 출력을 생성합니다:

| `durationMs` | 출력 |
|-------------|--------|
| `0` | `"Live"` |
| `90_000` | `"1:30"` |
| `3_661_000` | `"1:01:01"` |

---

## 코루틴

모든 I/O 바운드 작업은 `kotlinx.coroutines`로 지원되는 `suspend` 함수입니다. 라이브러리는 자체 `CoroutineScope`를 생성하지 않으며, 호출자가 스코프를 제공합니다.

```kotlin
scope.launch {
    val feed = feedRepository.fetch(viewerUhid)
    feedAggregator.pushAll(feed)
}
```

---

## 와이어 호환성

직렬화는 C# 참조와 동일한 필드 이름을 사용하는 `kotlinx.serialization`을 사용합니다. 언어 간 픽스처 테스트를 실행하세요:

```bash
./gradlew test --tests "*.WireCompatibilityTest"
```

---

## 프로젝트 구조

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

## 라이선스

MIT
