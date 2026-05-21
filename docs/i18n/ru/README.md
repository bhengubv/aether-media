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

> Децентрализованная социальная медиасеть и проигрыватель на базе mesh-протокола Aether.
> Интернет не нужен. Центральный сервер не нужен. Корпоративный владелец не нужен.

Два телефона в одном помещении — без Wi-Fi, без мобильных данных — могут обнаружить друг друга,
обменяться медиафайлами, транслировать прямое видео и социально взаимодействовать через BLE, Wi-Fi Direct, NearLink,
LoRa или HTTP-ретрансляцию.

---

## Зачем нужен Aether Media?

**Транслируйте живой концерт на телефоны зрителей — без интернета, без CDN, без оплаты стриминга.**

Устройство исполнителя транслирует по Wi-Fi Direct. Каждый телефон в зоне доступности получает поток и ретранслирует его дальше по BLE. Реакции (лайки, супер-реакции, комментарии к точной позиции воспроизведения) возвращаются тем же путём. Никакой буферизации с дата-центра в 10 000 км. Аккаунт для аудитории не нужен.

```
  [Performer] ──WiFi Direct──▶ [Row 1] ──BLE──▶ [Row 2] ──NearLink──▶ [Row 3]
                 1080p live           relayed, encrypted        relayed, encrypted
```

**Подписывайтесь на офлайн-автора. Получайте его контент, когда он снова окажется в зоне доступности.**

Подписки доставляются через уровень DTN (сети с задержкой и прерываниями) Aether с хранением и пересылкой. Если устройство автора недоступно прямо сейчас, намерение подписаться ждёт — до 72 часов — и доставляется, как только открывается маршрут. Без инфраструктуры push-уведомлений, без сервера приложений.

**Смотрите фильм вместе через сеть.**

У кого-то в сети есть файл. Координатор Watch Party синхронизирует воспроизведение, паузу и перемотку на всех устройствах с компенсацией RTT. Реакции срабатывают в реальном времени точно на метке времени видео. Если устройство хоста уходит офлайн в середине фильма, сеанс автоматически переходит к следующему доступному пиру.

---

### Сравнение с существующими проигрывателями и сетями

| Возможность | VLC | Spotify | Streambert | Aether Media |
|---|:---:|:---:|:---:|:---:|
| Работает офлайн (без интернета) | Только воспроизведение | Нет | Нет | **Да — обнаружение, трансляция, реакции** |
| Работает без аккаунта в приложении | Да | Нет | Нет | **Да** |
| Mesh-ретрансляция (переход через устройства) | Нет | Нет | Нет | **Да** |
| Прямая трансляция без CDN | Нет | Нет | Частично | **Да — BLE / Wi-Fi Direct / NearLink** |
| Социальные реакции на таймлайне | Нет | Нет | Нет | **Да — на точной позиции воспроизведения** |
| Граф подписок без сервера | Нет | Нет | Нет | **Да — через DTN** |
| Задержка менее секунды в одном помещении | Н/Д | Нет | Нет | **Да — NearLink 20 мкс** |
| Работает на микроконтроллере / C11 | Нет | Нет | Нет | **Да — реализация на C** |
| SDK для 8 языков | Нет | Нет | Нет | **Да** |

---

## Как это работает

**Шаг 1 — Обнаружение в сети.** Устройства транслируют свой Aether Tag (короткий идентификатор, например `@sam.5jk2`) через BLE-объявления и зондирующие ответы Wi-Fi Direct. Ни IP-адрес, ни аккаунт не нужны. Транспортный уровень автоматически переключается на NearLink (600 м, 12 Мбит/с, 20 мкс) на устройствах с его поддержкой, использует Wi-Fi Direct (200 м, 250 Мбит/с) как запасной, затем BLE (100 м, 1 Мбит/с), потом LoRa-over-BLE (1,3 км) и наконец HTTP-ретрансляцию как крайний вариант.

**Шаг 2 — Адресация контента.** Каждый медиафайл идентифицируется своим хэшем содержимого SHA-256 — не URL и не путём к серверу. `ContentDescriptor` (хэш + имя + MIME-тип + манифест чанков) транслируется по сети. Любое устройство, у которого есть файл, может обслуживать чанки для любого нуждающегося в них устройства. Нет исходного сервера. Файлы могут собираться из фрагментов, хранящихся у разных пиров одновременно, в стиле BitTorrent.

**Шаг 3 — Социальный уровень.** Подписки, реакции и обновления профиля кодируются как подписанные JSON-нагрузки и отправляются либо как DTN-пакеты (для доставки с допустимостью офлайна), либо как `MeshPacket` с максимальными усилиями (для реакций с малой задержкой во время прямых трансляций). `SocialGraph` отслеживает подписки. `FeedAggregator` прослушивает пакеты `StreamAnnounce` и `ContentAnnounce` от авторов, на которых подписаны, и собирает хронологическую ленту — полностью из mesh-событий, без сервера лент.

---

## Используемые возможности Aether

