```
  ╔═╗ ╔═╗ ╔╦╗ ╦ ╦ ╔═╗ ╦═╗   ╔╦╗ ╔═╗ ╔╦╗ ╦ ╔═╗
  ╠═╣ ║╣   ║  ╠═╣ ║╣  ╠╦╝   ║║║ ║╣   ║║ ║ ╠═╣
  ╩ ╩ ╚═╝  ╩  ╩ ╩ ╚═╝ ╩╚═   ╩ ╩ ╚═╝ ═╩╝ ╩ ╩ ╩
  decentralised social media · no internet required
```

<div dir="rtl">

[![MIT License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![npm version](https://img.shields.io/npm/v/@bhengubv/aether-media.svg)](https://www.npmjs.com/package/@bhengubv/aether-media)
[![Build](https://github.com/bhengubv/aether-media/actions/workflows/ci.yml/badge.svg)](https://github.com/bhengubv/aether-media/actions/workflows/ci.yml)

> شبكة تواصل اجتماعي لامركزية ومشغّل وسائط مبني على بروتوكول شبكة Aether.
> لا إنترنت مطلوب. لا خادم مركزي. لا مالك شركة.

هاتفان في نفس الغرفة — دون Wi-Fi، دون بيانات موبايل — يمكنهما اكتشاف بعضهما، ومشاركة الوسائط، وبث الفيديو المباشر، والتفاعل الاجتماعي عبر BLE، أو Wi-Fi Direct، أو NearLink، أو LoRa، أو ترحيل HTTP.

---

## لماذا Aether Media؟

**ابث حفلة موسيقية مباشرة إلى هواتف الجمهور — دون إنترنت، دون CDN، دون رسوم بث.**

يبث جهاز المؤدي عبر Wi-Fi Direct. كل هاتف في النطاق يستقبل البث ويُرحّله أبعد عبر BLE. التفاعلات (الإعجابات، والتفاعلات المميزة، والتعليقات في موضع التشغيل الدقيق) تسير بالطريقة ذاتها في الاتجاه المعاكس. لا تخزين مؤقت من مركز بيانات على بُعد 10,000 كيلومتر. لا حساب مطلوب من الجمهور.

```
  [Performer] ──WiFi Direct──▶ [Row 1] ──BLE──▶ [Row 2] ──NearLink──▶ [Row 3]
                 1080p live           relayed, encrypted        relayed, encrypted
```

**تابع منشئاً غير متصل. استلم محتواه عندما يعود إلى النطاق.**

يُسلَّم المتابعون عبر طبقة DTN (الشبكات المتسامحة مع التأخير) للتخزين والإعادة في Aether. إذا لم يكن جهاز المنشئ متاحاً الآن، تنتظر نية المتابعة — حتى 72 ساعة — وتُسلَّم في اللحظة التي يُفتح فيها مسار. لا بنية تحتية لإشعارات الدفع، لا خادم تطبيق.

**شاهد فيلماً معاً عبر الشبكة.**

أحدهم على الشبكة لديه الملف. يُزامن منسق Watch Party التشغيل والإيقاف المؤقت والبحث عبر كل جهاز مع تعويض RTT. تنطلق التفاعلات في الوقت الفعلي عند الطابع الزمني الدقيق في الفيديو. إذا انقطع جهاز المضيف أثناء الفيلم، تنتقل الجلسة تلقائياً إلى النظير التالي المتاح.

---

### المقارنة مع المشغّلات والشبكات الحالية

| القدرة | VLC | Spotify | Streambert | Aether Media |
|---|:---:|:---:|:---:|:---:|
| يعمل غير متصل (دون إنترنت) | التشغيل فقط | لا | لا | **نعم — اكتشاف، بث، تفاعل** |
| يعمل دون حساب تطبيق | نعم | لا | لا | **نعم** |
| ترحيل شبكي (القفز عبر الأجهزة) | لا | لا | لا | **نعم** |
| بث مباشر، دون CDN | لا | لا | جزئي | **نعم — BLE / Wi-Fi Direct / NearLink** |
| تفاعلات اجتماعية على الخط الزمني | لا | لا | لا | **نعم — في موضع التشغيل الدقيق** |
| رسم بياني للمتابعين دون خادم | لا | لا | لا | **نعم — مُسلَّم عبر DTN** |
| زمن استجابة دون الثانية في نفس الغرفة | غير قابل للتطبيق | لا | لا | **نعم — NearLink 20 µs** |
| يعمل على متحكم دقيق / C11 | لا | لا | لا | **نعم — تطبيق C** |
| حزمة SDK بـ8 لغات | لا | لا | لا | **نعم** |

---

## كيف يعمل

**الخطوة 1 — اكتشاف الشبكة.** تبث الأجهزة علامة Aether الخاصة بها (معرّف هوية قصير مثل `@sam.5jk2`) عبر إعلانات BLE واستجابات مسح Wi-Fi Direct. لا عنوان IP ولا حساب مطلوب. تُعلي طبقة النقل تلقائياً أولوية NearLink (600 م، 12 Mbps، 20 µs) على الأجهزة التي تمتلكه، وتنتقل إلى Wi-Fi Direct (200 م، 250 Mbps)، ثم BLE (100 م، 1 Mbps)، ثم LoRa-over-BLE (1.3 كم)، وأخيراً ترحيل HTTP كملاذ أخير.

**الخطوة 2 — عنونة المحتوى.** كل قطعة وسائط تُعرَّف بتجزئة محتواها SHA-256 — وليس بعنوان URL أو مسار خادم. يُبث `ContentDescriptor` (التجزئة + الاسم + نوع MIME + بيان القطع) عبر الشبكة. أي جهاز لديه الملف يمكنه تقديم القطع لأي جهاز يحتاجها. لا خادم أصل. يمكن تجميع الملفات من أجزاء محتفظ بها لدى أقران مختلفين في آن واحد، على غرار BitTorrent.

**الخطوة 3 — الطبقة الاجتماعية.** المتابعات والتفاعلات وتحديثات الملف الشخصي مُشفَّرة كحمولات JSON موقَّعة وتُرسَّل إما كحزم DTN (للتسليم المتسامح مع عدم الاتصال) أو `MeshPacket`s ببذل أفضل الجهود (للتفاعلات منخفضة الزمن أثناء البث المباشر). يتتبع `SocialGraph` من تتابع. يستمع `FeedAggregator` لحزم `StreamAnnounce` و`ContentAnnounce` من المنشئين المتابَعين ويُجمّع خلاصة زمنية — بالكامل من أحداث الشبكة، دون خادم خلاصة.

---

## ميزات Aether المستخدمة

Aether Media مبني فوق [aether-protocol](https://github.com/bhengubv/aether-protocol) ويستخدم هذه الواجهات:

| واجهة Aether | الحزمة | كيف تستخدمها Aether Media |
|---|---|---|
| `ITransportService` | `AetherNet.Transport` | إرسال إطارات الفيديو/الصوت المُشفَّرة والتفاعلات ونوايا المتابعة عبر الشبكة (BLE / Wi-Fi Direct / NearLink / LoRa / ترحيل HTTP) |
| `IStreamingService` | `AetherNet.Streaming` | بث `StreamAnnounce` عند البث المباشر؛ `FeedAggregator` يشترك في أحداث `StreamAnnounced` و`StreamEnded` للحفاظ على خلاصة البث المباشر |
| `IContentService` | `AetherNet.Content` | نشر `ContentDescriptor`s للوسائط المرفوعة؛ `FeedAggregator` يشترك في `ContentAnnounced` لاكتشاف VOD |
| `IDtnService` | `AetherNet.Dtn` | تسليم نوايا المتابعة بشكل موثوق للمنشئين غير المتصلين؛ تنتظر الحزم حتى 72 ساعة للحصول على مسار |
| `IMeshSender` | `AetherNet.Messaging` | إرسال حزم إلغاء المتابعة والتفاعلات المباشرة عبر الشبكة دون تكاليف DTN |
| `IRoutingService` | `AetherNet.Routing` | تسليم الحزم الاجتماعية مع مراعاة المسار؛ RREQ/RREP بأسلوب AODV مع ردود مسار موقَّعة بـEd25519 |
| `SignalProtocolService` | `AetherNet.Security` | تشفير الرسائل المباشرة من طرف إلى طرف، وحمولات مزامنة الملف الشخصي، ومحتوى القنوات الخاصة بـX3DH + Double Ratchet |
| `IAdaptiveBitrateController` | `AetherNet.Streaming` | اختيار أعلى مستوى جودة مستدام (H.264 / H.265 / VP8) بناءً على تقديرات النطاق الترددي المباشرة من وسيلة النقل النشطة |

---

## حزمة SDK بـ8 لغات

تشحن Aether Media بتطبيقات بـ8 لغات لتعمل على كل منصة في النظام البيئي.

| اللغة | الدليل | المنصة | محرك الوسائط | الدور |
|---|---|---|---|---|
| C# (.NET 10) | `src/` | Windows · Linux · macOS | LibVLC (LibVLCSharp) | التطبيق المرجعي، DI كامل، حزم NuGet |
| TypeScript | `typescript/` | المتصفح · Node 20 | HLS.js · Shaka Player | مشغّل ويب، عميل خلاصة، SDK اجتماعي |
| Python | `python/` | Python 3.11+ | mutagen (بيانات وصفية) | محرك إضافات، برمجة نصية، معالجة البيانات الوصفية |
| Rust | `rust/` | أي هدف Rust | `rodio` (صوت) | محرك خلاصة عالي الأداء، معايير |
| Go | `go/` | أي هدف Go 1.22 | — | مكتبة الرسم البياني الاجتماعي |
| Kotlin | `kotlin/` | JVM 21 · Android | media3 / ExoPlayer (Android) | مشغّل Android؛ الرسم البياني الاجتماعي لـJVM للاستخدام من جانب الخادم |
| Swift | `swift/` | macOS 13+ · iOS 16+ | AVFoundation | مشغّل منصة Apple |
| C | `c/` | أي هدف C11 | — | نماذج خلاصة ووسائط اجتماعية للأجهزة المُضمَّنة / المتحكمات الدقيقة |

تشترك جميع التطبيقات الثمانية في نفس تنسيق أسلاك `aether-protocol` وتُنتج حزمًا اجتماعية قابلة للتشغيل البيني متحقق منها عبر مخرجات متعددة اللغات في CI.

---

## البدء السريع

### C# سطح المكتب (Windows / Linux / macOS)

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media
dotnet run --project samples/AetherMedia.Demo.Console
```

سجّل جميع الأنظمة الفرعية:

```csharp
services.AddAetherNetMedia(media =>
    media.AddIdentity()
         .AddContent()
         .AddSocial()
         .AddStreaming()
         .AddAI());
```

حلّ واستخدم:

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

### TypeScript (المتصفح)

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

عميل الخلاصة مع ذاكرة تخزين مؤقت محلية:

```typescript
import { FeedClient } from '@bhengubv/aether-media';

const client = new FeedClient('https://relay.aethernet.network/media');
const items  = await client.getFeed(20, 0);   // limit, offset

for (const item of items) {
    console.log(item.content.title, item.likeCount);
}

await client.markWatched('a3f9...', 45_000);  // contentHash, ms watched
```

### Python (إضافة)

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

## البروتوكول الاجتماعي

الطبقة الاجتماعية لا تمتلك خادماً. كل متابعة، وإلغاء متابعة، وإعلان محتوى، وتفاعل هو `MeshPacket` موقَّع أو `DtnBundle` يسير عبر أي راديو متاح.

تُغلَّف **المتابعات** في `FollowIntentPayload` (JSON بترميز UTF-8)، وتُشفَّر بمفتاح جلسة Signal Protocol للمنشئ المستهدف (X3DH + Double Ratchet)، وتُلتزم كـ`DtnBundle` موجَّه إلى UHID المستهدف. تخزن طبقة DTN الحزمة محلياً وتُسلّمها عبر الشبكة متى فُتح مسار إلى الهدف — حتى لو استغرق ذلك ساعات. يستلم جهاز المنشئ الحزمة، ويتحقق من التوقيع، ويزيد عدد متابعيه. كل هذا يحدث دون أن يعلم أي خادم مركزي بالعلاقة.

**إعلانات المحتوى** هي حزم `ContentDescriptor` تبثها الجهاز الناشر. كل جهاز يستلم الواصف يخزّنه ويُعيد بثه للأقران القريبين (فيضان الشبكة مع إزالة التكرار). يستمع `FeedAggregator` على كل جهاز لهذه البثوث ويُظهر المحتوى الجديد من المنشئين المتابَعين في الخلاصة المحلية.

**التفاعلات** (إعجاب، مشاركة، تفاعل مميز، تعليق) تحمل تجزئة المحتوى، ونوع التفاعل، وموضع التشغيل الدقيق بالميلي ثانية. تسير كـ`MeshPacket`s ببذل أفضل الجهود موجَّهة إلى UHID المنشئ — مُوجَّهة بـAODV مع ردود مسار موقَّعة، لذا لا يمكن لأي وجهة مزيفة اعتراضها. أثناء البث المباشر، تُجمَّع التفاعلات وتُعرض في الوقت الفعلي على جهاز الناشر دون مغادرة الشبكة.

**مزامنة الملف الشخصي** تستخدم نفس آلية DTN للمتابعات. عندما يُحدّث منشئ اسمه المعروض أو صورة رمزية أو سيرته الذاتية، يُوقَّع `MediaProfile` الجديد بمفتاح هويته Ed25519، ويُسلسَّل، ويُبث كحزمة DTN. أي جهاز يستلمه — مباشرةً أو عبر ترحيل — يتحقق من التوقيع ويُحدّث ذاكرته التخزينية المؤقتة المحلية. تحديث الملف الشخصي المُجرى أثناء عدم الاتصال يصل إلى المتابعين في المرة القادمة التي يأتي فيها أي منهم ضمن نطاق الراديو.

---

## هيكل المستودع

```
aether-media/
  src/
    AetherMedia.Core/            Domain models and interfaces (MediaContent, IMediaLibrary, etc.)
    AetherMedia.Identity/        Profile management, avatar, profile sync
    AetherMedia.Content/         Media library scanner, metadata resolver, LRU cache, thumbnails
    AetherMedia.Social/          SocialGraph, FeedAggregator, ReactionService, DiscoveryService
    AetherMedia.Streaming/       LiveStreamPublisher, WatchPartyCoordinator, AbrController
    AetherMedia.AI/              ContentRanker, ContentModerator, CreatorReputationView
    AetherMedia.DependencyInjection/  AddAetherNetMedia() extension + AetherNetMediaBuilder fluent API
    AetherMedia.Desktop/         LibVLCSharp integration for Windows / Linux / macOS
  samples/
    AetherMedia.Demo.Console/    Interactive console demo showing all subsystems
    AetherMedia.RelayTest/       HTTP relay round-trip test (requires AetherNet.RelayServer)
  tests/
    AetherMedia.Core.Tests/      Unit tests for domain models and InMemoryMediaLibrary
    AetherMedia.Social.Tests/    Unit tests for SocialGraph and FeedAggregator
  typescript/                     TypeScript web player and social SDK (@bhengubv/aether-media)
    src/
      player/   AetherNetMediaPlayer (HLS.js + Shaka Player + native MSE)
      social/   FeedClient, ReactionClient
      identity/ ProfileClient
      streaming/ AetherNetStreamClient
      models/   TypeScript mirrors of the C# domain models
  python/                         Python plugin engine and metadata library (aether-media on PyPI)
    aethermedia/
      plugins/  AetherNetMediaPlugin base class, PluginHost
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
    Sources/AetherNetMedia/
      social/   SocialGraph (actor-based, Swift Concurrency)
      player/   AVFoundation player
      feed/     Feed models
      streaming/ Stream models
  c/                              C11 feed and social models for embedded targets
    include/aethermedia/         Public headers
    src/                          Implementations
    tests/                        CTest-based test suite
  android/                        Android Gradle modules
    media/      Main media module (Kotlin + Jetpack)
    media-tv/   Android TV variant
  docs/                           Architecture notes and design decisions
```

---

## البناء

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

## الرخصة

MIT — مجاني للأبد. يُستخدم محرك الترميز (LibVLC / FFmpeg / AVFoundation / media3 / ExoPlayer) عبر LGPL وApache 2.0 على التوالي، دون تعديل. انظر [LICENSE](LICENSE).

</div>
