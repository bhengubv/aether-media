<div dir="rtl">

# Aether Media — Android

[English](../../../../android/README.md) · [Français](../../fr/android/README.md) · [Español](../../es/android/README.md) · [العربية](README.md) · [中文简体](../../zh-CN/android/README.md) · [日本語](../../ja/android/README.md) · [Deutsch](../../de/android/README.md) · [Português (BR)](../../pt-BR/android/README.md) · [Русский](../../ru/android/README.md) · [فارسی](../../fa/android/README.md) · [한국어](../../ko/android/README.md)

تطبيقان لنظام Android مبنيان على Jetpack Compose وmedia3/ExoPlayer، يوفّران تجربة Aether Media الكاملة على الهواتف وأجهزة Android TV — بما يشمل الاكتشاف عبر الشبكة اللاسلكية دون اتصال، والبث المباشر، وجلسات المشاهدة الجماعية، والتفاعلات الاجتماعية — دون الحاجة إلى أي اتصال بالإنترنت.

---

## التطبيقات

| الوحدة | الحزمة | الهدف |
|--------|---------|--------|
| `media/` | `aethernet.media` | الهاتف / الجهاز اللوحي (Jetpack Compose) |
| `media-tv/` | `aethernet.media.tv` | Android TV (تنقل بـ D-pad، واجهة lean-back) |

---

## المتطلبات

- Android Studio Hedgehog (2023.1) أو أحدث
- Android SDK: `compileSdk 35`، `minSdk 26`
- Kotlin `2.1.0`
- AGP `8.7.3`
- Java 17

---

## البناء

```bash
# Phone app
cd media
./gradlew assembleDebug

# TV app
cd media-tv
./gradlew assembleDebug
```

### بناء إصدار الإنتاج

```bash
./gradlew assembleRelease
```

قم بتعيين بيانات التوقيع في `local.properties` أو عبر متغيرات البيئة قبل بناء APK الخاص بالإنتاج.

---

## تشغيل الاختبارات

```bash
./gradlew test          # unit tests
./gradlew connectedTest # instrumented tests (device / emulator required)
```

---

## البنية المعمارية

يتبع كلا التطبيقين نفس نمط MVVM المبني على مكونات Jetpack:

```
UI Layer       — Compose screens + ViewModels
Domain Layer   — Use-cases (shared with kotlin/ JVM module)
Data Layer     — Aether mesh transport via aether-protocol Android bindings
```

### الشاشات الرئيسية (تطبيق الهاتف)

| الشاشة | الوصف |
|--------|-------------|
| Home | تغذية بالمحتوى من المُنشئين المُتابَعين |
| Nearby | بثوث مباشرة مكتشَفة عبر الشبكة اللاسلكية (لا يلزم إنترنت) |
| Library | وسائط محلية ومُنزَّلة |
| Watch Together | جلسات المشاهدة الجماعية النشطة |
| Profile | هوية AetherNetTag وقناة المُنشئ |

### الشاشات الرئيسية (تطبيق TV)

| الشاشة | الوصف |
|--------|-------------|
| Browse | متصفح المحتوى بأسلوب Leanback |
| Playback | ExoPlayer بملء الشاشة مع تحكم D-pad |
| Nearby | اكتشاف الأجهزة القريبة عبر الشبكة اللاسلكية بصف من البطاقات |

---

## محرك الوسائط

يستخدم كلا التطبيقين **media3/ExoPlayer** للتشغيل:

- بث تكيّفي HLS وDASH من شبكة Aether اللاسلكية المحلية
- دعم مسارات الترجمة (SRT وVTT)
- تشغيل في الخلفية عبر `MediaSessionService`
- صورة داخل صورة (PiP) على Android 8.0 فأحدث

---

## تكامل الشبكة اللاسلكية

يرتبط التطبيقان بخدمة Aether Protocol على Android عند بدء التشغيل:

```kotlin
// Resolve nearby peers with streaming capability
aetherClient.handshake.peerNegotiated
    .filter { it.capabilities.streaming }
    .collect { peer -> nearbyFeed.add(peer) }
```

ترتيب التفاوض على وسيلة النقل: **NearLink → BLE → Wi-Fi Direct → HTTP relay**.

يتم توزيع قطع المحتوى عبر `IContentService`؛ وتستخدم البثوث المباشرة `IStreamingService`. كل شيء يعمل بين الأجهزة مباشرةً دون خادم مركزي.

---

## جلسات المشاهدة الجماعية

```kotlin
// Host a watch party
val session = watchTogether.hostAsync(contentHash)

// Guests join by AetherNetTag
watchTogether.joinAsync(hostUhid)
```

يتم مزامنة التشغيل بفارق ±100 مللي ثانية (مع تعويض زمن الذهاب والإياب). تظهر تفاعلات الرموز التعبيرية فوق الفيديو بشكل فوري.

---

## التبعيات

| المكتبة | الغرض |
|---------|---------|
| `media3-exoplayer` | تشغيل الفيديو والصوت |
| `media3-session` | جلسة الوسائط والتشغيل في الخلفية |
| `androidx.compose.ui` | مجموعة أدوات واجهة المستخدم |
| `androidx.leanback` | تنقل TV (media-tv فقط) |
| `aether-protocol-android` | نقل عبر الشبكة اللاسلكية |

---

## هيكل المشروع

```
android/
├── media/                  # Phone / tablet app
│   ├── app/
│   │   └── src/main/
│   │       ├── kotlin/     # ViewModels, screens, Compose UI
│   │       └── res/        # Layouts, drawables, strings
│   └── build.gradle.kts
└── media-tv/               # Android TV app
    ├── app/
    │   └── src/main/
    │       ├── kotlin/     # Leanback fragments, presenters
    │       └── res/
    └── build.gradle.kts
```

---

## الرخصة

MIT

</div>
