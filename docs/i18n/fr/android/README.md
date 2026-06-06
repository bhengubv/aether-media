# Aether Media — Android

[English](../../../../android/README.md) · [Français](README.md) · [Español](../../es/android/README.md) · [العربية](../../ar/android/README.md) · [中文简体](../../zh-CN/android/README.md) · [日本語](../../ja/android/README.md) · [Deutsch](../../de/android/README.md) · [Português (BR)](../../pt-BR/android/README.md) · [Русский](../../ru/android/README.md) · [فارسی](../../fa/android/README.md) · [한국어](../../ko/android/README.md)

Deux applications Android construites sur Jetpack Compose et media3/ExoPlayer, offrant l'expérience complète Aether Media sur téléphones et Android TV — incluant la découverte hors ligne par maillage, la diffusion en direct, les soirées cinéma partagées et les interactions sociales — sans nécessiter de connexion internet.

---

## Applications

| Module | Package | Cible |
|--------|---------|-------|
| `media/` | `aethermesh.media` | Téléphone / tablette (Jetpack Compose) |
| `media-tv/` | `aethermesh.media.tv` | Android TV (lean-back, navigation D-pad) |

---

## Prérequis

- Android Studio Hedgehog (2023.1) ou version ultérieure
- Android SDK : `compileSdk 35`, `minSdk 26`
- Kotlin `2.1.0`
- AGP `8.7.3`
- Java 17

---

## Compilation

```bash
# Application téléphone
cd media
./gradlew assembleDebug

# Application TV
cd media-tv
./gradlew assembleDebug
```

### Build de release

```bash
./gradlew assembleRelease
```

Définissez les identifiants de signature dans `local.properties` ou via des variables d'environnement avant de compiler un APK de release.

---

## Exécuter les tests

```bash
./gradlew test          # unit tests
./gradlew connectedTest # instrumented tests (device / emulator required)
```

---

## Architecture

Les deux applications suivent le même patron MVVM construit sur les composants Jetpack :

```
UI Layer       — Compose screens + ViewModels
Domain Layer   — Use-cases (shared with kotlin/ JVM module)
Data Layer     — Aether mesh transport via aether-protocol Android bindings
```

### Écrans principaux (application téléphone)

| Écran | Description |
|-------|-------------|
| Home | Fil de contenu des créateurs suivis |
| Nearby | Flux en direct découverts par maillage (sans internet requis) |
| Library | Médias locaux et téléchargés |
| Watch Together | Sessions de visionnage partagé actives |
| Profile | Identité AetherMeshTag et chaîne du créateur |

### Écrans principaux (application TV)

| Écran | Description |
|-------|-------------|
| Browse | Navigateur de contenu au style Leanback |
| Playback | ExoPlayer plein écran avec contrôles D-pad |
| Nearby | Découverte des pairs par maillage affichée sous forme de rangée de cartes |

---

## Moteur multimédia

Les deux applications utilisent **media3/ExoPlayer** pour la lecture :

- Diffusion adaptative HLS et DASH depuis le maillage Aether local
- Prise en charge des pistes de sous-titres (SRT, VTT)
- Lecture en arrière-plan via `MediaSessionService`
- Image dans l'image (PiP) sur Android 8.0+

---

## Intégration du maillage

Les applications se lient au service Android du protocole Aether au démarrage :

```kotlin
// Resolve nearby peers with streaming capability
aetherClient.handshake.peerNegotiated
    .filter { it.capabilities.streaming }
    .collect { peer -> nearbyFeed.add(peer) }
```

Ordre de négociation du transport : **NearLink → BLE → Wi-Fi Direct → HTTP relay**.

Les morceaux de contenu sont distribués via `IContentService` ; les flux en direct utilisent `IStreamingService`. Tout fonctionne en pair-à-pair sans serveur central.

---

## Soirées cinéma partagées

```kotlin
// Host a watch party
val session = watchTogether.hostAsync(contentHash)

// Guests join by AetherMeshTag
watchTogether.joinAsync(hostUhid)
```

La lecture est synchronisée à ±100 ms près (compensation du RTT). Les réactions en emoji se superposent à la vidéo en temps réel.

---

## Dépendances

| Bibliothèque | Rôle |
|--------------|------|
| `media3-exoplayer` | Lecture vidéo/audio |
| `media3-session` | Session multimédia + lecture en arrière-plan |
| `androidx.compose.ui` | Boîte à outils UI |
| `androidx.leanback` | Navigation TV (media-tv uniquement) |
| `aether-protocol-android` | Transport par maillage |

---

## Structure du projet

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

## Licence

MIT
