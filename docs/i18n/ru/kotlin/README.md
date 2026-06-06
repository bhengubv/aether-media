# Aether Media — Реализация на Kotlin

[English](../../../../kotlin/README.md) · [Français](../../fr/kotlin/README.md) · [Español](../../es/kotlin/README.md) · [العربية](../../ar/kotlin/README.md) · [中文简体](../../zh-CN/kotlin/README.md) · [日本語](../../ja/kotlin/README.md) · [Deutsch](../../de/kotlin/README.md) · [Português (BR)](../../pt-BR/kotlin/README.md) · [Русский](README.md) · [فارسی](../../fa/kotlin/README.md) · [한국어](../../ko/kotlin/README.md)

Библиотека на Kotlin/JVM, предоставляющая основные доменные модели, граф социальных связей и логику агрегации ленты для Aether Media. Используется совместно приложением для Android (в `android/`) и целевыми платформами JVM server/desktop. Совместима по формату проводного протокола с эталонной реализацией на C#.

> **Приложения для Android** находятся в [`../android/`](../android/README.md). Этот модуль содержит переносимую на JVM бизнес-логику; интеграция с ExoPlayer предназначена только для Android и расположена там.

---

## Требования

- JVM 17+
- Kotlin 1.9.22
- Gradle 8.5+

---

## Добавление в проект

```kotlin
// build.gradle.kts
implementation("dev.aether:aether-media:1.0.0")
```

Или сборка из исходников:

```bash
./gradlew build
```

---

## Запуск тестов

```bash
./gradlew test
```

---

## Основные классы

### `MediaContent`

Неизменяемый тип-значение, представляющий единицу медиаконтента, адресуемую по SHA-256 хэшу.

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

Идентификационные данные создателя контента, связанные с AetherNetTag.

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

Элемент ленты, объединяющий контент с социальными сигналами.

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

Потокобезопасное хранилище подписок и отписок.

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

Ограничен 500 элементами, дедублирует по `contentHash`, потокобезопасен.

```kotlin
val agg = FeedAggregator(capacity = 500)

agg.push(item)
println(agg.size)   // 1

val snapshot = agg.snapshot()
agg.clear()
```

---

## Форматирование длительности

`MediaContent.formattedDuration` обеспечивает единообразный вывод на всех платформах:

| `durationMs` | Вывод |
|-------------|--------|
| `0` | `"Live"` |
| `90_000` | `"1:30"` |
| `3_661_000` | `"1:01:01"` |

---

## Корутины

Все операции, связанные с вводом-выводом, реализованы как `suspend`-функции на основе `kotlinx.coroutines`. Библиотека не создаёт собственного `CoroutineScope`; область видимости предоставляется вызывающим кодом.

```kotlin
scope.launch {
    val feed = feedRepository.fetch(viewerUhid)
    feedAggregator.pushAll(feed)
}
```

---

## Совместимость проводного протокола

Сериализация использует `kotlinx.serialization` с теми же именами полей, что и эталонная реализация на C#. Запустите кросс-языковые тесты фикстур:

```bash
./gradlew test --tests "*.WireCompatibilityTest"
```

---

## Структура проекта

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

## Лицензия

MIT
