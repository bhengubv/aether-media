<div dir="rtl">

# Aether Media — تنفيذ Kotlin

[English](../../../../kotlin/README.md) · [Français](../../fr/kotlin/README.md) · [Español](../../es/kotlin/README.md) · [العربية](README.md) · [中文简体](../../zh-CN/kotlin/README.md) · [日本語](../../ja/kotlin/README.md) · [Deutsch](../../de/kotlin/README.md) · [Português (BR)](../../pt-BR/kotlin/README.md) · [Русский](../../ru/kotlin/README.md) · [فارسی](../../fa/kotlin/README.md) · [한국어](../../ko/kotlin/README.md)

مكتبة Kotlin/JVM تُوفِّر نماذج المجال الأساسية، والرسم البياني الاجتماعي، ومنطق تجميع الخلاصات لـ Aether Media. مشتركة بين تطبيق Android (في `android/`) وأهداف JVM للخادم والسطح المكتبي. متوافقة مع تنسيق السلك مع التنفيذ المرجعي بـ C#.

> **تطبيقات Android** موجودة في [`../android/`](../android/README.md). يحتوي هذا الوحدة على منطق الأعمال القابل للنقل عبر JVM؛ تكامل ExoPlayer مخصص لـ Android فقط ويقع هناك.

---

## المتطلبات

- JVM 17+
- Kotlin 1.9.22
- Gradle 8.5+

---

## إضافة المكتبة إلى مشروعك

```kotlin
// build.gradle.kts
implementation("dev.aether:aether-media:1.0.0")
```

أو قم بالبناء من المصدر:

```bash
./gradlew build
```

---

## تشغيل الاختبارات

```bash
./gradlew test
```

---

## الفئات الرئيسية

### `MediaContent`

نوع قيمة غير قابل للتغيير يمثل قطعة من المحتوى الإعلامي مُعنوَنة بتجزئة SHA-256.

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

هوية المنشئ مرتبطة بـ AetherMeshTag.

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

إدخال في الخلاصة يجمع المحتوى مع الإشارات الاجتماعية.

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

مخزن متابعة/إلغاء متابعة آمن للخيوط.

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

محدود بـ 500 عنصر، يُزيل التكرارات بحسب `contentHash`، وآمن للخيوط.

```kotlin
val agg = FeedAggregator(capacity = 500)

agg.push(item)
println(agg.size)   // 1

val snapshot = agg.snapshot()
agg.clear()
```

---

## تنسيق المدة الزمنية

تُنتج `MediaContent.formattedDuration` مخرجات متسقة عبر جميع المنصات:

| `durationMs` | المخرجات |
|-------------|--------|
| `0` | `"Live"` |
| `90_000` | `"1:30"` |
| `3_661_000` | `"1:01:01"` |

---

## الكوروتينات

جميع العمليات المرتبطة بالإدخال/الإخراج هي دوال `suspend` مدعومة بـ `kotlinx.coroutines`. لا تُنشئ المكتبة نطاق `CoroutineScope` خاصاً بها؛ يوفر المستدعون النطاق بأنفسهم.

```kotlin
scope.launch {
    val feed = feedRepository.fetch(viewerUhid)
    feedAggregator.pushAll(feed)
}
```

---

## التوافق مع السلك

تستخدم التسلسلية `kotlinx.serialization` بأسماء حقول مطابقة للتنفيذ المرجعي بـ C#. قم بتشغيل اختبارات الثوابت عبر اللغات:

```bash
./gradlew test --tests "*.WireCompatibilityTest"
```

---

## تخطيط المشروع

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

## الرخصة

MIT

</div>
