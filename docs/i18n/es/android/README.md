# Aether Media — Android

[English](../../../../android/README.md) · [Français](../../fr/android/README.md) · [Español](README.md) · [العربية](../../ar/android/README.md) · [中文简体](../../zh-CN/android/README.md) · [日本語](../../ja/android/README.md) · [Deutsch](../../de/android/README.md) · [Português (BR)](../../pt-BR/android/README.md) · [Русский](../../ru/android/README.md) · [فارسی](../../fa/android/README.md) · [한국어](../../ko/android/README.md)

Dos aplicaciones Android basadas en Jetpack Compose y media3/ExoPlayer que ofrecen la experiencia completa de Aether Media en teléfonos y Android TV — incluyendo descubrimiento en malla sin conexión, transmisión en vivo, sesiones de visionado compartido e interacciones sociales — sin necesidad de conexión a internet.

---

## Aplicaciones

| Módulo | Paquete | Destino |
|--------|---------|---------|
| `media/` | `aether.media` | Teléfono / tablet (Jetpack Compose) |
| `media-tv/` | `aether.media.tv` | Android TV (lean-back, navegación con D-pad) |

---

## Requisitos

- Android Studio Hedgehog (2023.1) o posterior
- Android SDK: `compileSdk 35`, `minSdk 26`
- Kotlin `2.1.0`
- AGP `8.7.3`
- Java 17

---

## Compilación

```bash
# Aplicación para teléfono
cd media
./gradlew assembleDebug

# Aplicación para TV
cd media-tv
./gradlew assembleDebug
```

### Compilación de lanzamiento

```bash
./gradlew assembleRelease
```

Configure las credenciales de firma en `local.properties` o mediante variables de entorno antes de compilar un APK de lanzamiento.

---

## Ejecutar pruebas

```bash
./gradlew test          # pruebas unitarias
./gradlew connectedTest # pruebas instrumentadas (se requiere dispositivo o emulador)
```

---

## Arquitectura

Ambas aplicaciones siguen el mismo patrón MVVM basado en componentes de Jetpack:

```
UI Layer       — Compose screens + ViewModels
Domain Layer   — Use-cases (shared with kotlin/ JVM module)
Data Layer     — Aether mesh transport via aether-protocol Android bindings
```

### Pantallas principales (aplicación de teléfono)

| Pantalla | Descripción |
|----------|-------------|
| Inicio | Contenido de los creadores seguidos |
| Cercanos | Transmisiones en vivo descubiertas en la malla (sin internet) |
| Biblioteca | Contenido multimedia local y descargado |
| Ver juntos | Sesiones de visionado compartido activas |
| Perfil | Identidad AetherTag y canal del creador |

### Pantallas principales (aplicación de TV)

| Pantalla | Descripción |
|----------|-------------|
| Explorar | Navegador de contenido estilo Leanback |
| Reproducción | ExoPlayer en pantalla completa con controles D-pad |
| Cercanos | Descubrimiento de pares en la malla mostrado como fila de tarjetas |

---

## Motor multimedia

Ambas aplicaciones utilizan **media3/ExoPlayer** para la reproducción:

- Transmisión adaptativa HLS y DASH desde la malla local de Aether
- Compatibilidad con pistas de subtítulos (SRT, VTT)
- Reproducción en segundo plano mediante `MediaSessionService`
- Imagen en imagen (PiP) en Android 8.0+

---

## Integración con la malla

Las aplicaciones se vinculan al servicio Android del Protocolo Aether al inicio:

```kotlin
// Resolve nearby peers with streaming capability
aetherClient.handshake.peerNegotiated
    .filter { it.capabilities.streaming }
    .collect { peer -> nearbyFeed.add(peer) }
```

Orden de negociación de transporte: **NearLink → BLE → Wi-Fi Direct → HTTP relay**.

Los fragmentos de contenido se distribuyen a través de `IContentService`; las transmisiones en vivo utilizan `IStreamingService`. Todo funciona entre pares sin ningún servidor central.

---

## Sesiones de visionado compartido

```kotlin
// Host a watch party
val session = watchTogether.hostAsync(contentHash)

// Guests join by AetherTag
watchTogether.joinAsync(hostUhid)
```

La reproducción se sincroniza con una precisión de ±100 ms (compensando el tiempo de ida y vuelta). Las reacciones con emojis se superponen al video en tiempo real.

---

## Dependencias

| Biblioteca | Propósito |
|-----------|----------|
| `media3-exoplayer` | Reproducción de video/audio |
| `media3-session` | Sesión multimedia y reproducción en segundo plano |
| `androidx.compose.ui` | Kit de herramientas de interfaz de usuario |
| `androidx.leanback` | Navegación para TV (solo media-tv) |
| `aether-protocol-android` | Transporte de malla |

---

## Estructura del proyecto

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

## Licencia

MIT
