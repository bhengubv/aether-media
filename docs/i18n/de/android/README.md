# Aether Media — Android

[English](../../../../android/README.md) · [Français](../../fr/android/README.md) · [Español](../../es/android/README.md) · [العربية](../../ar/android/README.md) · [中文简体](../../zh-CN/android/README.md) · [日本語](../../ja/android/README.md) · [Deutsch](README.md) · [Português (BR)](../../pt-BR/android/README.md) · [Русский](../../ru/android/README.md) · [فارسی](../../fa/android/README.md) · [한국어](../../ko/android/README.md)

Zwei Android-Anwendungen, aufgebaut auf Jetpack Compose und media3/ExoPlayer, die das vollständige Aether-Media-Erlebnis auf Smartphones und Android TV bieten — einschließlich Offline-Mesh-Erkennung, Live-Streaming, Watch-Parties und sozialen Interaktionen — ohne Internetverbindung.

---

## Anwendungen

| Modul | Paket | Zielplattform |
|-------|-------|---------------|
| `media/` | `aether.media` | Smartphone / Tablet (Jetpack Compose) |
| `media-tv/` | `aether.media.tv` | Android TV (Lean-Back, D-Pad-Navigation) |

---

## Voraussetzungen

- Android Studio Hedgehog (2023.1) oder neuer
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

### Release-Build

```bash
./gradlew assembleRelease
```

Tragen Sie die Signierungsdaten in `local.properties` ein oder übergeben Sie sie als Umgebungsvariablen, bevor Sie ein Release-APK erstellen.

---

## Tests ausführen

```bash
./gradlew test          # unit tests
./gradlew connectedTest # instrumented tests (device / emulator required)
```

---

## Architektur

Beide Apps folgen demselben MVVM-Muster, das auf Jetpack-Komponenten basiert:

```
UI Layer       — Compose screens + ViewModels
Domain Layer   — Use-cases (shared with kotlin/ JVM module)
Data Layer     — Aether mesh transport via aether-protocol Android bindings
```

### Wichtige Bildschirme (Smartphone-App)

| Bildschirm | Beschreibung |
|------------|-------------|
| Home | Feed mit Inhalten von abonnierten Erstellern |
| Nearby | Per Mesh erkannte Live-Streams (keine Internetverbindung erforderlich) |
| Library | Lokale und heruntergeladene Medien |
| Watch Together | Aktive Watch-Party-Sitzungen |
| Profile | AetherTag-Identität und Ersteller-Kanal |

### Wichtige Bildschirme (TV-App)

| Bildschirm | Beschreibung |
|------------|-------------|
| Browse | Inhaltsbrowser im Lean-Back-Stil |
| Playback | Vollbild-ExoPlayer mit D-Pad-Steuerung |
| Nearby | Mesh-Peer-Erkennung als Kartenreihe dargestellt |

---

## Media-Engine

Beide Apps verwenden **media3/ExoPlayer** für die Wiedergabe:

- Adaptives HLS- und DASH-Streaming aus dem lokalen Aether-Mesh
- Untertitelspuren (SRT, VTT)
- Hintergrundwiedergabe über `MediaSessionService`
- Bild-in-Bild (PiP) ab Android 8.0

---

## Mesh-Integration

Die Apps binden beim Start an den Aether-Protocol-Android-Dienst:

```kotlin
// Resolve nearby peers with streaming capability
aetherClient.handshake.peerNegotiated
    .filter { it.capabilities.streaming }
    .collect { peer -> nearbyFeed.add(peer) }
```

Reihenfolge der Transportaushandlung: **NearLink → BLE → Wi-Fi Direct → HTTP relay**.

Inhaltsfragmente werden über `IContentService` verteilt; Live-Streams nutzen `IStreamingService`. Alles funktioniert Peer-to-Peer ohne zentralen Server.

---

## Watch-Parties

```kotlin
// Host a watch party
val session = watchTogether.hostAsync(contentHash)

// Guests join by AetherTag
watchTogether.joinAsync(hostUhid)
```

Die Wiedergabe wird auf ±100 ms synchronisiert (RTT-kompensiert). Emoji-Reaktionen werden in Echtzeit über das Video eingeblendet.

---

## Abhängigkeiten

| Bibliothek | Zweck |
|-----------|-------|
| `media3-exoplayer` | Video-/Audiowiedergabe |
| `media3-session` | Media-Session und Hintergrundwiedergabe |
| `androidx.compose.ui` | UI-Toolkit |
| `androidx.leanback` | TV-Navigation (nur media-tv) |
| `aether-protocol-android` | Mesh-Transport |

---

## Projektstruktur

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

## Lizenz

MIT
