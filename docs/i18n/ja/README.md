```
  ╔═╗ ╔═╗ ╔╦╗ ╦ ╦ ╔═╗ ╦═╗   ╔╦╗ ╔═╗ ╔╦╗ ╦ ╔═╗
  ╠═╣ ║╣   ║  ╠═╣ ║╣  ╠╦╝   ║║║ ║╣   ║║ ║ ╠═╣
  ╩ ╩ ╚═╝  ╩  ╩ ╩ ╚═╝ ╩╚═   ╩ ╩ ╚═╝ ═╩╝ ╩ ╩ ╩
  decentralised social media · no internet required
```

[![MIT License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![npm version](https://img.shields.io/npm/v/@bhengubv/aether-media.svg)](https://www.npmjs.com/package/@bhengubv/aether-media)
[![Build](https://github.com/bhengubv/aether-media/actions/workflows/ci.yml/badge.svg)](https://github.com/bhengubv/aether-media/actions/workflows/ci.yml)

> Aetherメッシュプロトコル上に構築された分散型ソーシャルメディアネットワークとプレイヤー。
> インターネット不要。中央サーバー不要。企業オーナー不要。

同じ部屋にある2台のスマートフォン — Wi-FiもモバイルデータもなしでBLE、Wi-Fi Direct、NearLink、LoRa、またはHTTPリレー経由で互いを発見し、メディアを共有し、ライブビデオをストリーミングし、ソーシャルに反応できます。

---

## なぜAether Media？

**インターネットなし、CDNなし、ストリーミング料金なしでライブコンサートを観客のスマートフォンにストリーミングする。**

パフォーマーのデバイスがWi-Fi Directでブロードキャストします。範囲内のすべてのスマートフォンがストリームを受信し、BLE経由でさらに中継します。リアクション（いいね、スーパーリアクト、正確な再生位置でのコメント）も同じ経路で返ってきます。10,000 km離れたデータセンターからのバッファリングはありません。観客にアカウントは必要ありません。

```
  [Performer] ──WiFi Direct──▶ [Row 1] ──BLE──▶ [Row 2] ──NearLink──▶ [Row 3]
                 1080p live           relayed, encrypted        relayed, encrypted
```

**オフラインのクリエイターをフォローする。範囲内に戻ってきたときにコンテンツを受け取る。**

フォローはAetherのDTN（遅延耐性ネットワーキング）ストアアンドフォワード層で配信されます。クリエイターのデバイスが現在到達可能でない場合、フォローのインテントは最大72時間待機し、ルートが開いた瞬間に配信されます。プッシュ通知インフラも、アプリサーバーも不要です。

**メッシュを越えて一緒に映画を観る。**

メッシュ上の誰かがファイルを持っています。ウォッチパーティのコーディネーターがRTT補正で全デバイスの再生、一時停止、シークを同期します。リアクションはビデオ内の正確なタイムスタンプでリアルタイムに発火します。ホストのデバイスが映画の途中でオフラインになった場合、セッションは自動的に次の利用可能なピアに移行します。

---

### 既存のプレイヤーおよびネットワークとの比較

| 機能 | VLC | Spotify | Streambert | Aether Media |
|---|:---:|:---:|:---:|:---:|
| オフライン動作（インターネット不要） | 再生のみ | いいえ | いいえ | **はい — 発見、ストリーム、リアクション** |
| アプリアカウントなしで動作 | はい | いいえ | いいえ | **はい** |
| メッシュリレー（デバイスをホップ） | いいえ | いいえ | いいえ | **はい** |
| CDNなしのライブストリーミング | いいえ | いいえ | 一部 | **はい — BLE / Wi-Fi Direct / NearLink** |
| タイムラインのソーシャルリアクション | いいえ | いいえ | いいえ | **はい — 正確な再生位置で** |
| サーバーなしのフォローグラフ | いいえ | いいえ | いいえ | **はい — DTN配信** |
| 同じ部屋でのサブ秒レイテンシ | N/A | いいえ | いいえ | **はい — NearLink 20 µs** |
| マイクロコントローラ / C11で動作 | いいえ | いいえ | いいえ | **はい — C実装** |
| 8言語SDK | いいえ | いいえ | いいえ | **はい** |

---

## 仕組み

**ステップ1 — メッシュ発見。** デバイスはBLEアドバタイズメントとWi-Fi Directプローブ応答でAether Tag（`@sam.5jk2` のような短いアイデンティティハンドル）をブロードキャストします。IPアドレスもアカウントも不要です。トランスポート層はNearLink（600 m、12 Mbps、20 µs）を持つデバイスでは自動的に昇格し、Wi-Fi Direct（200 m、250 Mbps）、BLE（100 m、1 Mbps）、LoRa-over-BLE（1.3 km）、そして最終手段としてHTTPリレーにフォールバックします。

**ステップ2 — コンテンツアドレッシング。** すべてのメディアはURLやサーバーパスではなく、SHA-256コンテンツハッシュによって識別されます。`ContentDescriptor`（ハッシュ + 名前 + MIMEタイプ + チャンクマニフェスト）がメッシュでブロードキャストされます。ファイルを持つすべてのデバイスが必要とするデバイスにチャンクを提供できます。オリジンサーバーはありません。ファイルはBitTorrentスタイルで複数のピアが同時に持つフラグメントから組み立てられます。

**ステップ3 — ソーシャル層。** フォロー、リアクション、プロフィール更新は署名されたJSONペイロードとしてエンコードされ、DTNバンドル（オフライン耐性配信向け）またはベストエフォートの `MeshPacket`（ライブストリーム中の低レイテンシリアクション向け）として送信されます。`SocialGraph` はフォローしているユーザーを追跡します。`FeedAggregator` はフォロー中のクリエイターからの `StreamAnnounce` と `ContentAnnounce` パケットを待機し、フィードサーバーなしで純粋にメッシュイベントから時系列のフィードを組み立てます。

---

## 使用されているAether機能

Aether Mediaは [aether-protocol](https://github.com/bhengubv/aether-protocol) の上に構築され、以下のインターフェースを使用します:

| Aetherインターフェース | パッケージ | Aether Mediaでの使用方法 |
|---|---|---|
| `ITransportService` | `Aether.Transport` | エンコードされたビデオ/オーディオフレーム、リアクション、フォローインテントをメッシュ（BLE / Wi-Fi Direct / NearLink / LoRa / HTTPリレー）で送信 |
| `IStreamingService` | `Aether.Streaming` | ライブ開始時に `StreamAnnounce` をブロードキャスト; `FeedAggregator` はライブストリームフィードを維持するために `StreamAnnounced` と `StreamEnded` イベントをサブスクライブ |
| `IContentService` | `Aether.Content` | アップロードされたメディアの `ContentDescriptor` を公開; `FeedAggregator` はVOD発見のために `ContentAnnounced` をサブスクライブ |
| `IDtnService` | `Aether.Dtn` | オフラインのクリエイターにフォローインテントを確実に配信; バンドルはルートが開くまで最大72時間待機 |
| `IMeshSender` | `Aether.Messaging` | DTNオーバーヘッドなしでメッシュ経由のベストエフォートアンフォローパケットとライブリアクションを送信 |
| `IRoutingService` | `Aether.Routing` | ソーシャルパケットのルート対応配信; Ed25519署名済みルート応答を持つAODVスタイルのRREQ/RREP |
| `SignalProtocolService` | `Aether.Security` | X3DH + Double Ratchetでダイレクトメッセージ、プロフィール同期ペイロード、プライベートチャンネルコンテンツをエンドツーエンド暗号化 |
| `IAdaptiveBitrateController` | `Aether.Streaming` | アクティブトランスポートからのライブ帯域幅推定に基づいて最高持続可能な品質ランク（H.264 / H.265 / VP8）を選択 |

---

## 8言語SDK

Aether Mediaはエコシステム内のすべてのプラットフォームで動作するように8言語で実装されています。

| 言語 | ディレクトリ | プラットフォーム | メディアエンジン | 役割 |
|---|---|---|---|---|
| C# (.NET 10) | `src/` | Windows · Linux · macOS | LibVLC (LibVLCSharp) | リファレンス実装、完全DI、NuGetパッケージ |
| TypeScript | `typescript/` | ブラウザ · Node 20 | HLS.js · Shaka Player | Webプレイヤー、フィードクライアント、ソーシャルSDK |
| Python | `python/` | Python 3.11+以降 | mutagen（メタデータ） | プラグインエンジン、スクリプティング、メタデータ処理 |
| Rust | `rust/` | あらゆるRustターゲット | `rodio`（オーディオ） | 高性能フィードエンジン、ベンチマーク |
| Go | `go/` | あらゆるGo 1.22ターゲット | — | ソーシャルグラフライブラリ |
| Kotlin | `kotlin/` | JVM 21 · Android | media3 / ExoPlayer (Android) | Androidプレイヤー; サーバーサイド用途のJVMソーシャルグラフ |
| Swift | `swift/` | macOS 13+ · iOS 16+ | AVFoundation | Appleプラットフォームプレイヤー |
| C | `c/` | あらゆるC11ターゲット | — | 組み込み / マイクロコントローラのフィードとソーシャルモデル |

全8実装は `aether-protocol` と同じワイヤーフォーマットを共有し、CIのクロス言語フィクスチャで検証された相互運用可能なソーシャルパケットを生成します。

---

## クイックスタート

### C#デスクトップ（Windows / Linux / macOS）

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media
dotnet run --project samples/Aether.Media.Demo.Console
```

全サブシステムを登録:

```csharp
services.AddAetherMedia(media =>
    media.AddIdentity()
         .AddContent()
         .AddSocial()
         .AddStreaming()
         .AddAI());
```

解決して使用:

```csharp
var library = provider.GetRequiredService<IMediaLibrary>();
var graph   = provider.GetRequiredService<ISocialGraph>();
var feed    = provider.GetRequiredService<IFeedAggregator>();

var content = new MediaContent(
    ContentHash:  "a3f9...",
    Title:        "Sample Video",
    DurationMs:   125_000,
    Codec:        "H.264",
    ContentType:  "video/mp4",
    CreatorUhid:  "KXJB7-MN2P4",
    SizeBytes:    15_728_640,
    CreatedAt:    DateTime.UtcNow,
    ThumbnailHash: null,
    Tags:         ["demo", "sample"]);

await library.AddAsync(content);
await graph.FollowAsync("KXJB7-MN2P4");
await feed.StartAsync();
```

### TypeScript（ブラウザ）

```typescript
import { AetherMediaPlayer } from '@bhengubv/aether-media';

const video  = document.querySelector('video') as HTMLVideoElement;
const player = new AetherMediaPlayer(video);

// メッシュ上のピアが公開したHLSストリームを読み込む
await player.load('aether://stream/KXJB7-MN2P4');
await player.play();

// 生のメッシュセグメントを直接MSEパイプラインに送り込む
player.feedSegment(encodedBytes, 'video/mp4; codecs="avc1.42E01E"');
```

ローカルストレージキャッシュを持つフィードクライアント:

```typescript
import { FeedClient } from '@bhengubv/aether-media';

const client = new FeedClient('https://relay.aether.network/media');
const items  = await client.getFeed(20, 0);   // limit, offset

for (const item of items) {
    console.log(item.content.title, item.likeCount);
}

await client.markWatched('a3f9...', 45_000);  // contentHash, 視聴時間(ms)
```

### Python（プラグイン）

```python
from aether_media.plugins.base import AetherMediaPlugin
from aether_media.models import MediaContent, MediaReaction

class MyPlugin(AetherMediaPlugin):
    @property
    def name(self) -> str:
        return "My Plugin"

    @property
    def version(self) -> str:
        return "1.0.0"

    def on_content_loaded(self, content: MediaContent) -> None:
        print(f"Loaded: {content.title}  ({content.formatted_duration})")

    def on_reaction_received(self, reaction: MediaReaction) -> None:
        print(f"Reaction: {reaction.type.name} at {reaction.position_ms} ms")
```

### Kotlin（Android / JVM）

```kotlin
import aether.media.social.SocialGraph

val graph = SocialGraph()
graph.follow("KXJB7-MN2P4")
println(graph.isFollowing("KXJB7-MN2P4"))  // true
println(graph.getFollowing())               // ["KXJB7-MN2P4"]
graph.unfollow("KXJB7-MN2P4")
println(graph.count)                        // 0
```

### Rust

```rust
use aether_media::feed::{FeedStore, FeedEntry};

let mut store = FeedStore::new(500);
let entry = FeedEntry {
    content_hash: "a3f9ee...".to_string(),
    title:        "Sample".to_string(),
    creator_uhid: "KXJB7-MN2P4".to_string(),
    duration_ms:  125_000,
    like_count:   0,
    is_live:      false,
};
store.add(entry);
println!("Feed has {} item(s)", store.len());
```

### Go

```go
import "github.com/bhengubv/aether-media/go/social"

g := social.NewSocialGraph()
g.Follow("KXJB7-MN2P4")
fmt.Println(g.IsFollowing("KXJB7-MN2P4")) // true
fmt.Println(g.Following())                 // [KXJB7-MN2P4]
```

### Swift

```swift
import AetherMedia

let graph = SocialGraph()
try await graph.follow(uhid: "KXJB7-MN2P4")
let following = try await graph.following()
print(following) // ["KXJB7-MN2P4"]
```

### C

```c
#include "aether_media/social.h"

aether_social_graph_t *graph = aether_social_graph_create();
aether_social_graph_follow(graph, "KXJB7-MN2P4");
printf("Following: %d\n", aether_social_graph_is_following(graph, "KXJB7-MN2P4")); // 1
aether_social_graph_destroy(graph);
```

---

## ソーシャルプロトコル

ソーシャル層にサーバーはありません。すべてのフォロー、アンフォロー、コンテンツアナウンス、リアクションは署名された `MeshPacket` または `DtnBundle` であり、利用可能なラジオ経由で転送されます。

**フォロー**は `FollowIntentPayload`（UTF-8 JSON）にラップされ、ターゲットクリエイターのSignal Protocolセッションキー（X3DH + Double Ratchet）で暗号化され、ターゲットUHID宛の `DtnBundle` としてコミットされます。DTN層はバンドルをローカルに保存し、ターゲットへのパスが開いたときにメッシュ経由で配信します — 数時間かかっても。クリエイターのデバイスがバンドルを受信し、署名を検証し、フォロワー数をインクリメントします。これらのすべては中央サーバーが関係を知ることなく行われます。

**コンテンツアナウンス**は公開デバイスがブロードキャストする `ContentDescriptor` パケットです。ディスクリプターを受信したすべてのデバイスはそれをキャッシュし、近くのピアに再ブロードキャストします（重複排除付きメッシュフラッド）。各デバイスの `FeedAggregator` はこれらのブロードキャストを待機し、フォロー中のクリエイターからの新しいコンテンツをローカルフィードに表示します。

**リアクション**（いいね、シェア、スーパーリアクト、コメント）はコンテンツハッシュ、リアクションタイプ、ミリ秒単位の正確な再生位置を含みます。クリエイターのUHID宛てのベストエフォートの `MeshPacket` として転送されます — Ed25519署名済みルート応答を持つAODVでルーティングされるため、偽の宛先がインターセプトできません。ライブストリーム中、リアクションはメッシュを離れることなく、パブリッシャーのデバイスでリアルタイムに集計・表示されます。

**プロフィール同期**はフォローと同じDTNメカニズムを使用します。クリエイターが表示名、アバター、バイオを更新すると、新しい `MediaProfile` がEd25519アイデンティティキーで署名され、シリアライズされ、DTNバンドルとしてブロードキャストされます。それを受信したすべてのデバイス — 直接または中継経由 — は署名を検証してローカルキャッシュを更新します。オフライン中に行われたプロフィール更新は、フォロワーのいずれかがラジオ範囲内に入ったときにフォロワーに届きます。

---

## リポジトリ構成

```
aether-media/
  src/
    Aether.Media.Core/            ドメインモデルとインターフェース (MediaContent, IMediaLibrary等)
    Aether.Media.Identity/        プロフィール管理、アバター、プロフィール同期
    Aether.Media.Content/         メディアライブラリスキャナー、メタデータリゾルバー、LRUキャッシュ、サムネイル
    Aether.Media.Social/          SocialGraph、FeedAggregator、ReactionService、DiscoveryService
    Aether.Media.Streaming/       LiveStreamPublisher、WatchPartyCoordinator、AbrController
    Aether.Media.AI/              ContentRanker、ContentModerator、CreatorReputationView
    Aether.Media.DependencyInjection/  AddAetherMedia()拡張 + AetherMediaBuilderフルエントAPI
    Aether.Media.Desktop/         Windows / Linux / macOS用LibVLCSharp統合
  samples/
    Aether.Media.Demo.Console/    全サブシステムを示すインタラクティブコンソールデモ
    Aether.Media.RelayTest/       HTTPリレーラウンドトリップテスト（Aether.RelayServerが必要）
  tests/
    Aether.Media.Core.Tests/      ドメインモデルとInMemoryMediaLibraryのユニットテスト
    Aether.Media.Social.Tests/    SocialGraphとFeedAggregatorのユニットテスト
  typescript/                     TypeScript Webプレイヤーとソーシャルアプリ (@bhengubv/aether-media)
    src/
      player/   AetherMediaPlayer (HLS.js + Shaka Player + ネイティブMSE)
      social/   FeedClient、ReactionClient
      identity/ ProfileClient
      streaming/ AetherStreamClient
      models/   C#ドメインモデルのTypeScriptミラー
  python/                         Pythonプラグインエンジンとメタデータライブラリ（PyPI上のaether-media）
    aether_media/
      plugins/  AetherMediaPluginベースクラス、PluginHost
      metadata/ タグリーダー/ライター（mutagenラッパー）
      cli/      コマンドラインエントリーポイント
  rust/                           Rustフィードエンジン（crates.io上のaether-media）
    src/
      feed/     FeedStore、FeedEntry
      social/   SocialGraph、フォロー/アンフォロー
      streaming/ StreamAnnounce、セグメントモデル
      player/   rodio経由のオーディオ再生
  go/                             Goソーシャルグラフライブラリ（github.com/bhengubv/aether-media/go）
    social/     SocialGraph
    player/     プレイヤーモデル
    feed/       フィードモデル
    streaming/  ストリームモデル
  kotlin/                         Kotlin/JVMソーシャルグラフ + Android ExoPlayer統合
    src/main/kotlin/
      social/   SocialGraph (ConcurrentHashMapバックエンド、JVMとAndroid)
      feed/     フィードモデル
      player/   ExoPlayer統合（Android; JVMテストはコアのみ使用）
      content/  コンテンツディスクリプターモデル
      streaming/ ストリームセッションモデル
    android/    media3-exoplayer依存付きGradle Androidモジュール
  swift/                          Swift / Appleプラットフォームプレイヤー（SwiftPMパッケージ）
    Sources/AetherMedia/
      social/   SocialGraph（アクターベース、Swift Concurrency）
      player/   AVFoundationプレイヤー
      feed/     フィードモデル
      streaming/ ストリームモデル
  c/                              組み込みターゲット向けC11フィードとソーシャルモデル
    include/aether_media/         公開ヘッダー
    src/                          実装
    tests/                        CTestベースのテストスイート
  android/                        Android Gradleモジュール
    media/      メインメディアモジュール（Kotlin + Jetpack）
    media-tv/   Android TVバリアント
  docs/                           アーキテクチャノートと設計上の決定
```

---

## ビルド

### C#

```bash
dotnet build AetherMedia.slnx
dotnet test
```

### TypeScript

```bash
cd typescript && npm install && npm run build && npm test
```

### Python

```bash
cd python && pip install -e ".[dev]" && pytest
```

### Rust

```bash
cd rust && cargo build && cargo test
```

### Go

```bash
cd go && go build ./... && go test ./...
```

### Kotlin

```bash
cd kotlin && ./gradlew build test
```

### Swift

```bash
cd swift && swift build && swift test
```

### C

```bash
cd c && cmake -B build && cmake --build build && ctest --test-dir build
```

### Android

```bash
cd android/media && ./gradlew assembleDebug
```

---

## ライセンス

MIT — 永遠に無料。コーデックエンジン（LibVLC / FFmpeg / AVFoundation / media3 / ExoPlayer）はそれぞれLGPLとApache 2.0でそのまま使用しています。[LICENSE](LICENSE) を参照してください。
