# Aether Media — Implementação em Kotlin

[English](../../../../kotlin/README.md) · [Français](../../fr/kotlin/README.md) · [Español](../../es/kotlin/README.md) · [العربية](../../ar/kotlin/README.md) · [中文简体](../../zh-CN/kotlin/README.md) · [日本語](../../ja/kotlin/README.md) · [Deutsch](../../de/kotlin/README.md) · [Português (BR)](README.md) · [Русский](../../ru/kotlin/README.md) · [فارسی](../../fa/kotlin/README.md) · [한국어](../../ko/kotlin/README.md)

Uma biblioteca Kotlin/JVM que fornece os modelos de domínio principais, o grafo social e a lógica de agregação de feed para o Aether Media. Compartilhada entre o aplicativo Android (em `android/`) e alvos JVM para servidor/desktop. Compatível em formato de serialização com a implementação de referência em C#.

> **Os aplicativos Android** estão em [`../android/`](../android/README.md). Este módulo contém a lógica de negócio portável para JVM; a integração com ExoPlayer é exclusiva para Android e reside naquele diretório.

---

## Requisitos

- JVM 17+
- Kotlin 1.9.22
- Gradle 8.5+

---

## Adicionar ao seu projeto

```kotlin
// build.gradle.kts
implementation("dev.aether:aether-media:1.0.0")
```

Ou compilar a partir do código-fonte:

```bash
./gradlew build
```

---

## Executar testes

```bash
./gradlew test
```

---

## Classes principais

### `MediaContent`

Tipo de valor imutável que representa um conteúdo de mídia endereçado por hash SHA-256.

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

Identidade do criador vinculada a uma AetherTag.

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

Entrada do feed combinando conteúdo com sinais sociais.

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

Armazenamento de seguir/deixar de seguir com segurança para threads.

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

Limitado a 500 itens, deduplica por `contentHash`, com segurança para threads.

```kotlin
val agg = FeedAggregator(capacity = 500)

agg.push(item)
println(agg.size)   // 1

val snapshot = agg.snapshot()
agg.clear()
```

---

## Formatação de duração

`MediaContent.formattedDuration` produz saída consistente em todas as plataformas:

| `durationMs` | Saída |
|-------------|--------|
| `0` | `"Live"` |
| `90_000` | `"1:30"` |
| `3_661_000` | `"1:01:01"` |

---

## Corrotinas

Todas as operações com I/O são funções `suspend` sustentadas por `kotlinx.coroutines`. A biblioteca não cria seu próprio `CoroutineScope`; os chamadores fornecem o escopo.

```kotlin
scope.launch {
    val feed = feedRepository.fetch(viewerUhid)
    feedAggregator.pushAll(feed)
}
```

---

## Compatibilidade de formato

A serialização usa `kotlinx.serialization` com os mesmos nomes de campo da referência em C#. Execute os testes de fixtures entre linguagens:

```bash
./gradlew test --tests "*.WireCompatibilityTest"
```

---

## Estrutura do projeto

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

## Licença

MIT
