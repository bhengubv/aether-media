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

> یک شبکه رسانه اجتماعی غیرمتمرکز و پلیر ساخته‌شده بر پایه پروتکل mesh Aether.
> بدون اینترنت. بدون سرور مرکزی. بدون مالک شرکتی.

دو تلفن در یک اتاق — بدون Wi-Fi، بدون داده موبایل — می‌توانند یکدیگر را کشف کنند،
رسانه به اشتراک بگذارند، ویدیوی زنده پخش کنند و از طریق BLE، Wi-Fi Direct، NearLink،
LoRa یا رله HTTP واکنش اجتماعی نشان دهند.

---

## چرا Aether Media؟

**پخش یک کنسرت زنده به تلفن‌های مخاطبان — بدون اینترنت، بدون CDN، بدون کارمزد استریمینگ.**

دستگاه مجری از طریق Wi-Fi Direct پخش می‌کند. هر تلفن در محدوده جریان را دریافت کرده و از طریق BLE آن را به جلوتر رله می‌کند. واکنش‌ها (لایک‌ها، super-react، نظرات در موقعیت دقیق پخش) همان مسیر را برمی‌گردند. بدون بافرینگ از یک مرکز داده ۱۰,۰۰۰ کیلومتر دور. برای مخاطبان نیازی به حساب کاربری نیست.

```
  [Performer] ──WiFi Direct──▶ [Row 1] ──BLE──▶ [Row 2] ──NearLink──▶ [Row 3]
                 1080p live           relayed, encrypted        relayed, encrypted
```

**یک سازنده آفلاین را دنبال کنید. محتوای او را وقتی دوباره در محدوده آمد دریافت کنید.**

دنبال کردن از طریق لایه DTN (شبکه‌سازی تحمل‌پذیر تأخیر) store-and-forward Aether تحویل داده می‌شود. اگر دستگاه سازنده اکنون قابل دسترس نیست، قصد دنبال کردن منتظر می‌ماند — تا ۷۲ ساعت — و به محض باز شدن یک مسیر تحویل می‌دهد. بدون زیرساخت push-notification، بدون سرور برنامه.

**تماشای فیلم با هم از طریق mesh.**

کسی روی mesh فایل را دارد. هماهنگ‌کننده Watch Party پخش، توقف و جستجو را در همه دستگاه‌ها با جبران RTT همزمان می‌کند. واکنش‌ها در زمان واقعی در دقیقاً همان timestamp در ویدیو اجرا می‌شوند. اگر دستگاه میزبان در حین فیلم آفلاین شد، جلسه به‌طور خودکار به همتای بعدی موجود مهاجرت می‌کند.

---

### مقایسه با پلیرها و شبکه‌های موجود

| قابلیت | VLC | Spotify | Streambert | Aether Media |
|---|:---:|:---:|:---:|:---:|
| آفلاین کار می‌کند (بدون اینترنت) | فقط پخش | خیر | خیر | **بله — کشف، استریم، واکنش** |
| بدون حساب کاربری برنامه کار می‌کند | بله | خیر | خیر | **بله** |
| رله mesh (هاپ از طریق دستگاه‌ها) | خیر | خیر | خیر | **بله** |
| استریمینگ زنده، بدون CDN | خیر | خیر | جزئی | **بله — BLE / Wi-Fi Direct / NearLink** |
| واکنش‌های اجتماعی روی timeline | خیر | خیر | خیر | **بله — در موقعیت دقیق پخش** |
| گراف دنبال‌کردن بدون سرور | خیر | خیر | خیر | **بله — تحویل DTN** |
| تأخیر زیر ثانیه در یک اتاق | N/A | خیر | خیر | **بله — NearLink 20 میکروثانیه** |
| روی میکروکنترلر / C11 اجرا می‌شود | خیر | خیر | خیر | **بله — پیاده‌سازی C** |
| SDK هشت‌زبانه | خیر | خیر | خیر | **بله** |

---

## چطور کار می‌کند

