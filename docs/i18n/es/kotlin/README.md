# Aether Media — Implementación en Kotlin

[English](../../../../kotlin/README.md) · [Français](../../fr/kotlin/README.md) · [Español](README.md) · [العربية](../../ar/kotlin/README.md) · [中文简体](../../zh-CN/kotlin/README.md) · [日本語](../../ja/kotlin/README.md) · [Deutsch](../../de/kotlin/README.md) · [Português (BR)](../../pt-BR/kotlin/README.md) · [Русский](../../ru/kotlin/README.md) · [فارسی](../../fa/kotlin/README.md) · [한국어](../../ko/kotlin/README.md)

Una biblioteca Kotlin/JVM que proporciona los modelos de dominio principales, el grafo social y la lógica de agregación de feeds para Aether Media. Compartida entre la aplicación Android (en `android/`) y los destinos JVM para servidor/escritorio. Compatible en formato de cable con la implementación de referencia en C#.

> **Las aplicaciones Android** se encuentran en [`../android/`](../android/README.md). Este módulo contiene la lógica de negocio portable para JVM; la integración con ExoPlayer es exclusiva de Android y reside allí.

---

## Requisitos

- JVM 17+
- Kotlin 1.9.22
- Gradle 8.5+

---

## Añadir a tu proyecto

```kotlin
// build.gradle.kts
implementation("dev.aether:aether-media:1.0.0")
```

O compilar desde el código fuente:

```bash
./gradlew build
```

---

## Ejecutar pruebas

```bash
./gradlew test
```

---

## Clases principales

### `MediaContent`

Tipo de valor inmutable que representa un fragmento de contenido multimedia identificado por su hash SHA-256.

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

Identidad del creador vinculada a un AetherMeshTag.

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

Entrada del feed que combina contenido con señales sociales.

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

Almacén de seguimiento/dejar de seguir con seguridad para hilos.

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

Limitado a 500 elementos, deduplica por `contentHash`, seguro para hilos.

```kotlin
val agg = FeedAggregator(capacity = 500)

agg.push(item)
println(agg.size)   // 1

val snapshot = agg.snapshot()
agg.clear()
```

---

## Formato de duración

`MediaContent.formattedDuration` produce una salida coherente en todas las plataformas:

| `durationMs` | Salida |
|-------------|--------|
| `0` | `"Live"` |
| `90_000` | `"1:30"` |
| `3_661_000` | `"1:01:01"` |

---

## Corrutinas

Todas las operaciones vinculadas a I/O son funciones `suspend` respaldadas por `kotlinx.coroutines`. La biblioteca no crea su propio `CoroutineScope`; los llamantes proporcionan el scope.

```kotlin
scope.launch {
    val feed = feedRepository.fetch(viewerUhid)
    feedAggregator.pushAll(feed)
}
```

---

## Compatibilidad de cable

La serialización utiliza `kotlinx.serialization` con los mismos nombres de campo que la referencia en C#. Ejecuta las pruebas de fixture entre lenguajes:

```bash
./gradlew test --tests "*.WireCompatibilityTest"
```

---

## Estructura del proyecto

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

## Licencia

MIT
