# Aether Media — Android

[English](../../../../android/README.md) · [Français](../../fr/android/README.md) · [Español](../../es/android/README.md) · [العربية](../../ar/android/README.md) · [中文简体](../../zh-CN/android/README.md) · [日本語](README.md) · [Deutsch](../../de/android/README.md) · [Português (BR)](../../pt-BR/android/README.md) · [Русский](../../ru/android/README.md) · [فارسی](../../fa/android/README.md) · [한국어](../../ko/android/README.md)

Jetpack Compose と media3/ExoPlayer をベースに構築された 2 つの Android アプリケーションです。スマートフォンおよび Android TV 上で Aether Media のフル機能を提供します。オフラインのメッシュ検出、ライブストリーミング、ウォッチパーティ、ソーシャルインタラクションを含むすべての機能が、インターネット接続なしで動作します。

---

## アプリケーション

| モジュール | パッケージ | ターゲット |
|--------|---------|--------|
| `media/` | `aethermedia` | スマートフォン / タブレット (Jetpack Compose) |
| `media-tv/` | `aethermedia.tv` | Android TV (リーンバック、D パッドナビゲーション) |

---

## 要件

- Android Studio Hedgehog (2023.1) 以降
- Android SDK: `compileSdk 35`、`minSdk 26`
- Kotlin `2.1.0`
- AGP `8.7.3`
- Java 17

---

## ビルド

```bash
# Phone app
cd media
./gradlew assembleDebug

# TV app
cd media-tv
./gradlew assembleDebug
```

### リリースビルド

```bash
./gradlew assembleRelease
```

リリース APK をビルドする前に、`local.properties` または環境変数で署名情報を設定してください。

---

## テストの実行

```bash
./gradlew test          # unit tests
./gradlew connectedTest # instrumented tests (device / emulator required)
```

---

## アーキテクチャ

両アプリとも Jetpack コンポーネントを基盤とした同一の MVVM パターンに従っています。

```
UI Layer       — Compose screens + ViewModels
Domain Layer   — Use-cases (shared with kotlin/ JVM module)
Data Layer     — Aether mesh transport via aether-protocol Android bindings
```

### 主要画面（スマートフォンアプリ）

| 画面 | 説明 |
|--------|-------------|
| Home | フォロー中のクリエイターのコンテンツフィード |
| Nearby | メッシュで検出されたライブストリーム（インターネット不要） |
| Library | ローカルおよびダウンロード済みメディア |
| Watch Together | 進行中のウォッチパーティセッション |
| Profile | AetherNetTag アイデンティティとクリエイターチャンネル |

### 主要画面（TV アプリ）

| 画面 | 説明 |
|--------|-------------|
| Browse | リーンバックスタイルのコンテンツブラウザー |
| Playback | D パッドコントロール付きフルスクリーン ExoPlayer |
| Nearby | カード行として表示されるメッシュピア検出 |

---

## メディアエンジン

両アプリとも再生に **media3/ExoPlayer** を使用しています。

- ローカル Aether メッシュからの HLS および DASH アダプティブストリーミング
- 字幕トラックのサポート（SRT、VTT）
- `MediaSessionService` によるバックグラウンド再生
- Android 8.0 以降でのピクチャーインピクチャー（PiP）

---

## メッシュ統合

アプリは起動時に Aether Protocol Android サービスにバインドします。

```kotlin
// Resolve nearby peers with streaming capability
aetherClient.handshake.peerNegotiated
    .filter { it.capabilities.streaming }
    .collect { peer -> nearbyFeed.add(peer) }
```

トランスポートのネゴシエーション順序: **NearLink → BLE → Wi-Fi Direct → HTTP relay**

コンテンツチャンクは `IContentService` を介して配布され、ライブストリームは `IStreamingService` を使用します。すべての処理は中央サーバーなしでピアツーピアで行われます。

---

## ウォッチパーティ

```kotlin
// Host a watch party
val session = watchTogether.hostAsync(contentHash)

// Guests join by AetherNetTag
watchTogether.joinAsync(hostUhid)
```

再生は RTT 補正により ±100 ms 以内で同期されます。絵文字リアクションがリアルタイムで動画にオーバーレイ表示されます。

---

## 依存ライブラリ

| ライブラリ | 目的 |
|---------|---------|
| `media3-exoplayer` | 動画 / 音声再生 |
| `media3-session` | メディアセッション + バックグラウンド再生 |
| `androidx.compose.ui` | UI ツールキット |
| `androidx.leanback` | TV ナビゲーション（media-tv のみ） |
| `aether-protocol-android` | メッシュトランスポート |

---

## プロジェクト構成

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

## ライセンス

MIT