**مرحله ۱ — کشف mesh.** دستگاه‌ها Aether Tag خود (یک دستگیره هویتی کوتاه مانند `@sam.5jk2`) را از طریق تبلیغات BLE و پاسخ‌های probe Wi-Fi Direct پخش می‌کنند. نیازی به آدرس IP یا حساب کاربری نیست. لایه انتقال به‌طور خودکار NearLink (۶۰۰ متر، ۱۲ مگابیت/ثانیه، ۲۰ میکروثانیه) را در دستگاه‌هایی که آن را دارند ارتقا می‌دهد، به Wi-Fi Direct (200 متر، 250 مگابیت/ثانیه)، سپس BLE (100 متر، 1 مگابیت/ثانیه)، سپس LoRa-over-BLE (1.3 کیلومتر)، و در نهایت رله HTTP به‌عنوان آخرین چاره برمی‌گردد.

**مرحله ۲ — آدرس‌دهی محتوا.** هر قطعه رسانه با hash محتوای SHA-256 آن شناسایی می‌شود — نه با URL یا مسیر سرور. یک `ContentDescriptor` (hash + نام + MIME type + manifest تکه) از طریق mesh پخش می‌شود. هر دستگاهی که فایل را دارد می‌تواند تکه‌ها را به هر دستگاهی که نیاز دارد ارائه دهد. هیچ سرور مبدأیی وجود ندارد. فایل‌ها می‌توانند از قطعاتی که توسط همتایان مختلف به‌طور همزمان نگه داشته می‌شوند، به سبک BitTorrent، مونتاژ شوند.

**مرحله ۳ — لایه اجتماعی.** دنبال کردن، واکنش‌ها و به‌روزرسانی‌های پروفایل به‌عنوان پیلودهای JSON امضاشده رمزگذاری شده و به‌عنوان بسته‌های DTN (برای تحویل تحمل‌پذیر در آفلاین) یا `MeshPacket`های best-effort (برای واکنش‌های کم‌تأخیر در طول پخش‌های زنده) ارسال می‌شوند. `SocialGraph` ردیابی می‌کند که چه کسی را دنبال می‌کنید. `FeedAggregator` به بسته‌های `StreamAnnounce` و `ContentAnnounce` از سازندگان دنبال‌شده گوش می‌دهد و یک فید زمانی مرتب می‌سازد — کاملاً از رویدادهای mesh، بدون سرور فید.

---

## از چه ویژگی‌های Aether استفاده می‌شود

