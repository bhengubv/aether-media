<div dir="rtl">

# Aether Media — پیاده‌سازی Kotlin

[English](../../../../kotlin/README.md) · [Français](../../fr/kotlin/README.md) · [Español](../../es/kotlin/README.md) · [العربية](../../ar/kotlin/README.md) · [中文简体](../../zh-CN/kotlin/README.md) · [日本語](../../ja/kotlin/README.md) · [Deutsch](../../de/kotlin/README.md) · [Português (BR)](../../pt-BR/kotlin/README.md) · [Русский](../../ru/kotlin/README.md) · [فارسی](README.md) · [한국어](../../ko/kotlin/README.md)

یک کتابخانه Kotlin/JVM که مدل‌های دامنه‌ی اصلی، گراف اجتماعی، و منطق تجمیع فید را برای Aether Media فراهم می‌کند. بین اپلیکیشن Android (در `android/`) و اهداف سرور/دسکتاپ JVM به اشتراک گذاشته شده است. از نظر فرمت انتقالی با پیاده‌سازی مرجع C# سازگار است.

> **اپلیکیشن‌های Android** در [`../android/`](../android/README.md) قرار دارند. این ماژول شامل منطق تجاری قابل انتقال به JVM است؛ یکپارچه‌سازی ExoPlayer مخصوص Android است و در آنجا قرار دارد.

---

## پیش‌نیازها

- JVM 17+
- Kotlin 1.9.22
- Gradle 8.5+

---

## افزودن به پروژه

```kotlin
// build.gradle.kts
implementation("dev.aether:aether-media:1.0.0")
```

یا ساخت از سورس:

```bash
./gradlew build
```

---

## اجرای تست‌ها

```bash
./gradlew test
```

---

## کلاس‌های کلیدی

### `MediaContent`

نوع مقدار تغییرناپذیر که یک محتوای رسانه‌ای آدرس‌دهی‌شده با هش SHA-256 را نمایش می‌دهد.

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

هویت سازنده که به یک AetherNetTag مرتبط است.

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

ورودی فید که محتوا را با سیگنال‌های اجتماعی ترکیب می‌کند.

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

فروشگاه فالو/آنفالو ایمن در برابر چند نخ.

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

با ظرفیت ۵۰۰ آیتم محدود شده، بر اساس `contentHash` تکراری‌ها را حذف می‌کند و ایمن در برابر چند نخ است.

```kotlin
val agg = FeedAggregator(capacity = 500)

agg.push(item)
println(agg.size)   // 1

val snapshot = agg.snapshot()
agg.clear()
```

---

## قالب‌بندی مدت زمان

`MediaContent.formattedDuration` خروجی یکسانی در تمام پلتفرم‌ها تولید می‌کند:

| `durationMs` | خروجی |
|-------------|--------|
| `0` | `"Live"` |
| `90_000` | `"1:30"` |
| `3_661_000` | `"1:01:01"` |

---

## کوروتین‌ها

تمام عملیات‌های وابسته به I/O توابع `suspend` هستند که توسط `kotlinx.coroutines` پشتیبانی می‌شوند. این کتابخانه `CoroutineScope` خودش را نمی‌سازد؛ فراخوان‌دهنده‌ها باید scope را فراهم کنند.

```kotlin
scope.launch {
    val feed = feedRepository.fetch(viewerUhid)
    feedAggregator.pushAll(feed)
}
```

---

## سازگاری انتقالی

سریال‌سازی از `kotlinx.serialization` با همان نام‌های فیلد پیاده‌سازی مرجع C# استفاده می‌کند. تست‌های fixture بین زبان‌ها را اجرا کنید:

```bash
./gradlew test --tests "*.WireCompatibilityTest"
```

---

## ساختار پروژه

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

## مجوز

MIT

</div>
