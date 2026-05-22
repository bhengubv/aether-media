# Aether Media — Android

[English](../../../../android/README.md) · [Français](../../fr/android/README.md) · [Español](../../es/android/README.md) · [العربية](../../ar/android/README.md) · [中文简体](../../zh-CN/android/README.md) · [日本語](../../ja/android/README.md) · [Deutsch](../../de/android/README.md) · [Português (BR)](../../pt-BR/android/README.md) · [Русский](README.md) · [فارسی](../../fa/android/README.md) · [한국어](../../ko/android/README.md)

Два Android-приложения, созданных на основе Jetpack Compose и media3/ExoPlayer, предоставляющих полный функционал Aether Media на телефонах и Android TV — включая офлайн-обнаружение через меш-сеть, прямые трансляции, совместный просмотр и социальные взаимодействия — без подключения к интернету.

---

## Приложения

| Модуль | Пакет | Целевое устройство |
|--------|-------|--------------------|
| `media/` | `aether.media` | Телефон / планшет (Jetpack Compose) |
| `media-tv/` | `aether.media.tv` | Android TV (lean-back, навигация с D-pad) |

---

## Требования

- Android Studio Hedgehog (2023.1) или новее
- Android SDK: `compileSdk 35`, `minSdk 26`
- Kotlin `2.1.0`
- AGP `8.7.3`
- Java 17

---

## Сборка

```bash
# Phone app
cd media
./gradlew assembleDebug

# TV app
cd media-tv
./gradlew assembleDebug
```

### Релизная сборка

```bash
./gradlew assembleRelease
```

Укажите данные для подписи в `local.properties` или через переменные окружения перед сборкой релизного APK.

---

## Запуск тестов

```bash
./gradlew test          # unit tests
./gradlew connectedTest # instrumented tests (device / emulator required)
```

---

## Архитектура

Оба приложения следуют одному шаблону MVVM, построенному на компонентах Jetpack:

```
UI Layer       — Compose screens + ViewModels
Domain Layer   — Use-cases (shared with kotlin/ JVM module)
Data Layer     — Aether mesh transport via aether-protocol Android bindings
```

### Ключевые экраны (приложение для телефона)

| Экран | Описание |
|-------|----------|
| Home | Лента контента от отслеживаемых авторов |
| Nearby | Прямые трансляции, обнаруженные через меш-сеть (без интернета) |
| Library | Локальные и загруженные медиафайлы |
| Watch Together | Активные сессии совместного просмотра |
| Profile | Идентификатор AetherTag и канал автора |

### Ключевые экраны (приложение для TV)

| Экран | Описание |
|-------|----------|
| Browse | Браузер контента в стиле Leanback |
| Playback | Полноэкранный ExoPlayer с управлением через D-pad |
| Nearby | Обнаружение меш-пиров в виде строки карточек |

---

## Медиадвижок

Оба приложения используют **media3/ExoPlayer** для воспроизведения:

- Адаптивное потоковое воспроизведение HLS и DASH из локальной меш-сети Aether
- Поддержка субтитров (SRT, VTT)
- Фоновое воспроизведение через `MediaSessionService`
- Картинка-в-картинке (PiP) на Android 8.0+

---

## Интеграция с меш-сетью

Приложения подключаются к Android-сервису протокола Aether при запуске:

```kotlin
// Resolve nearby peers with streaming capability
aetherClient.handshake.peerNegotiated
    .filter { it.capabilities.streaming }
    .collect { peer -> nearbyFeed.add(peer) }
```

Порядок согласования транспорта: **NearLink → BLE → Wi-Fi Direct → HTTP relay**.

Блоки контента распределяются через `IContentService`; прямые трансляции используют `IStreamingService`. Всё работает в режиме «пир-к-пиру» без центрального сервера.

---

## Совместный просмотр

```kotlin
// Host a watch party
val session = watchTogether.hostAsync(contentHash)

// Guests join by AetherTag
watchTogether.joinAsync(hostUhid)
```

Воспроизведение синхронизируется с точностью ±100 мс (с компенсацией RTT). Эмодзи-реакции накладываются на видео в режиме реального времени.

---

## Зависимости

| Библиотека | Назначение |
|------------|-----------|
| `media3-exoplayer` | Воспроизведение видео/аудио |
| `media3-session` | Медиасессия + фоновое воспроизведение |
| `androidx.compose.ui` | UI-инструментарий |
| `androidx.leanback` | Навигация на TV (только media-tv) |
| `aether-protocol-android` | Меш-транспорт |

---

## Структура проекта

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

## Лицензия

MIT