Aether Media بر پایه [aether-protocol](https://github.com/bhengubv/aether-protocol) ساخته شده و از این رابط‌ها استفاده می‌کند:

| رابط Aether | پکیج | نحوه استفاده Aether Media |
|---|---|---|
| `ITransportService` | `AetherNet.Transport` | فریم‌های ویدیو/صدای رمزگذاری‌شده، واکنش‌ها و قصدهای دنبال‌کردن را از طریق mesh ارسال می‌کند (BLE / Wi-Fi Direct / NearLink / LoRa / رله HTTP) |
| `IStreamingService` | `AetherNet.Streaming` | هنگام زنده شدن `StreamAnnounce` پخش می‌کند؛ `FeedAggregator` برای رویدادهای `StreamAnnounced` و `StreamEnded` مشترک می‌شود |
| `IContentService` | `AetherNet.Content` | `ContentDescriptor`ها را برای رسانه آپلودشده منتشر می‌کند؛ `FeedAggregator` برای `ContentAnnounced` برای کشف VOD مشترک می‌شود |
| `IDtnService` | `AetherNet.Dtn` | قصدهای دنبال‌کردن را به‌طور پایدار به سازندگان آفلاین تحویل می‌دهد؛ بسته‌ها تا ۷۲ ساعت منتظر مسیر می‌مانند |
| `IMeshSender` | `AetherNet.Messaging` | بسته‌های unfollow best-effort و واکنش‌های زنده را بدون سربار DTN از طریق mesh ارسال می‌کند |
| `IRoutingService` | `AetherNet.Routing` | تحویل route-aware بسته‌های اجتماعی؛ RREQ/RREP به‌سبک AODV با پاسخ‌های مسیر امضاشده Ed25519 |
| `SignalProtocolService` | `AetherNet.Security` | پیام‌های مستقیم، پیلودهای همگام‌سازی پروفایل و محتوای کانال خصوصی را با X3DH + Double Ratchet رمزگذاری انتها-به-انتها می‌کند |
| `IAdaptiveBitrateController` | `AetherNet.Streaming` | بالاترین رتبه کیفیت پایدار (H.264 / H.265 / VP8) را بر اساس تخمین‌های پهنای باند زنده از انتقال فعال انتخاب می‌کند |

---

## SDK هشت‌زبانه

Aether Media در ۸ زبان پیاده‌سازی ارائه می‌دهد تا روی هر پلتفرمی در اکوسیستم اجرا شود.

| زبان | پوشه | پلتفرم | موتور رسانه | نقش |
|---|---|---|---|---|
| C# (.NET 10) | `src/` | Windows · Linux · macOS | LibVLC (LibVLCSharp) | پیاده‌سازی مرجع، DI کامل، پکیج‌های NuGet |
| TypeScript | `typescript/` | مرورگر · Node 20 | HLS.js · Shaka Player | پلیر وب، کلاینت فید، SDK اجتماعی |
| Python | `python/` | هر Python 3.11+ | mutagen (متادیتا) | موتور پلاگین، اسکریپ‌نویسی، پردازش متادیتا |
| Rust | `rust/` | هر هدف Rust | `rodio` (صدا) | موتور فید پرعملکرد، معیارسنجی |
| Go | `go/` | هر هدف Go 1.22 | — | کتابخانه گراف اجتماعی |
| Kotlin | `kotlin/` | JVM 21 · Android | media3 / ExoPlayer (Android) | پلیر Android؛ گراف اجتماعی JVM برای استفاده سمت سرور |
| Swift | `swift/` | macOS 13+ · iOS 16+ | AVFoundation | پلیر پلتفرم Apple |
| C | `c/` | هر هدف C11 | — | مدل‌های فید و اجتماعی embedded / میکروکنترلر |

همه ۸ پیاده‌سازی فرمت سیم یکسانی با `aether-protocol` دارند و
بسته‌های اجتماعی تعامل‌پذیر تأییدشده با fixture‌های بین‌زبانی در CI تولید می‌کنند.

---

## شروع سریع

### C# دسکتاپ (Windows / Linux / macOS)

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media
dotnet run --project samples/AetherNet.Media.Demo.Console
```

همه زیرسیستم‌ها را ثبت کنید:

```csharp
services.AddAetherNetMedia(media =>
    media.AddIdentity()
         .AddContent()
         .AddSocial()
         .AddStreaming()
         .AddAI());
```

رزولوشن و استفاده:

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

### TypeScript (مرورگر)

```typescript
import { AetherNetMediaPlayer } from '@bhengubv/aether-media';

const video  = document.querySelector('video') as HTMLVideoElement;
const player = new AetherNetMediaPlayer(video);

// یک جریان HLS منتشرشده توسط یک همتا روی mesh را بارگذاری کنید
await player.load('aether://stream/KXJB7-MN2P4');
await player.play();

// تکه‌های mesh خام را مستقیماً به pipeline MSE تزریق کنید
player.feedSegment(encodedBytes, 'video/mp4; codecs="avc1.42E01E"');
```

کلاینت فید با cache ذخیره‌سازی محلی:

```typescript
import { FeedClient } from '@bhengubv/aether-media';

const client = new FeedClient('https://relay.aethernet.network/media');
const items  = await client.getFeed(20, 0);   // محدودیت، افست

for (const item of items) {
    console.log(item.content.title, item.likeCount);
}

await client.markWatched('a3f9...', 45_000);  // contentHash، میلی‌ثانیه تماشاشده
```

### Python (پلاگین)

```python
from aethernet_media.plugins.base import AetherNetMediaPlugin
from aethernet_media.models import MediaContent, MediaReaction

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
import aethernet.media.social.SocialGraph

val graph = SocialGraph()
graph.follow("KXJB7-MN2P4")
println(graph.isFollowing("KXJB7-MN2P4"))  // true
println(graph.getFollowing())               // ["KXJB7-MN2P4"]
graph.unfollow("KXJB7-MN2P4")
println(graph.count)                        // 0
```

### Rust

```rust
use aethernet_media::feed::{FeedStore, FeedEntry};

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
#include "aethernet_media/social.h"

aethernet_social_graph_t *graph = aethernet_social_graph_create();
aethernet_social_graph_follow(graph, "KXJB7-MN2P4");
printf("Following: %d\n", aethernet_social_graph_is_following(graph, "KXJB7-MN2P4")); // 1
aethernet_social_graph_destroy(graph);
```

---

## پروتکل اجتماعی

لایه اجتماعی هیچ سروری ندارد. هر دنبال کردن، unfollow، اعلان محتوا و
واکنش یک `MeshPacket` یا `DtnBundle` امضاشده است که از هر رادیویی که موجود باشد عبور می‌کند.

**دنبال کردن** در یک `FollowIntentPayload` (JSON با رمزگذاری UTF-8) پیچیده می‌شود، با کلید جلسه Signal Protocol سازنده هدف (X3DH + Double Ratchet) رمزگذاری می‌شود، و به‌عنوان یک `DtnBundle` خطاب به UHID هدف commit می‌شود. لایه DTN بسته را به‌صورت محلی ذخیره می‌کند و هر زمان که مسیری به هدف باز شود — حتی اگر ساعت‌ها طول بکشد — از طریق mesh تحویل می‌دهد. دستگاه سازنده بسته را دریافت می‌کند، امضا را تأیید می‌کند و تعداد دنبال‌کنندگان را افزایش می‌دهد. همه اینها بدون اینکه هیچ سرور مرکزی از این رابطه آگاه شود.

**اعلان‌های محتوا** بسته‌های `ContentDescriptor` هستند که توسط دستگاه منتشرکننده پخش می‌شوند. هر دستگاهی که descriptor را دریافت می‌کند آن را cache می‌کند و به همتایان نزدیک دوباره پخش می‌کند (flood mesh با حذف تکراری). `FeedAggregator` روی هر دستگاه به این پخش‌ها گوش می‌دهد و محتوای جدید از سازندگان دنبال‌شده را در فید محلی نمایش می‌دهد.

**واکنش‌ها** (لایک، اشتراک‌گذاری، super-react، نظر) hash محتوا، نوع واکنش و موقعیت دقیق پخش را بر حسب میلی‌ثانیه حمل می‌کنند. آن‌ها به‌عنوان `MeshPacket`های best-effort خطاب به UHID سازنده سفر می‌کنند — توسط AODV با پاسخ‌های مسیر امضاشده مسیریابی می‌شوند، بنابراین هیچ مقصد جعلی نمی‌تواند آن‌ها را رهگیری کند. در طول یک پخش زنده، واکنش‌ها در زمان واقعی روی دستگاه ناشر جمع‌آوری و نمایش داده می‌شوند بدون اینکه mesh را ترک کنند.

**همگام‌سازی پروفایل** از همان مکانیزم DTN به‌عنوان دنبال کردن استفاده می‌کند. وقتی یک سازنده نام نمایشی، آواتار یا بیوی خود را به‌روز می‌کند، `MediaProfile` جدید با کلید هویتی Ed25519 آن‌ها امضا، سریال‌سازی و به‌عنوان یک بسته DTN پخش می‌شود. هر دستگاهی که آن را دریافت می‌کند — مستقیماً یا از طریق رله — امضا را تأیید و cache محلی خود را به‌روز می‌کند. یک به‌روزرسانی پروفایل ساخته‌شده در حالت آفلاین به دنبال‌کنندگان می‌رسد هر بار که هر کدام از آن‌ها در محدوده رادیویی قرار گیرند.

---

## ساختار مخزن

```
aether-media/
  src/
    AetherNet.Media.Core/            مدل‌های دامنه و رابط‌ها (MediaContent، IMediaLibrary و غیره)
    AetherNet.Media.Identity/        مدیریت پروفایل، آواتار، همگام‌سازی پروفایل
    AetherNet.Media.Content/         اسکنر کتابخانه رسانه، رزولور متادیتا، cache LRU، thumbnail
    AetherNet.Media.Social/          SocialGraph، FeedAggregator، ReactionService، DiscoveryService
    AetherNet.Media.Streaming/       LiveStreamPublisher، WatchPartyCoordinator، AbrController
    AetherNet.Media.AI/              ContentRanker، ContentModerator، CreatorReputationView
    AetherNet.Media.DependencyInjection/  پسوند AddAetherNetMedia() + API fluent AetherNetMediaBuilder
    AetherNet.Media.Desktop/         یکپارچه‌سازی LibVLCSharp برای Windows / Linux / macOS
  samples/
    AetherNet.Media.Demo.Console/    دمو کنسول تعاملی نشان‌دهنده همه زیرسیستم‌ها
    AetherNet.Media.RelayTest/       آزمون رفت‌وبرگشت رله HTTP (نیاز به AetherNet.RelayServer)
  tests/
    AetherNet.Media.Core.Tests/      آزمون‌های واحد برای مدل‌های دامنه و InMemoryMediaLibrary
    AetherNet.Media.Social.Tests/    آزمون‌های واحد برای SocialGraph و FeedAggregator
  typescript/                     پلیر وب TypeScript و SDK اجتماعی (@bhengubv/aether-media)
    src/
      player/   AetherNetMediaPlayer (HLS.js + Shaka Player + MSE بومی)
      social/   FeedClient، ReactionClient
      identity/ ProfileClient
      streaming/ AetherNetStreamClient
      models/   آینه‌های TypeScript از مدل‌های دامنه C#
  python/                         موتور پلاگین Python و کتابخانه متادیتا (aether-media در PyPI)
    aethernet_media/
      plugins/  کلاس پایه AetherNetMediaPlugin، PluginHost
      metadata/ خواننده/نویسنده Tag (wrapper mutagen)
      cli/      نقاط ورودی خط فرمان
  rust/                           موتور فید Rust (aether-media در crates.io)
    src/
      feed/     FeedStore، FeedEntry
      social/   SocialGraph، follow/unfollow
      streaming/ StreamAnnounce، مدل‌های segment
      player/   پخش صدا از طریق rodio
  go/                             کتابخانه گراف اجتماعی Go (github.com/bhengubv/aether-media/go)
    social/     SocialGraph
    player/     مدل‌های پلیر
    feed/       مدل‌های فید
    streaming/  مدل‌های استریم
  kotlin/                         گراف اجتماعی Kotlin/JVM + یکپارچه‌سازی ExoPlayer Android
    src/main/kotlin/
      social/   SocialGraph (مبتنی بر ConcurrentHashMap، JVM و Android)
      feed/     مدل‌های فید
      player/   یکپارچه‌سازی ExoPlayer (Android؛ آزمون‌های JVM فقط از core استفاده می‌کنند)
      content/  مدل‌های descriptor محتوا
      streaming/ مدل‌های جلسه استریم
    android/    ماژول Android Gradle با وابستگی media3-exoplayer
  swift/                          پلیر پلتفرم Swift / Apple (پکیج SwiftPM)
    Sources/AetherNetMedia/
      social/   SocialGraph (مبتنی بر actor، Swift Concurrency)
      player/   پلیر AVFoundation
      feed/     مدل‌های فید
      streaming/ مدل‌های استریم
  c/                              مدل‌های فید و اجتماعی C11 برای اهداف embedded
    include/aethernet_media/         هدرهای عمومی
    src/                          پیاده‌سازی‌ها
    tests/                        مجموعه آزمون مبتنی بر CTest
  android/                        ماژول‌های Android Gradle
    media/      ماژول رسانه اصلی (Kotlin + Jetpack)
    media-tv/   نوع Android TV
  docs/                           یادداشت‌های معماری و تصمیمات طراحی
```

---

## ساخت

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

## مجوز

MIT — برای همیشه رایگان. موتور کدک (LibVLC / FFmpeg / AVFoundation / media3 / ExoPlayer)
از طریق LGPL و Apache 2.0 به ترتیب، بدون تغییر استفاده می‌شود. به [LICENSE](LICENSE) مراجعه کنید.

</div>
