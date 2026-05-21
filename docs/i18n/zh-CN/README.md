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

> 基于 Aether 网格协议构建的去中心化社交媒体网络与播放器。
> 无需互联网。无中央服务器。无企业所有者。

同一房间内的两部手机——无 Wi-Fi，无移动数据——可以通过 BLE、Wi-Fi Direct、NearLink、LoRa 或 HTTP 中继相互发现、共享媒体、直播视频并进行社交互动。

---

## 为何选择 Aether Media？

**无需互联网、无需 CDN、无需流媒体费用，即可向观众手机直播演出。**

表演者设备通过 Wi-Fi Direct 广播。范围内的每部手机接收流并通过 BLE 进一步中继。反应（点赞、超级反应、精确到播放位置的评论）以相同方式返回。不再有来自 10,000 公里外数据中心的缓冲。观众无需账号。

```
  [Performer] ──WiFi Direct──▶ [Row 1] ──BLE──▶ [Row 2] ──NearLink──▶ [Row 3]
                 1080p live           relayed, encrypted        relayed, encrypted
```

**关注一位离线的创作者。当他们再次进入范围时接收其内容。**

关注通过 Aether 的 DTN（延迟容忍网络）存储转发层投递。如果创作者的设备当前不可达，关注意图最多等待 72 小时，并在路由开通的瞬间完成投递。无需推送通知基础设施，无需应用服务器。

**跨网格一起看电影。**

网格上的某人有文件。观影协调者在每台设备上同步播放、暂停和跳转，并进行 RTT 补偿。反应在视频的精确时间戳处实时触发。如果主机设备在影片播放中途离线，会话会自动迁移至下一个可用的节点。

---

### 与现有播放器和网络的对比

| 功能 | VLC | Spotify | Streambert | Aether Media |
|---|:---:|:---:|:---:|:---:|
| 离线工作（无需互联网） | 仅播放 | 否 | 否 | **是——发现、流媒体、互动** |
| 无需应用账号 | 是 | 否 | 否 | **是** |
| 网格中继（通过设备跳转） | 否 | 否 | 否 | **是** |
| 直播流媒体，无需 CDN | 否 | 否 | 部分 | **是——BLE / Wi-Fi Direct / NearLink** |
| 时间线上的社交反应 | 否 | 否 | 否 | **是——精确到播放位置** |
| 无服务器的关注图谱 | 否 | 否 | 否 | **是——DTN 投递** |
| 同室内亚秒级延迟 | N/A | 否 | 否 | **是——NearLink 20 µs** |
| 运行于微控制器 / C11 | 否 | 否 | 否 | **是——C 实现** |
| 8 语言 SDK | 否 | 否 | 否 | **是** |

---

## 工作原理

**步骤 1 — 网格发现。** 设备通过 BLE 广告和 Wi-Fi Direct 探测响应广播其 Aether Tag（类似 `@sam.5jk2` 的短身份标识）。无需 IP 地址或账号。传输层在具备条件的设备上自动优先使用 NearLink（600 m、12 Mbps、20 µs），回退至 Wi-Fi Direct（200 m、250 Mbps），再回退至 BLE（100 m、1 Mbps），然后是 LoRa-over-BLE（1.3 km），最后将 HTTP 中继作为最后手段。

**步骤 2 — 内容寻址。** 每段媒体都通过其 SHA-256 内容哈希来标识——而非 URL 或服务器路径。`ContentDescriptor`（哈希 + 名称 + MIME 类型 + 分块清单）在网格上广播。任何拥有该文件的设备都可以向需要它的设备提供分块。没有源服务器。文件可以类似 BitTorrent 的方式从多个节点同时持有的片段中组装。

**步骤 3 — 社交层。** 关注、反应和个人资料更新被编码为签名的 JSON 负载，以 DTN 包（用于容忍离线投递）或尽力而为的 `MeshPacket`（用于直播流期间的低延迟反应）发送。`SocialGraph` 跟踪你关注的人。`FeedAggregator` 监听来自所关注创作者的 `StreamAnnounce` 和 `ContentAnnounce` 数据包，组装按时间排序的动态——完全来自网格事件，无需动态服务器。

---

## 使用了哪些 Aether 功能

