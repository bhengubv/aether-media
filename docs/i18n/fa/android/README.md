<div dir="rtl">

# Aether Media — اندروید

[English](../../../../android/README.md) · [Français](../../fr/android/README.md) · [Español](../../es/android/README.md) · [العربية](../../ar/android/README.md) · [中文简体](../../zh-CN/android/README.md) · [日本語](../../ja/android/README.md) · [Deutsch](../../de/android/README.md) · [Português (BR)](../../pt-BR/android/README.md) · [Русский](../../ru/android/README.md) · [فارسی](README.md) · [한국어](../../ko/android/README.md)

دو اپلیکیشن اندرویدی ساخته‌شده بر پایه Jetpack Compose و media3/ExoPlayer که تجربه کامل Aether Media را روی گوشی‌ها و Android TV ارائه می‌دهند — شامل کشف مِش آفلاین، پخش زنده، جلسات تماشای مشترک و تعاملات اجتماعی — بدون نیاز به هیچ‌گونه اتصال اینترنتی.

---

## اپلیکیشن‌ها

| ماژول | پکیج | هدف |
|--------|---------|--------|
| `media/` | `aethermesh.media` | گوشی / تبلت (Jetpack Compose) |
| `media-tv/` | `aethermesh.media.tv` | Android TV (lean-back، ناوبری با D-pad) |

---

## پیش‌نیازها

- Android Studio Hedgehog (2023.1) یا جدیدتر
- Android SDK: `compileSdk 35`، `minSdk 26`
- Kotlin `2.1.0`
- AGP `8.7.3`
- Java 17

---

## ساخت

```bash
# Phone app
cd media
./gradlew assembleDebug

# TV app
cd media-tv
./gradlew assembleDebug
```

### ساخت نسخه انتشار

```bash
./gradlew assembleRelease
```

پیش از ساخت APK انتشار، اطلاعات امضا را در `local.properties` یا از طریق متغیرهای محیطی تنظیم کنید.

---

## اجرای آزمون‌ها

```bash
./gradlew test          # unit tests
./gradlew connectedTest # instrumented tests (device / emulator required)
```

---

## معماری

هر دو اپلیکیشن از همان الگوی MVVM بر پایه اجزای Jetpack پیروی می‌کنند:

```
UI Layer       — Compose screens + ViewModels
Domain Layer   — Use-cases (shared with kotlin/ JVM module)
Data Layer     — Aether mesh transport via aether-protocol Android bindings
```

### صفحه‌های کلیدی (اپلیکیشن گوشی)

| صفحه | توضیح |
|--------|-------------|
| Home | فید محتوا از سازندگانی که دنبال می‌کنید |
| Nearby | پخش‌های زنده کشف‌شده از طریق مِش (بدون اینترنت) |
| Library | رسانه محلی و دانلودشده |
| Watch Together | جلسات فعال تماشای مشترک |
| Profile | هویت AetherMeshTag و کانال سازنده |

### صفحه‌های کلیدی (اپلیکیشن TV)

| صفحه | توضیح |
|--------|-------------|
| Browse | مرورگر محتوا به سبک Leanback |
| Playback | ExoPlayer تمام‌صفحه با کنترل‌های D-pad |
| Nearby | کشف همتایان مِش نمایش‌داده‌شده به‌صورت ردیف کارت |

---

## موتور رسانه

هر دو اپلیکیشن از **media3/ExoPlayer** برای پخش استفاده می‌کنند:

- استریمینگ تطبیقی HLS و DASH از مِش محلی Aether
- پشتیبانی از زیرنویس (SRT، VTT)
- پخش در پس‌زمینه از طریق `MediaSessionService`
- Picture-in-picture (PiP) در اندروید 8.0 به بالا

---

## یکپارچه‌سازی مِش

اپلیکیشن‌ها هنگام راه‌اندازی به سرویس Android پروتکل Aether متصل می‌شوند:

```kotlin
// Resolve nearby peers with streaming capability
aetherClient.handshake.peerNegotiated
    .filter { it.capabilities.streaming }
    .collect { peer -> nearbyFeed.add(peer) }
```

ترتیب مذاکره انتقال: **NearLink → BLE → Wi-Fi Direct → HTTP relay**.

قطعه‌های محتوا از طریق `IContentService` توزیع می‌شوند؛ پخش‌های زنده از `IStreamingService` استفاده می‌کنند. همه چیز به‌صورت همتا-به-همتا و بدون سرور مرکزی کار می‌کند.

---

## جلسات تماشای مشترک

```kotlin
// Host a watch party
val session = watchTogether.hostAsync(contentHash)

// Guests join by AetherMeshTag
watchTogether.joinAsync(hostUhid)
```

پخش در بازه ±۱۰۰ میلی‌ثانیه (با جبران RTT) همگام‌سازی می‌شود. واکنش‌های ایموجی به‌صورت بلادرنگ روی ویدیو نمایش داده می‌شوند.

---

## وابستگی‌ها

| کتابخانه | هدف |
|---------|---------|
| `media3-exoplayer` | پخش ویدیو/صدا |
| `media3-session` | جلسه رسانه + پخش در پس‌زمینه |
| `androidx.compose.ui` | جعبه‌ابزار رابط کاربری |
| `androidx.leanback` | ناوبری TV (فقط media-tv) |
| `aether-protocol-android` | انتقال مِش |

---

## ساختار پروژه

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

## مجوز

MIT

</div>
