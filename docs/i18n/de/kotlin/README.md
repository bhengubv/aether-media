# Aether Media — Kotlin-Implementierung

[English](../../../../kotlin/README.md) · [Français](../../fr/kotlin/README.md) · [Español](../../es/kotlin/README.md) · [العربية](../../ar/kotlin/README.md) · [中文简体](../../zh-CN/kotlin/README.md) · [日本語](../../ja/kotlin/README.md) · [Deutsch](README.md) · [Português (BR)](../../pt-BR/kotlin/README.md) · [Русский](../../ru/kotlin/README.md) · [فارسی](../../fa/kotlin/README.md) · [한국어](../../ko/kotlin/README.md)

Eine Kotlin/JVM-Bibliothek, die die grundlegenden Domänenmodelle, den sozialen Graphen und die Feed-Aggregationslogik für Aether Media bereitstellt. Wird zwischen der Android-App (in `android/`) und JVM-Server-/Desktop-Zielen geteilt. Wire-Format-kompatibel mit der C#-Referenzimplementierung.

> **Android-Apps** befinden sich unter [`../android/`](../android/README.md). Dieses Modul enthält die JVM-portable Geschäftslogik; die ExoPlayer-Integration ist Android-exklusiv und befindet sich dort.

---

## Voraussetzungen

- JVM 17+
- Kotlin 1.9.22
- Gradle 8.5+

---

## Zum Projekt hinzufügen

```kotlin
// build.gradle.kts
implementation("dev.aether:aether-media:1.0.0")
```

Oder aus dem Quellcode erstellen:

```bash
./gradlew build
```

---

## Tests ausführen

```bash
./gradlew test
```

---

## Schlüsselklassen

### `MediaContent`

Unveränderlicher Werttyp, der ein Medienelement repräsentiert, das durch einen SHA-256-Hash adressiert wird.

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

Ersteller-Identität, die mit einem AetherMeshTag verknüpft ist.

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

Feed-Eintrag, der Inhalt mit sozialen Signalen kombiniert.

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

Thread-sicherer Speicher für Folgen/Entfolgen.

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

Auf 500 Einträge begrenzt, dedupliziert nach `contentHash`, thread-sicher.

```kotlin
val agg = FeedAggregator(capacity = 500)

agg.push(item)
println(agg.size)   // 1

val snapshot = agg.snapshot()
agg.clear()
```

---

## Dauerformatierung

`MediaContent.formattedDuration` erzeugt eine einheitliche Ausgabe über alle Plattformen hinweg:

| `durationMs` | Ausgabe |
|-------------|--------|
| `0` | `"Live"` |
| `90_000` | `"1:30"` |
| `3_661_000` | `"1:01:01"` |

---

## Coroutines

Alle I/O-gebundenen Operationen sind `suspend`-Funktionen, die von `kotlinx.coroutines` unterstützt werden. Die Bibliothek erstellt keinen eigenen `CoroutineScope`; Aufrufer stellen den Scope bereit.

```kotlin
scope.launch {
    val feed = feedRepository.fetch(viewerUhid)
    feedAggregator.pushAll(feed)
}
```

---

## Wire-Kompatibilität

Die Serialisierung verwendet `kotlinx.serialization` mit denselben Feldnamen wie die C#-Referenz. Führen Sie die sprachübergreifenden Fixture-Tests aus:

```bash
./gradlew test --tests "*.WireCompatibilityTest"
```

---

## Projektstruktur

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

## Lizenz

MIT
