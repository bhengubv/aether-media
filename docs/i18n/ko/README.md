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

> Aether 메시 프로토콜 위에 구축된 분산형 소셜 미디어 네트워크 및 플레이어입니다.
> 인터넷 불필요. 중앙 서버 불필요. 기업 소유자 불필요.

같은 공간에 있는 두 대의 스마트폰 — Wi-Fi도 모바일 데이터도 없이 — BLE, Wi-Fi Direct, NearLink,
LoRa, 또는 HTTP 중계를 통해 서로를 발견하고, 미디어를 공유하고, 라이브 영상을 스트리밍하고, 소셜 반응을 주고받을 수 있습니다.

---

## 왜 Aether Media인가요?

**인터넷, CDN, 스트리밍 요금 없이 관객의 스마트폰에 라이브 콘서트를 스트리밍하세요.**

공연자의 기기가 Wi-Fi Direct로 방송합니다. 범위 내 모든 스마트폰이 스트림을 수신하고 BLE를 통해 더 멀리 전달합니다. 반응(좋아요, 슈퍼 리액트, 정확한 재생 위치의 댓글)도 같은 방식으로 돌아옵니다. 10,000km 떨어진 데이터 센터에서의 버퍼링 없습니다. 관객은 계정이 필요하지 않습니다.

```
  [Performer] ──WiFi Direct──▶ [Row 1] ──BLE──▶ [Row 2] ──NearLink──▶ [Row 3]
                 1080p live           relayed, encrypted        relayed, encrypted
```

**오프라인인 크리에이터를 팔로우하세요. 범위 내로 돌아오면 콘텐츠를 받으세요.**

팔로우는 Aether의 DTN (지연 허용 네트워킹) 저장-전달 계층을 통해 전달됩니다. 크리에이터의 기기가 지금 도달 불가능하다면, 팔로우 의도는 — 최대 72시간 동안 — 기다렸다가 경로가 열리는 순간 전달됩니다. 푸시 알림 인프라도, 앱 서버도 없습니다.

**메시를 통해 함께 영화를 보세요.**

메시의 누군가가 파일을 갖고 있습니다. Watch Party 조정자는 RTT 보상과 함께 모든 기기에서 재생, 일시정지, 탐색을 동기화합니다. 반응은 영상의 정확한 타임스탬프에 실시간으로 발생합니다. 호스트의 기기가 영화 중간에 오프라인이 되면 다음 사용 가능한 피어로 세션이 자동으로 이동합니다.

---

### 기존 플레이어 및 네트워크와의 비교

| 기능 | VLC | Spotify | Streambert | Aether Media |
|---|:---:|:---:|:---:|:---:|
| 오프라인 작동 (인터넷 불필요) | 재생만 | 불가 | 불가 | **가능 — 검색, 스트리밍, 반응** |
| 앱 계정 없이 사용 가능 | 가능 | 불가 | 불가 | **가능** |
| 메시 중계 (기기를 통한 홉) | 불가 | 불가 | 불가 | **가능** |
| CDN 없는 라이브 스트리밍 | 불가 | 불가 | 부분 | **가능 — BLE / Wi-Fi Direct / NearLink** |
| 타임라인의 소셜 반응 | 불가 | 불가 | 불가 | **가능 — 정확한 재생 위치에** |
| 서버 없는 팔로우 그래프 | 불가 | 불가 | 불가 | **가능 — DTN 전달** |
| 같은 공간에서 1초 미만 지연 | 해당 없음 | 불가 | 불가 | **가능 — NearLink 20 µs** |
| 마이크로컨트롤러 / C11 실행 | 불가 | 불가 | 불가 | **가능 — C 구현체** |
| 8개 언어 SDK | 불가 | 불가 | 불가 | **가능** |

---

## 작동 원리

**1단계 — 메시 검색.** 기기들은 BLE 광고와 Wi-Fi Direct 프로브 응답을 통해 Aether Tag (예: `@sam.5jk2`와 같은 짧은 신원 핸들)를 방송합니다. IP 주소나 계정이 필요하지 않습니다. 전송 계층은 NearLink (600 m, 12 Mbps, 20 µs)를 지원하는 기기에서 자동으로 우선시하며, Wi-Fi Direct (200 m, 250 Mbps), BLE (100 m, 1 Mbps), LoRa-over-BLE (1.3 km), 그리고 최후 수단으로 HTTP 중계로 폴백합니다.

**2단계 — 콘텐츠 주소 지정.** 모든 미디어는 URL이나 서버 경로가 아닌 SHA-256 콘텐츠 해시로 식별됩니다. `ContentDescriptor` (해시 + 이름 + MIME 타입 + 청크 매니페스트)가 메시를 통해 방송됩니다. 파일을 가진 모든 기기가 해당 파일이 필요한 기기에 청크를 제공할 수 있습니다. 원본 서버가 없습니다. BitTorrent 방식으로 여러 피어가 동시에 보유한 조각들로 파일을 조립할 수 있습니다.

