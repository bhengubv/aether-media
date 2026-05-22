# Aether Media — Kotlin 実装

[English](../../../../kotlin/README.md) · [Français](../../fr/kotlin/README.md) · [Español](../../es/kotlin/README.md) · [العربية](../../ar/kotlin/README.md) · [中文简体](../../zh-CN/kotlin/README.md) · [日本語](README.md) · [Deutsch](../../de/kotlin/README.md) · [Português (BR)](../../pt-BR/kotlin/README.md) · [Русский](../../ru/kotlin/README.md) · [فارسی](../../fa/kotlin/README.md) · [한국어](../../ko/kotlin/README.md)

Aether Media のコアドメインモデル、ソーシャルグラフ、フィード集約ロジックを提供する Kotlin/JVM ライブラリです。Android アプリ（`android/` 配下）と JVM サーバー/デスクトップターゲットの間で共有されます。C# リファレンス実装とワイヤーフォーマット互換です。

> **Android アプリ**は [`../android/`](../android/README.md) にあります。このモジュールには JVM ポータブルなビジネスロジックが含まれます。ExoPlayer の統合は Android 専用であり、そちらに配置されています。

---

## 要件

- JVM 17+
- Kotlin 1.9.22
- Gradle 8.5+

---

## プロジェクトへの追加

```kotlin
// build.gradle.kts
implementation("dev.aether:aether-media:1.0.0")
```

またはソースからビルド:

```bash
./gradlew build
```

---

## テストの実行

```bash
./gradlew test
```

---

## 主要クラス

### `MediaContent`

SHA-256 ハッシュによってアドレス指定されるメディアコンテンツを表すイミュータブル値型。

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

AetherTag にリンクされたクリエイターのアイデンティティ。

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

コンテンツとソーシャルシグナルを組み合わせたフィードエントリ。

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

スレッドセーフなフォロー/アンフォローストア。

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

最大 500 件、`contentHash` による重複排除、スレッドセーフ。

```kotlin
val agg = FeedAggregator(capacity = 500)

agg.push(item)
println(agg.size)   // 1

val snapshot = agg.snapshot()
agg.clear()
```

---

## 再生時間のフォーマット

`MediaContent.formattedDuration` はすべてのプラットフォームで一貫した出力を生成します:

| `durationMs` | 出力 |
|-------------|--------|
| `0` | `"Live"` |
| `90_000` | `"1:30"` |
| `3_661_000` | `"1:01:01"` |

---

## コルーチン

すべての I/O バウンド操作は `kotlinx.coroutines` を基盤とした `suspend` 関数です。ライブラリ自身は `CoroutineScope` を作成しません。呼び出し側がスコープを提供します。

```kotlin
scope.launch {
    val feed = feedRepository.fetch(viewerUhid)
    feedAggregator.pushAll(feed)
}
```

---

## ワイヤー互換性

シリアライゼーションは `kotlinx.serialization` を使用し、C# リファレンスと同じフィールド名を持ちます。クロス言語フィクスチャテストを実行してください:

```bash
./gradlew test --tests "*.WireCompatibilityTest"
```

---

## プロジェクト構成

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

## ライセンス

MIT
