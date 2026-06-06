# Aether Media — Android

[English](README.md) · [Français](../docs/i18n/fr/android/README.md) · [Español](../docs/i18n/es/android/README.md) · [العربية](../docs/i18n/ar/android/README.md) · [中文简体](../docs/i18n/zh-CN/android/README.md) · [日本語](../docs/i18n/ja/android/README.md) · [Deutsch](../docs/i18n/de/android/README.md) · [Português (BR)](../docs/i18n/pt-BR/android/README.md) · [Русский](../docs/i18n/ru/android/README.md) · [فارسی](../docs/i18n/fa/android/README.md) · [한국어](../docs/i18n/ko/android/README.md)

Two Android applications built on Jetpack Compose and media3/ExoPlayer, delivering the full Aether Media experience on phones and Android TV — including offline mesh discovery, live streaming, watch parties, and social interactions — without requiring any internet connection.

---

## Applications

| Module | Package | Target |
|--------|---------|--------|
| `media/` | `aethermedia` | Phone / tablet (Jetpack Compose) |
| `media-tv/` | `aethermedia.tv` | Android TV (lean-back, D-pad navigation) |

---

## Requirements

- Android Studio Hedgehog (2023.1) or later
- Android SDK: `compileSdk 35`, `minSdk 26`
- Kotlin `2.1.0`
- AGP `8.7.3`
- Java 17

---

## Build

```bash
# Phone app
cd media
./gradlew assembleDebug

# TV app
cd media-tv
./gradlew assembleDebug
```

### Release build

```bash
./gradlew assembleRelease
```

Set signing credentials in `local.properties` or via environment variables before building a release APK.

---

## Run tests

```bash
./gradlew test          # unit tests
./gradlew connectedTest # instrumented tests (device / emulator required)
```

---

## Architecture

Both apps follow the same MVVM pattern built on Jetpack components:

```
UI Layer       — Compose screens + ViewModels
Domain Layer   — Use-cases (shared with kotlin/ JVM module)
Data Layer     — Aether mesh transport via aether-protocol Android bindings
```

### Key screens (phone app)

| Screen | Description |
|--------|-------------|
| Home | Feed of content from followed creators |
| Nearby | Mesh-discovered live streams (no internet required) |
| Library | Local and downloaded media |
| Watch Together | Active watch-party sessions |
| Profile | AetherNetTag identity and creator channel |

### Key screens (TV app)

| Screen | Description |
|--------|-------------|
| Browse | Leanback-style content browser |
| Playback | Full-screen ExoPlayer with D-pad controls |
| Nearby | Mesh peer discovery shown as a card row |

---

## Media engine

Both apps use **media3/ExoPlayer** for playback:

- HLS and DASH adaptive streaming from the local Aether mesh
- Subtitle track support (SRT, VTT)
- Background playback via `MediaSessionService`
- Picture-in-picture (PiP) on Android 8.0+

---

## Mesh integration

The apps bind to the Aether Protocol Android service at startup:

```kotlin
// Resolve nearby peers with streaming capability
aetherClient.handshake.peerNegotiated
    .filter { it.capabilities.streaming }
    .collect { peer -> nearbyFeed.add(peer) }
```

Transport negotiation order: **NearLink → BLE → Wi-Fi Direct → HTTP relay**.

Content chunks are distributed via `IContentService`; live streams use `IStreamingService`. Everything works peer-to-peer with no central server.

---

## Watch parties

```kotlin
// Host a watch party
val session = watchTogether.hostAsync(contentHash)

// Guests join by AetherNetTag
watchTogether.joinAsync(hostUhid)
```

Playback is synchronised within ±100 ms (RTT-compensated). Emoji reactions overlay the video in real time.

---

## Dependencies

| Library | Purpose |
|---------|---------|
| `media3-exoplayer` | Video/audio playback |
| `media3-session` | Media session + background playback |
| `androidx.compose.ui` | UI toolkit |
| `androidx.leanback` | TV navigation (media-tv only) |
| `aether-protocol-android` | Mesh transport |

---

## Project layout

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

## License

MIT
