# Aether Media — Android

[English](../../../../android/README.md) · [Français](../../fr/android/README.md) · [Español](../../es/android/README.md) · [العربية](../../ar/android/README.md) · [中文简体](README.md) · [日本語](../../ja/android/README.md) · [Deutsch](../../de/android/README.md) · [Português (BR)](../../pt-BR/android/README.md) · [Русский](../../ru/android/README.md) · [فارسی](../../fa/android/README.md) · [한국어](../../ko/android/README.md)

两款基于 Jetpack Compose 和 media3/ExoPlayer 构建的 Android 应用，在手机和 Android TV 上提供完整的 Aether Media 体验——包括离线网状网络发现、直播流媒体、共同观影和社交互动——无需任何网络连接。

---

## 应用模块

| 模块 | 包名 | 目标平台 |
|--------|---------|--------|
| `media/` | `aethermedia` | 手机 / 平板（Jetpack Compose） |
| `media-tv/` | `aethermedia.tv` | Android TV（lean-back，方向键导航） |

---

## 环境要求

- Android Studio Hedgehog (2023.1) 或更高版本
- Android SDK：`compileSdk 35`，`minSdk 26`
- Kotlin `2.1.0`
- AGP `8.7.3`
- Java 17

---

## 构建

```bash
# 手机应用
cd media
./gradlew assembleDebug

# TV 应用
cd media-tv
./gradlew assembleDebug
```

### 发布构建

```bash
./gradlew assembleRelease
```

构建发布 APK 前，请在 `local.properties` 中或通过环境变量配置签名凭据。

---

## 运行测试

```bash
./gradlew test          # 单元测试
./gradlew connectedTest # 仪器测试（需要设备或模拟器）
```

---

## 架构

两款应用均采用基于 Jetpack 组件的 MVVM 模式：

```
UI Layer       — Compose 界面 + ViewModels
Domain Layer   — 用例（与 kotlin/ JVM 模块共享）
Data Layer     — 通过 aether-protocol Android 绑定实现的 Aether 网状传输
```

### 主要界面（手机应用）

| 界面 | 说明 |
|--------|-------------|
| Home | 所关注创作者的内容流 |
| Nearby | 网状网络发现的直播流（无需网络连接） |
| Library | 本地及已下载媒体 |
| Watch Together | 活跃的共同观影会话 |
| Profile | AetherNetTag 身份与创作者频道 |

### 主要界面（TV 应用）

| 界面 | 说明 |
|--------|-------------|
| Browse | Leanback 风格内容浏览器 |
| Playback | 带方向键控制的全屏 ExoPlayer |
| Nearby | 以卡片行形式展示的网状节点发现 |

---

## 媒体引擎

两款应用均使用 **media3/ExoPlayer** 进行播放：

- 来自本地 Aether 网状网络的 HLS 和 DASH 自适应流媒体
- 字幕轨道支持（SRT、VTT）
- 通过 `MediaSessionService` 实现后台播放
- Android 8.0+ 的画中画（PiP）功能

---

## 网状网络集成

应用在启动时绑定至 Aether Protocol Android 服务：

```kotlin
// Resolve nearby peers with streaming capability
aetherClient.handshake.peerNegotiated
    .filter { it.capabilities.streaming }
    .collect { peer -> nearbyFeed.add(peer) }
```

传输协商顺序：**NearLink → BLE → Wi-Fi Direct → HTTP relay**。

内容块通过 `IContentService` 分发；直播流使用 `IStreamingService`。一切均以点对点方式运行，无需中央服务器。

---

## 共同观影

```kotlin
// Host a watch party
val session = watchTogether.hostAsync(contentHash)

// Guests join by AetherNetTag
watchTogether.joinAsync(hostUhid)
```

播放同步精度在 ±100 ms 以内（经 RTT 补偿）。表情反应实时叠加在视频上。

---

## 依赖项

| 库 | 用途 |
|---------|---------|
| `media3-exoplayer` | 视频/音频播放 |
| `media3-session` | 媒体会话 + 后台播放 |
| `androidx.compose.ui` | UI 工具包 |
| `androidx.leanback` | TV 导航（仅 media-tv） |
| `aether-protocol-android` | 网状传输 |

---

## 项目结构

```
android/
├── media/                  # 手机 / 平板应用
│   ├── app/
│   │   └── src/main/
│   │       ├── kotlin/     # ViewModels, screens, Compose UI
│   │       └── res/        # Layouts, drawables, strings
│   └── build.gradle.kts
└── media-tv/               # Android TV 应用
    ├── app/
    │   └── src/main/
    │       ├── kotlin/     # Leanback fragments, presenters
    │       └── res/
    └── build.gradle.kts
```

---

## 许可证

MIT