**3단계 — 소셜 계층.** 팔로우, 반응, 프로필 업데이트는 서명된 JSON 페이로드로 인코딩되어 DTN 번들 (오프라인 허용 전달용) 또는 최선형 `MeshPacket` (라이브 스트림 중 저지연 반응용)으로 전송됩니다. `SocialGraph`는 팔로우 관계를 추적합니다. `FeedAggregator`는 팔로우한 크리에이터의 `StreamAnnounce`와 `ContentAnnounce` 패킷을 수신하여 피드 서버 없이 메시 이벤트만으로 시간순 피드를 구성합니다.

---

## 사용된 Aether 기능

Aether Media는 [aether-protocol](https://github.com/bhengubv/aether-protocol) 위에 구축되어 있으며 다음 인터페이스를 사용합니다:

| Aether 인터페이스 | 패키지 | Aether Media에서의 사용 방식 |
|---|---|---|
| `ITransportService` | `AetherNet.Transport` | 인코딩된 영상/오디오 프레임, 반응, 팔로우 의도를 메시 (BLE / Wi-Fi Direct / NearLink / LoRa / HTTP 중계)를 통해 전송 |
| `IStreamingService` | `AetherNet.Streaming` | 라이브 시작 시 `StreamAnnounce` 방송; `FeedAggregator`가 라이브 스트림 피드 유지를 위해 `StreamAnnounced` 및 `StreamEnded` 이벤트 구독 |
| `IContentService` | `AetherNet.Content` | 업로드된 미디어에 대한 `ContentDescriptor` 게시; `FeedAggregator`가 VOD 검색을 위해 `ContentAnnounced` 구독 |
| `IDtnService` | `AetherNet.Dtn` | 오프라인 크리에이터에게 팔로우 의도 안정적으로 전달; 번들이 최대 72시간 동안 경로를 기다림 |
| `IMeshSender` | `AetherNet.Messaging` | DTN 오버헤드 없이 최선형 언팔로우 패킷과 라이브 반응을 메시를 통해 전송 |
| `IRoutingService` | `AetherNet.Routing` | 소셜 패킷의 경로 인식 전달; Ed25519 서명 경로 응답을 갖는 AODV 방식 RREQ/RREP |
| `SignalProtocolService` | `AetherNet.Security` | X3DH + Double Ratchet으로 직접 메시지, 프로필 동기화 페이로드, 비공개 채널 콘텐츠를 종단 간 암호화 |
| `IAdaptiveBitrateController` | `AetherNet.Streaming` | 활성 전송의 실시간 대역폭 추정을 기반으로 최고 지속 가능한 품질 등급 (H.264 / H.265 / VP8) 선택 |

---

## 8개 언어 SDK

Aether Media는 생태계의 모든 플랫폼에서 실행될 수 있도록 8개 언어로 구현체를 제공합니다.

| 언어 | 디렉터리 | 플랫폼 | 미디어 엔진 | 역할 |
|---|---|---|---|---|
| C# (.NET 10) | `src/` | Windows · Linux · macOS | LibVLC (LibVLCSharp) | 참조 구현체, 완전한 DI, NuGet 패키지 |
| TypeScript | `typescript/` | 브라우저 · Node 20 | HLS.js · Shaka Player | 웹 플레이어, 피드 클라이언트, 소셜 SDK |
| Python | `python/` | Python 3.11+ | mutagen (메타데이터) | 플러그인 엔진, 스크립팅, 메타데이터 처리 |
| Rust | `rust/` | 모든 Rust 대상 | `rodio` (오디오) | 고성능 피드 엔진, 벤치마크 |
| Go | `go/` | 모든 Go 1.22 대상 | — | 소셜 그래프 라이브러리 |
| Kotlin | `kotlin/` | JVM 21 · Android | media3 / ExoPlayer (Android) | Android 플레이어; 서버 측 JVM 소셜 그래프 |
| Swift | `swift/` | macOS 13+ · iOS 16+ | AVFoundation | Apple 플랫폼 플레이어 |
| C | `c/` | 모든 C11 대상 | — | 임베디드 / 마이크로컨트롤러 피드 및 소셜 모델 |

8개 구현체 모두 `aether-protocol`과 동일한 와이어 형식을 공유하며
CI의 언어 간 픽스처로 검증된 상호 운용 가능한 소셜 패킷을 생성합니다.

---

## 빠른 시작

### C# 데스크탑 (Windows / Linux / macOS)

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media
dotnet run --project samples/AetherMedia.Demo.Console
```

모든 서브시스템 등록:

```csharp
services.AddAetherNetMedia(media =>
    media.AddIdentity()
         .AddContent()
         .AddSocial()
         .AddStreaming()
         .AddAI());
```

확인 및 사용:

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

### TypeScript (브라우저)

```typescript
import { AetherNetMediaPlayer } from '@bhengubv/aether-media';

const video  = document.querySelector('video') as HTMLVideoElement;
const player = new AetherNetMediaPlayer(video);

// Load an HLS stream published by a peer on the mesh
await player.load('aether://stream/KXJB7-MN2P4');
await player.play();

// Feed raw mesh segments directly into the MSE pipeline
player.feedSegment(encodedBytes, 'video/mp4; codecs="avc1.42E01E"');
```

로컬 저장소 캐시가 있는 피드 클라이언트:

```typescript
import { FeedClient } from '@bhengubv/aether-media';

const client = new FeedClient('https://relay.aethernet.network/media');
const items  = await client.getFeed(20, 0);   // limit, offset

for (const item of items) {
    console.log(item.content.title, item.likeCount);
}

await client.markWatched('a3f9...', 45_000);  // contentHash, ms watched
```

### Python (플러그인)

```python
from aethermedia.plugins.base import AetherNetMediaPlugin
from aethermedia.models import MediaContent, MediaReaction

class MyPlugin(AetherNetMediaPlugin):
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

### Kotlin (Android / JVM)

```kotlin
import aethermedia.social.SocialGraph

val graph = SocialGraph()
graph.follow("KXJB7-MN2P4")
println(graph.isFollowing("KXJB7-MN2P4"))  // true
println(graph.getFollowing())               // ["KXJB7-MN2P4"]
graph.unfollow("KXJB7-MN2P4")
println(graph.count)                        // 0
```

### Rust

```rust
use aethermedia::feed::{FeedStore, FeedEntry};

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
import AetherNetMedia

let graph = SocialGraph()
try await graph.follow(uhid: "KXJB7-MN2P4")
let following = try await graph.following()
print(following) // ["KXJB7-MN2P4"]
```

### C

```c
#include "aethermedia/social.h"

aethernet_social_graph_t *graph = aethernet_social_graph_create();
aethernet_social_graph_follow(graph, "KXJB7-MN2P4");
printf("Following: %d\n", aethernet_social_graph_is_following(graph, "KXJB7-MN2P4")); // 1
aethernet_social_graph_destroy(graph);
```

---

## 소셜 프로토콜

소셜 계층에는 서버가 없습니다. 모든 팔로우, 언팔로우, 콘텐츠 공지,
반응은 서명된 `MeshPacket` 또는 `DtnBundle`로, 사용 가능한 어떤 라디오를 통해서도 전달됩니다.

**팔로우**는 `FollowIntentPayload` (UTF-8 JSON)에 래핑되어, 대상 크리에이터의
Signal Protocol 세션 키 (X3DH + Double Ratchet)로 암호화되고, 대상 UHID로 주소가 지정된
`DtnBundle`로 커밋됩니다. DTN 계층은 번들을 로컬에 저장하고 경로가 열릴 때마다 메시를 통해 전달합니다 —
몇 시간이 걸리더라도. 크리에이터의 기기는 번들을 수신하고, 서명을 검증하고, 팔로워 수를 증가시킵니다.
이 모든 과정이 어떤 중앙 서버도 해당 관계를 알지 못한 채 일어납니다.

**콘텐츠 공지**는 게시 기기가 방송하는 `ContentDescriptor` 패킷입니다. 디스크립터를
수신하는 모든 기기는 이를 캐싱하고 근처 피어에게 재방송합니다 (중복 제거가 있는 메시 플러드).
각 기기의 `FeedAggregator`는 이러한 방송을 수신하고 팔로우한 크리에이터의 새 콘텐츠를
로컬 피드에 표시합니다.

**반응** (좋아요, 공유, 슈퍼 리액트, 댓글)은 콘텐츠 해시, 반응 타입, 밀리초 단위의
정확한 재생 위치를 담습니다. 크리에이터의 UHID로 주소가 지정된 최선형 `MeshPacket`으로
전달됩니다 — 서명된 경로 응답을 갖는 AODV로 라우팅되어 어떤 가짜 목적지도 이를 가로챌 수 없습니다.
라이브 스트림 중에는 반응이 집계되어 메시를 벗어나지 않고 게시자의 기기에 실시간으로 표시됩니다.

**프로필 동기화**는 팔로우와 동일한 DTN 메커니즘을 사용합니다. 크리에이터가 표시 이름,
아바타, 소개를 업데이트하면, 새 `MediaProfile`이 Ed25519 신원 키로 서명되고, 직렬화되어
DTN 번들로 방송됩니다. 이를 수신하는 모든 기기 — 직접 또는 중계를 통해 — 는 서명을 검증하고
로컬 캐시를 업데이트합니다. 오프라인 중에 이루어진 프로필 업데이트는 팔로워 중 누군가가
라디오 범위 내로 들어오는 다음 순간에 팔로워들에게 전달됩니다.

---

## 저장소 구조

```
aether-media/
  src/
    AetherMedia.Core/            도메인 모델 및 인터페이스 (MediaContent, IMediaLibrary 등)
    AetherMedia.Identity/        프로필 관리, 아바타, 프로필 동기화
    AetherMedia.Content/         미디어 라이브러리 스캐너, 메타데이터 확인자, LRU 캐시, 썸네일
    AetherMedia.Social/          SocialGraph, FeedAggregator, ReactionService, DiscoveryService
    AetherMedia.Streaming/       LiveStreamPublisher, WatchPartyCoordinator, AbrController
    AetherMedia.AI/              ContentRanker, ContentModerator, CreatorReputationView
    AetherMedia.DependencyInjection/  AddAetherNetMedia() 확장 + AetherNetMediaBuilder 플루언트 API
    AetherMedia.Desktop/         Windows / Linux / macOS용 LibVLCSharp 통합
  samples/
    AetherMedia.Demo.Console/    모든 서브시스템을 보여주는 인터랙티브 콘솔 데모
    AetherMedia.RelayTest/       HTTP 중계 왕복 테스트 (AetherNet.RelayServer 필요)
  tests/
    AetherMedia.Core.Tests/      도메인 모델 및 InMemoryMediaLibrary 단위 테스트
    AetherMedia.Social.Tests/    SocialGraph 및 FeedAggregator 단위 테스트
  typescript/                     TypeScript 웹 플레이어 및 소셜 SDK (@bhengubv/aether-media)
    src/
      player/   AetherNetMediaPlayer (HLS.js + Shaka Player + 네이티브 MSE)
      social/   FeedClient, ReactionClient
      identity/ ProfileClient
      streaming/ AetherNetStreamClient
      models/   C# 도메인 모델의 TypeScript 미러
  python/                         Python 플러그인 엔진 및 메타데이터 라이브러리 (PyPI의 aether-media)
    aethermedia/
      plugins/  AetherNetMediaPlugin 기본 클래스, PluginHost
      metadata/ 태그 리더/라이터 (mutagen 래퍼)
      cli/      커맨드라인 진입점
  rust/                           Rust 피드 엔진 (crates.io의 aether-media)
    src/
      feed/     FeedStore, FeedEntry
      social/   SocialGraph, 팔로우/언팔로우
      streaming/ StreamAnnounce, 세그먼트 모델
      player/   rodio를 통한 오디오 재생
  go/                             Go 소셜 그래프 라이브러리 (github.com/bhengubv/aether-media/go)
    social/     SocialGraph
    player/     플레이어 모델
    feed/       피드 모델
    streaming/  스트림 모델
  kotlin/                         Kotlin/JVM 소셜 그래프 + Android ExoPlayer 통합
    src/main/kotlin/
      social/   SocialGraph (ConcurrentHashMap 기반, JVM 및 Android)
      feed/     피드 모델
      player/   ExoPlayer 통합 (Android; JVM 테스트는 코어만 사용)
      content/  콘텐츠 디스크립터 모델
      streaming/ 스트림 세션 모델
    android/    media3-exoplayer 의존성이 있는 Gradle Android 모듈
  swift/                          Swift / Apple 플랫폼 플레이어 (SwiftPM 패키지)
    Sources/AetherNetMedia/
      social/   SocialGraph (액터 기반, Swift Concurrency)
      player/   AVFoundation 플레이어
      feed/     피드 모델
      streaming/ 스트림 모델
  c/                              임베디드 대상용 C11 피드 및 소셜 모델
    include/aethermedia/         공개 헤더
    src/                          구현체
    tests/                        CTest 기반 테스트 스위트
  android/                        Android Gradle 모듈
    media/      주 미디어 모듈 (Kotlin + Jetpack)
    media-tv/   Android TV 변형
  docs/                           아키텍처 노트 및 설계 결정 사항
```

---

## 빌드

### C#

```bash
dotnet build AetherNetMedia.slnx
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

## 라이선스

MIT — 영구 무료. 코덱 엔진 (LibVLC / FFmpeg / AVFoundation / media3 / ExoPlayer)은
각각 LGPL 및 Apache 2.0 라이선스 하에 수정 없이 사용됩니다. [LICENSE](LICENSE) 참조.
