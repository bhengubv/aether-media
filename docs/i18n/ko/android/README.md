# Aether Media — Android

[English](../../../../android/README.md) · [Français](../../fr/android/README.md) · [Español](../../es/android/README.md) · [العربية](../../ar/android/README.md) · [中文简体](../../zh-CN/android/README.md) · [日本語](../../ja/android/README.md) · [Deutsch](../../de/android/README.md) · [Português (BR)](../../pt-BR/android/README.md) · [Русский](../../ru/android/README.md) · [فارسی](../../fa/android/README.md) · [한국어](README.md)

Jetpack Compose와 media3/ExoPlayer를 기반으로 제작된 두 가지 Android 애플리케이션으로, 스마트폰과 Android TV에서 Aether Media의 전체 기능을 제공합니다. 오프라인 메시 탐색, 라이브 스트리밍, 같이 보기, 소셜 기능 등을 인터넷 연결 없이 사용할 수 있습니다.

---

## 애플리케이션

| 모듈 | 패키지 | 대상 |
|--------|---------|--------|
| `media/` | `aether.media` | 스마트폰 / 태블릿 (Jetpack Compose) |
| `media-tv/` | `aether.media.tv` | Android TV (린백, D-패드 탐색) |

---

## 요구 사항

- Android Studio Hedgehog (2023.1) 이상
- Android SDK: `compileSdk 35`, `minSdk 26`
- Kotlin `2.1.0`
- AGP `8.7.3`
- Java 17

---

## 빌드

```bash
# Phone app
cd media
./gradlew assembleDebug

# TV app
cd media-tv
./gradlew assembleDebug
```

### 릴리스 빌드

```bash
./gradlew assembleRelease
```

릴리스 APK를 빌드하기 전에 `local.properties` 또는 환경 변수에 서명 자격 증명을 설정하세요.

---

## 테스트 실행

```bash
./gradlew test          # unit tests
./gradlew connectedTest # instrumented tests (device / emulator required)
```

---

## 아키텍처

두 앱 모두 Jetpack 컴포넌트 기반의 동일한 MVVM 패턴을 따릅니다:

```
UI Layer       — Compose screens + ViewModels
Domain Layer   — Use-cases (shared with kotlin/ JVM module)
Data Layer     — Aether mesh transport via aether-protocol Android bindings
```

### 주요 화면 (스마트폰 앱)

| 화면 | 설명 |
|--------|-------------|
| Home | 팔로우한 크리에이터의 콘텐츠 피드 |
| Nearby | 메시로 탐색된 라이브 스트림 (인터넷 불필요) |
| Library | 로컬 및 다운로드한 미디어 |
| Watch Together | 현재 진행 중인 같이 보기 세션 |
| Profile | AetherTag 신원 및 크리에이터 채널 |

### 주요 화면 (TV 앱)

| 화면 | 설명 |
|--------|-------------|
| Browse | 린백 스타일 콘텐츠 브라우저 |
| Playback | D-패드 컨트롤이 포함된 전체 화면 ExoPlayer |
| Nearby | 카드 행으로 표시되는 메시 피어 탐색 |

---

## 미디어 엔진

두 앱 모두 재생에 **media3/ExoPlayer**를 사용합니다:

- 로컬 Aether 메시에서 HLS 및 DASH 적응형 스트리밍
- 자막 트랙 지원 (SRT, VTT)
- `MediaSessionService`를 통한 백그라운드 재생
- Android 8.0 이상에서 PIP(화면 속 화면) 지원

---

## 메시 연동

앱은 시작 시 Aether Protocol Android 서비스에 바인딩합니다:

```kotlin
// Resolve nearby peers with streaming capability
aetherClient.handshake.peerNegotiated
    .filter { it.capabilities.streaming }
    .collect { peer -> nearbyFeed.add(peer) }
```

전송 협상 순서: **NearLink → BLE → Wi-Fi Direct → HTTP relay**.

콘텐츠 청크는 `IContentService`를 통해 배포되며, 라이브 스트림은 `IStreamingService`를 사용합니다. 모든 동작은 중앙 서버 없이 피어 투 피어로 이루어집니다.

---

## 같이 보기

```kotlin
// Host a watch party
val session = watchTogether.hostAsync(contentHash)

// Guests join by AetherTag
watchTogether.joinAsync(hostUhid)
```

재생은 ±100 ms 이내로 동기화됩니다 (RTT 보상). 이모지 반응이 실시간으로 영상 위에 오버레이됩니다.

---

## 의존성

| 라이브러리 | 목적 |
|---------|---------|
| `media3-exoplayer` | 비디오/오디오 재생 |
| `media3-session` | 미디어 세션 + 백그라운드 재생 |
| `androidx.compose.ui` | UI 툴킷 |
| `androidx.leanback` | TV 탐색 (media-tv 전용) |
| `aether-protocol-android` | 메시 전송 |

---

## 프로젝트 구조

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

## 라이선스

MIT