Aether Media построен поверх [aether-protocol](https://github.com/bhengubv/aether-protocol) и использует следующие интерфейсы:

| Интерфейс Aether | Пакет | Как Aether Media использует его |
|---|---|---|
| `ITransportService` | `Aether.Transport` | Отправляет закодированные аудио/видеофреймы, реакции и намерения подписки по сети (BLE / Wi-Fi Direct / NearLink / LoRa / HTTP-ретрансляция) |
| `IStreamingService` | `Aether.Streaming` | Транслирует `StreamAnnounce` при начале прямого эфира; `FeedAggregator` подписывается на события `StreamAnnounced` и `StreamEnded` для поддержки ленты прямых трансляций |
| `IContentService` | `Aether.Content` | Публикует `ContentDescriptor` для загруженных медиафайлов; `FeedAggregator` подписывается на `ContentAnnounced` для обнаружения VOD |
| `IDtnService` | `Aether.Dtn` | Надёжно доставляет намерения подписки офлайн-авторам; пакеты ждут маршрута до 72 ч |
| `IMeshSender` | `Aether.Messaging` | Отправляет пакеты отписки и реакции прямых трансляций по сети без DTN-накладных расходов |
| `IRoutingService` | `Aether.Routing` | Маршрутно-осведомлённая доставка социальных пакетов; AODV-стиль RREQ/RREP с подписанными маршрутными ответами Ed25519 |
| `SignalProtocolService` | `Aether.Security` | Сквозное шифрование прямых сообщений, нагрузок синхронизации профиля и контента приватных каналов с X3DH + Double Ratchet |
| `IAdaptiveBitrateController` | `Aether.Streaming` | Выбирает максимально устойчивую ступень качества (H.264 / H.265 / VP8) на основе оценок пропускной способности от активного транспорта |

---

## SDK для 8 языков

Aether Media поставляется с реализациями на 8 языках, чтобы работать на каждой платформе экосистемы.

| Язык | Каталог | Платформа | Медиадвижок | Роль |
|---|---|---|---|---|
| C# (.NET 10) | `src/` | Windows · Linux · macOS | LibVLC (LibVLCSharp) | Эталонная реализация, полный DI, пакеты NuGet |
| TypeScript | `typescript/` | Браузер · Node 20 | HLS.js · Shaka Player | Веб-проигрыватель, клиент лент, социальный SDK |
| Python | `python/` | Любой Python 3.11+ | mutagen (метаданные) | Движок плагинов, скриптинг, обработка метаданных |
| Rust | `rust/` | Любая цель Rust | `rodio` (аудио) | Высокопроизводительный движок лент, бенчмарки |
| Go | `go/` | Любая цель Go 1.22 | — | Библиотека социального графа |
| Kotlin | `kotlin/` | JVM 21 · Android | media3 / ExoPlayer (Android) | Android-проигрыватель; социальный граф JVM для серверного использования |
| Swift | `swift/` | macOS 13+ · iOS 16+ | AVFoundation | Проигрыватель для платформ Apple |
| C | `c/` | Любая цель C11 | — | Встроенные / микроконтроллерные модели лент и социального графа |

Все 8 реализаций используют тот же формат проводного протокола, что и `aether-protocol`, и производят
совместимые социальные пакеты, верифицированные кросс-языковыми фикстурами в CI.

---

## Быстрый старт

### C# Desktop (Windows / Linux / macOS)

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media
dotnet run --project samples/Aether.Media.Demo.Console
```

Регистрация всех подсистем:

```csharp
services.AddAetherMedia(media =>
    media.AddIdentity()
         .AddContent()
         .AddSocial()
         .AddStreaming()
         .AddAI());
```

Получение и использование:

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

### TypeScript (Браузер)

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

Клиент лент с кешем локального хранилища:

```typescript
import { FeedClient } from '@bhengubv/aether-media';

const client = new FeedClient('https://relay.aether.network/media');
const items  = await client.getFeed(20, 0);   // limit, offset

for (const item of items) {
    console.log(item.content.title, item.likeCount);
}

await client.markWatched('a3f9...', 45_000);  // contentHash, ms watched
```

### Python (Плагин)

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

### Kotlin (Android / JVM)

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

## Социальный протокол

Социальный уровень не имеет сервера. Каждая подписка, отписка, объявление контента и
реакция — это подписанный `MeshPacket` или `DtnBundle`, путешествующий через доступное радио.

**Подписки** оборачиваются в `FollowIntentPayload` (UTF-8 JSON), шифруются ключом сеанса Signal Protocol
целевого автора (X3DH + Double Ratchet) и фиксируются как `DtnBundle`, адресованный целевому UHID. Уровень DTN хранит пакет локально
и доставляет его по сети, как только открывается путь к цели — даже если это занимает
часы. Устройство автора получает пакет, проверяет подпись и увеличивает
счётчик подписчиков. Всё это происходит без ведома какого-либо центрального сервера о связи.

**Объявления контента** — это пакеты `ContentDescriptor`, транслируемые публикующим
устройством. Каждое устройство, получившее дескриптор, кешируеет его и ретранслирует соседним
пирам (сетевой флуд с дедупликацией). `FeedAggregator` на каждом устройстве прослушивает
эти трансляции и добавляет новый контент от отслеживаемых авторов в локальную ленту.

**Реакции** (лайк, репост, супер-реакция, комментарий) несут хэш контента, тип реакции
и точную позицию воспроизведения в миллисекундах. Они путешествуют как `MeshPacket`
с максимальными усилиями, адресованный UHID автора — маршрутизируется AODV с подписанными маршрутными
ответами, поэтому ни один поддельный адресат не может их перехватить. Во время прямой трансляции реакции
агрегируются и отображаются в реальном времени на устройстве издателя, не покидая сеть.

**Синхронизация профиля** использует тот же DTN-механизм, что и подписки. Когда автор обновляет
отображаемое имя, аватар или биографию, новый `MediaProfile` подписывается его ключом идентификации
Ed25519, сериализуется и транслируется как DTN-пакет. Любое устройство, получившее его — напрямую
или через ретрансляцию — проверяет подпись и обновляет локальный кеш. Обновление
профиля, сделанное офлайн, достигает подписчиков в следующий раз, когда любой из них окажется
в радиусе действия.

---

## Структура репозитория

```
aether-media/
  src/
    Aether.Media.Core/            Доменные модели и интерфейсы (MediaContent, IMediaLibrary и др.)
    Aether.Media.Identity/        Управление профилем, аватар, синхронизация профиля
    Aether.Media.Content/         Сканер медиабиблиотеки, резолвер метаданных, LRU-кеш, миниатюры
    Aether.Media.Social/          SocialGraph, FeedAggregator, ReactionService, DiscoveryService
    Aether.Media.Streaming/       LiveStreamPublisher, WatchPartyCoordinator, AbrController
    Aether.Media.AI/              ContentRanker, ContentModerator, CreatorReputationView
    Aether.Media.DependencyInjection/  Расширение AddAetherMedia() + fluent API AetherMediaBuilder
    Aether.Media.Desktop/         Интеграция LibVLCSharp для Windows / Linux / macOS
  samples/
    Aether.Media.Demo.Console/    Интерактивная консольная демонстрация всех подсистем
    Aether.Media.RelayTest/       Тест кругового обхода HTTP-ретрансляции (требует Aether.RelayServer)
  tests/
    Aether.Media.Core.Tests/      Модульные тесты доменных моделей и InMemoryMediaLibrary
    Aether.Media.Social.Tests/    Модульные тесты SocialGraph и FeedAggregator
  typescript/                     TypeScript веб-проигрыватель и социальный SDK (@bhengubv/aether-media)
    src/
      player/   AetherMediaPlayer (HLS.js + Shaka Player + native MSE)
      social/   FeedClient, ReactionClient
      identity/ ProfileClient
      streaming/ AetherStreamClient
      models/   TypeScript-зеркала доменных моделей C#
  python/                         Python движок плагинов и библиотека метаданных (aether-media на PyPI)
    aether_media/
      plugins/  Базовый класс AetherMediaPlugin, PluginHost
      metadata/ Читатель/писатель тегов (обёртка mutagen)
      cli/      Точки входа командной строки
  rust/                           Rust движок лент (aether-media на crates.io)
    src/
      feed/     FeedStore, FeedEntry
      social/   SocialGraph, follow/unfollow
      streaming/ StreamAnnounce, модели сегментов
      player/   Воспроизведение аудио через rodio
  go/                             Go библиотека социального графа (github.com/bhengubv/aether-media/go)
    social/     SocialGraph
    player/     Модели проигрывателя
    feed/       Модели лент
    streaming/  Модели потоковой передачи
  kotlin/                         Kotlin/JVM социальный граф + интеграция Android ExoPlayer
    src/main/kotlin/
      social/   SocialGraph (на основе ConcurrentHashMap, JVM и Android)
      feed/     Модели лент
      player/   Интеграция ExoPlayer (Android; JVM-тесты используют только ядро)
      content/  Модели дескрипторов контента
      streaming/ Модели сессий потоковой передачи
    android/    Gradle Android-модуль с зависимостью media3-exoplayer
  swift/                          Swift / проигрыватель для платформ Apple (пакет SwiftPM)
    Sources/AetherMedia/
      social/   SocialGraph (на основе actor, Swift Concurrency)
      player/   Проигрыватель AVFoundation
      feed/     Модели лент
      streaming/ Модели потоковой передачи
  c/                              C11 модели лент и социального графа для встроенных целей
    include/aether_media/         Публичные заголовки
    src/                          Реализации
    tests/                        Набор тестов на основе CTest
  android/                        Gradle-модули Android
    media/      Основной медиамодуль (Kotlin + Jetpack)
    media-tv/   Вариант для Android TV
  docs/                           Примечания по архитектуре и проектные решения
```

---

## Сборка

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

## Лицензия

MIT — бесплатно навсегда. Кодировочный движок (LibVLC / FFmpeg / AVFoundation / media3 / ExoPlayer)
используется по лицензиям LGPL и Apache 2.0 соответственно, без изменений. См. [LICENSE](LICENSE).
