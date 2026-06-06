# Aether Media — Implémentation Kotlin

[English](../../../../kotlin/README.md) · [Français](README.md) · [Español](../../es/kotlin/README.md) · [العربية](../../ar/kotlin/README.md) · [中文简体](../../zh-CN/kotlin/README.md) · [日本語](../../ja/kotlin/README.md) · [Deutsch](../../de/kotlin/README.md) · [Português (BR)](../../pt-BR/kotlin/README.md) · [Русский](../../ru/kotlin/README.md) · [فارسی](../../fa/kotlin/README.md) · [한국어](../../ko/kotlin/README.md)

Une bibliothèque Kotlin/JVM fournissant les modèles de domaine fondamentaux, le graphe social et la logique d'agrégation de flux pour Aether Media. Partagée entre l'application Android (dans `android/`) et les cibles JVM serveur/bureau. Compatible au niveau du format filaire avec l'implémentation de référence C#.

> **Les applications Android** se trouvent dans [`../android/`](../android/README.md). Ce module contient la logique métier portable JVM ; l'intégration ExoPlayer est réservée à Android et réside dans ce répertoire.

---

## Prérequis

- JVM 17+
- Kotlin 1.9.22
- Gradle 8.5+

---

## Ajouter à votre projet

```kotlin
// build.gradle.kts
implementation("dev.aether:aether-media:1.0.0")
```

Ou compiler depuis les sources :

```bash
./gradlew build
```

---

## Exécuter les tests

```bash
./gradlew test
```

---

## Classes principales

### `MediaContent`

Type valeur immuable représentant un élément de contenu multimédia adressé par hachage SHA-256.

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

Identité du créateur liée à un AetherMeshTag.

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

Entrée de flux combinant le contenu avec les signaux sociaux.

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

Magasin thread-safe de suivis/désabonnements.

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

Limité à 500 éléments, déduplique par `contentHash`, thread-safe.

```kotlin
val agg = FeedAggregator(capacity = 500)

agg.push(item)
println(agg.size)   // 1

val snapshot = agg.snapshot()
agg.clear()
```

---

## Formatage de la durée

`MediaContent.formattedDuration` produit une sortie cohérente sur toutes les plateformes :

| `durationMs` | Sortie |
|-------------|--------|
| `0` | `"Live"` |
| `90_000` | `"1:30"` |
| `3_661_000` | `"1:01:01"` |

---

## Coroutines

Toutes les opérations liées aux I/O sont des fonctions `suspend` reposant sur `kotlinx.coroutines`. La bibliothèque ne crée pas son propre `CoroutineScope` ; les appelants fournissent la portée.

```kotlin
scope.launch {
    val feed = feedRepository.fetch(viewerUhid)
    feedAggregator.pushAll(feed)
}
```

---

## Compatibilité filaire

La sérialisation utilise `kotlinx.serialization` avec les mêmes noms de champs que la référence C#. Exécutez les tests de fixtures inter-langages :

```bash
./gradlew test --tests "*.WireCompatibilityTest"
```

---

## Structure du projet

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

## Licence

MIT