Aether Media 基于 [aether-protocol](https://github.com/bhengubv/aether-protocol) 构建，并使用以下接口：

| Aether 接口 | 包 | Aether Media 的使用方式 |
|---|---|---|
| `ITransportService` | `Aether.Transport` | 通过网格（BLE / Wi-Fi Direct / NearLink / LoRa / HTTP 中继）发送编码的视频/音频帧、反应和关注意图 |
| `IStreamingService` | `Aether.Streaming` | 开始直播时广播 `StreamAnnounce`；`FeedAggregator` 订阅 `StreamAnnounced` 和 `StreamEnded` 事件以维护直播动态 |
| `IContentService` | `Aether.Content` | 为上传的媒体发布 `ContentDescriptor`；`FeedAggregator` 订阅 `ContentAnnounced` 进行点播发现 |
| `IDtnService` | `Aether.Dtn` | 将关注意图持久投递至离线创作者；包最多等待 72 小时寻找路由 |
| `IMeshSender` | `Aether.Messaging` | 通过网格发送尽力而为的取消关注数据包和直播反应，无需 DTN 开销 |
| `IRoutingService` | `Aether.Routing` | 社交数据包的路由感知投递；带 Ed25519 签名路由回复的 AODV 风格 RREQ/RREP |
| `SignalProtocolService` | `Aether.Security` | 使用 X3DH + 双棘轮端到端加密私信、个人资料同步负载和私有频道内容 |
| `IAdaptiveBitrateController` | `Aether.Streaming` | 根据活跃传输的实时带宽估算选择最高可持续质量档次（H.264 / H.265 / VP8） |

---

## 8 语言 SDK

Aether Media 提供 8 种语言的实现，可在生态系统中的每个平台上运行。

| 语言 | 目录 | 平台 | 媒体引擎 | 角色 |
|---|---|---|---|---|
| C# (.NET 10) | `src/` | Windows · Linux · macOS | LibVLC（LibVLCSharp） | 参考实现，完整 DI，NuGet 包 |
| TypeScript | `typescript/` | 浏览器 · Node 20 | HLS.js · Shaka Player | Web 播放器，动态客户端，社交 SDK |
| Python | `python/` | 任意 Python 3.11+ | mutagen（元数据） | 插件引擎，脚本，元数据处理 |
| Rust | `rust/` | 任意 Rust 目标 | `rodio`（音频） | 高性能动态引擎，基准测试 |
| Go | `go/` | 任意 Go 1.22 目标 | — | 社交图谱库 |
| Kotlin | `kotlin/` | JVM 21 · Android | media3 / ExoPlayer（Android） | Android 播放器；JVM 社交图谱（服务端使用） |
| Swift | `swift/` | macOS 13+ · iOS 16+ | AVFoundation | Apple 平台播放器 |
| C | `c/` | 任意 C11 目标 | — | 嵌入式 / 微控制器动态和社交模型 |

全部 8 种实现共享与 `aether-protocol` 相同的线路格式，并通过 CI 中的跨语言测试用例生成可互操作的社交数据包。

---

## 快速入门

### C# 桌面（Windows / Linux / macOS）

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media
dotnet run --project samples/Aether.Media.Demo.Console
```

注册所有子系统：

```csharp
services.AddAetherMedia(media =>
    media.AddIdentity()
         .AddContent()
         .AddSocial()
         .AddStreaming()
         .AddAI());
```

解析并使用：

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

### TypeScript（浏览器）

```typescript
import { AetherMediaPlayer } from '@bhengubv/aether-media';

const video  = document.querySelector('video') as HTMLVideoElement;
const player = new AetherMediaPlayer(video);

// Load an HLS stream published by a peer on the mesh
await player.load('aether://stream/KXJB7-MN2P4');
await player.play();

// Feed raw mesh segments directly into the MSE pipeline
player.feedSegment(encodedBytes, 'video/mp4; codecs="avc1.42E01E"');
```

带本地存储缓存的动态客户端：

```typescript
import { FeedClient } from '@bhengubv/aether-media';

const client = new FeedClient('https://relay.aether.network/media');
const items  = await client.getFeed(20, 0);   // limit, offset

for (const item of items) {
    console.log(item.content.title, item.likeCount);
}

await client.markWatched('a3f9...', 45_000);  // contentHash, ms watched
```

### Python（插件）

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

## 社交协议

社交层没有服务器。每一次关注、取消关注、内容公告和反应都是一个签名的 `MeshPacket` 或 `DtnBundle`，通过任何可用的无线电传输。

**关注**被封装在 `FollowIntentPayload`（UTF-8 JSON）中，使用目标创作者的 Signal Protocol 会话密钥（X3DH + 双棘轮）加密，并作为寻址至目标 UHID 的 `DtnBundle` 提交。DTN 层在本地存储该包，并在路径向目标开通时通过网格投递——即使需要数小时。创作者的设备接收该包，验证签名，并增加其粉丝数。这一切都在没有任何中央服务器知晓该关系的情况下发生。

**内容公告**是由发布设备广播的 `ContentDescriptor` 数据包。每台收到描述符的设备都会缓存它，并将其重新广播给附近的节点（带去重的网格洪泛）。每台设备上的 `FeedAggregator` 监听这些广播，并在本地动态中呈现所关注创作者的新内容。

**反应**（点赞、分享、超级反应、评论）携带内容哈希、反应类型和精确到毫秒的播放位置。它们以尽力而为的 `MeshPacket` 形式寻址至创作者的 UHID——由带签名路由回复的 AODV 路由，因此没有虚假目的地可以拦截它们。在直播期间，反应在发布者设备上实时聚合和显示，无需离开网格。

**个人资料同步**使用与关注相同的 DTN 机制。当创作者更新其显示名称、头像或简介时，新的 `MediaProfile` 用其 Ed25519 身份密钥签名、序列化，并作为 DTN 包广播。任何收到该包的设备——直接或通过中继——都会验证签名并更新其本地缓存。离线时进行的个人资料更新，在其粉丝中的任何一位进入无线电范围时即可到达。

---

## 仓库结构

```
aether-media/
  src/
    Aether.Media.Core/            Domain models and interfaces (MediaContent, IMediaLibrary, etc.)
    Aether.Media.Identity/        Profile management, avatar, profile sync
    Aether.Media.Content/         Media library scanner, metadata resolver, LRU cache, thumbnails
    Aether.Media.Social/          SocialGraph, FeedAggregator, ReactionService, DiscoveryService
    Aether.Media.Streaming/       LiveStreamPublisher, WatchPartyCoordinator, AbrController
    Aether.Media.AI/              ContentRanker, ContentModerator, CreatorReputationView
    Aether.Media.DependencyInjection/  AddAetherMedia() extension + AetherMediaBuilder fluent API
    Aether.Media.Desktop/         LibVLCSharp integration for Windows / Linux / macOS
  samples/
    Aether.Media.Demo.Console/    Interactive console demo showing all subsystems
    Aether.Media.RelayTest/       HTTP relay round-trip test (requires Aether.RelayServer)
  tests/
    Aether.Media.Core.Tests/      Unit tests for domain models and InMemoryMediaLibrary
    Aether.Media.Social.Tests/    Unit tests for SocialGraph and FeedAggregator
  typescript/                     TypeScript web player and social SDK (@bhengubv/aether-media)
    src/
      player/   AetherMediaPlayer (HLS.js + Shaka Player + native MSE)
      social/   FeedClient, ReactionClient
      identity/ ProfileClient
      streaming/ AetherStreamClient
      models/   TypeScript mirrors of the C# domain models
  python/                         Python plugin engine and metadata library (aether-media on PyPI)
    aether_media/
      plugins/  AetherMediaPlugin base class, PluginHost
      metadata/ Tag reader/writer (mutagen wrapper)
      cli/      Command-line entry points
  rust/                           Rust feed engine (aether-media on crates.io)
    src/
      feed/     FeedStore, FeedEntry
      social/   SocialGraph, follow/unfollow
      streaming/ StreamAnnounce, segment models
      player/   Audio playback via rodio
  go/                             Go social graph library (github.com/bhengubv/aether-media/go)
    social/     SocialGraph
    player/     Player models
    feed/       Feed models
    streaming/  Stream models
  kotlin/                         Kotlin/JVM social graph + Android ExoPlayer integration
    src/main/kotlin/
      social/   SocialGraph (ConcurrentHashMap-backed, JVM and Android)
      feed/     Feed models
      player/   ExoPlayer integration (Android; JVM tests use core only)
      content/  Content descriptor models
      streaming/ Stream session models
    android/    Gradle Android module with media3-exoplayer dependency
  swift/                          Swift / Apple platform player (SwiftPM package)
    Sources/AetherMedia/
      social/   SocialGraph (actor-based, Swift Concurrency)
      player/   AVFoundation player
      feed/     Feed models
      streaming/ Stream models
  c/                              C11 feed and social models for embedded targets
    include/aether_media/         Public headers
    src/                          Implementations
    tests/                        CTest-based test suite
  android/                        Android Gradle modules
    media/      Main media module (Kotlin + Jetpack)
    media-tv/   Android TV variant
  docs/                           Architecture notes and design decisions
```

---

## 构建

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

## 许可证

MIT——永久免费。编解码器引擎（LibVLC / FFmpeg / AVFoundation / media3 / ExoPlayer）
分别通过 LGPL 和 Apache 2.0 使用，未作修改。参见 [LICENSE](LICENSE)。
