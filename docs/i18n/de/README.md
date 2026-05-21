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

> Ein dezentrales soziales Mediennetzwerk und Player, aufgebaut auf dem Aether-Mesh-Protokoll.
> Kein Internet erforderlich. Kein zentraler Server. Kein Unternehmensinhaber.

Zwei Telefone im selben Raum — kein WLAN, keine mobilen Daten — können sich gegenseitig
entdecken, Medien teilen, Live-Video streamen und sozial interagieren über BLE, Wi-Fi Direct, NearLink,
LoRa oder HTTP-Relay.

---

## Warum Aether Media?

**Ein Live-Konzert auf die Telefone des Publikums streamen — kein Internet, kein CDN, keine Streaming-Gebühr.**

Das Gerät des Performers sendet über Wi-Fi Direct. Jedes Telefon in Reichweite empfängt den Stream und leitet ihn weiter über BLE. Reaktionen (Likes, Super-Reacts, Kommentare an der genauen Wiedergabeposition) reisen auf demselben Weg zurück. Keine Pufferung von einem Rechenzentrum 10.000 km entfernt. Kein Konto für das Publikum erforderlich.

```
  [Performer] ──WiFi Direct──▶ [Reihe 1] ──BLE──▶ [Reihe 2] ──NearLink──▶ [Reihe 3]
                 1080p live              weitergeleitet, verschlüsselt   weitergeleitet, verschlüsselt
```

**Einem Creator folgen, der offline ist. Seinen Content empfangen, wenn er wieder in Reichweite kommt.**

Follows werden über die DTN-Schicht (Delay-Tolerant Networking) von Aether mit Store-and-Forward übermittelt. Wenn das Gerät des Creators jetzt nicht erreichbar ist, wartet die Follow-Absicht — bis zu 72 Stunden — und wird zugestellt, sobald eine Route öffnet. Keine Push-Benachrichtigungs-Infrastruktur, kein App-Server.

**Gemeinsam einen Film über das Mesh ansehen.**

Jemand im Mesh hat die Datei. Der Watch-Party-Koordinator synchronisiert Play, Pause und Such-Position auf jedem Gerät mit RTT-Kompensation. Reaktionen erfolgen in Echtzeit am genauen Zeitstempel im Video. Wenn das Gerät des Hosts während des Films offline geht, migriert die Sitzung automatisch zum nächsten verfügbaren Peer.

---

### Vergleich mit vorhandenen Playern und Netzwerken

| Fähigkeit | VLC | Spotify | Streambert | Aether Media |
|---|:---:|:---:|:---:|:---:|
| Funktioniert offline (kein Internet) | Nur Wiedergabe | Nein | Nein | **Ja — entdecken, streamen, reagieren** |
| Funktioniert ohne App-Konto | Ja | Nein | Nein | **Ja** |
| Mesh-Relay (über Geräte weiterleiten) | Nein | Nein | Nein | **Ja** |
| Live-Streaming, kein CDN | Nein | Nein | Teilweise | **Ja — BLE / Wi-Fi Direct / NearLink** |
| Soziale Reaktionen auf Zeitachse | Nein | Nein | Nein | **Ja — an genauer Wiedergabeposition** |
| Follow-Graph ohne Server | Nein | Nein | Nein | **Ja — DTN-zugestellt** |
| Untersekundenlatenz im selben Raum | N/A | Nein | Nein | **Ja — NearLink 20 µs** |
| Läuft auf Mikrocontroller / C11 | Nein | Nein | Nein | **Ja — C-Implementierung** |
| 8-Sprachen-SDK | Nein | Nein | Nein | **Ja** |

---

## Wie es funktioniert

**Schritt 1 — Mesh-Erkennung.** Geräte senden ihren Aether-Tag (ein kurzes Identitätskürzel wie `@sam.5jk2`) über BLE-Werbungen und Wi-Fi-Direct-Probe-Antworten. Keine IP-Adresse oder Konto erforderlich. Die Transport-Schicht befördert NearLink (600 m, 12 Mbps, 20 µs) automatisch auf Geräten, die es haben, fällt auf Wi-Fi Direct (200 m, 250 Mbps) zurück, dann BLE (100 m, 1 Mbps), dann LoRa-over-BLE (1,3 km) und schließlich HTTP-Relay als letztes Mittel.

**Schritt 2 — Inhaltsadressierung.** Jedes Medienstück wird durch seinen SHA-256-Inhalts-Hash identifiziert — nicht durch eine URL oder einen Server-Pfad. Ein `ContentDescriptor` (Hash + Name + MIME-Typ + Chunk-Manifest) wird über das Mesh gesendet. Jedes Gerät, das die Datei hat, kann Chunks an jedes Gerät liefern, das sie benötigt. Es gibt keinen Ursprungsserver. Dateien können aus Fragmenten zusammengesetzt werden, die gleichzeitig von verschiedenen Peers gehalten werden — nach dem BitTorrent-Prinzip.

**Schritt 3 — Soziale Schicht.** Follows, Reaktionen und Profil-Updates werden als signierte JSON-Nutzdaten kodiert und entweder als DTN-Bundles (für offline-tolerante Zustellung) oder als Best-Effort-`MeshPacket`s (für reaktionsarme Reaktionen während Live-Streams) gesendet. Der `SocialGraph` verfolgt, wem Sie folgen. Der `FeedAggregator` lauscht auf `StreamAnnounce`- und `ContentAnnounce`-Pakete von gefolgten Creatorn und stellt einen chronologischen Feed zusammen — vollständig aus Mesh-Ereignissen, ohne Feed-Server.

---

## Welche Aether-Features werden verwendet

Aether Media ist aufgebaut auf [aether-protocol](https://github.com/bhengubv/aether-protocol) und nutzt folgende Schnittstellen:

| Aether-Schnittstelle | Paket | Wie Aether Media sie nutzt |
|---|---|---|
| `ITransportService` | `Aether.Transport` | Sendet kodierte Video-/Audioframes, Reaktionen und Follow-Absichten über das Mesh (BLE / Wi-Fi Direct / NearLink / LoRa / HTTP-Relay) |
| `IStreamingService` | `Aether.Streaming` | Sendet `StreamAnnounce` beim Go-Live; `FeedAggregator` abonniert `StreamAnnounced`- und `StreamEnded`-Ereignisse zur Pflege des Live-Stream-Feeds |
| `IContentService` | `Aether.Content` | Veröffentlicht `ContentDescriptor`s für hochgeladene Medien; `FeedAggregator` abonniert `ContentAnnounced` für VOD-Erkennung |
| `IDtnService` | `Aether.Dtn` | Stellt Follow-Absichten dauerhaft an offline-Creator zu; Bundles warten bis zu 72 Stunden auf eine Route |
| `IMeshSender` | `Aether.Messaging` | Sendet Best-Effort-Unfollow-Pakete und Live-Reaktionen über das Mesh ohne DTN-Overhead |
| `IRoutingService` | `Aether.Routing` | Routenaware Zustellung von sozialen Paketen; AODV-artiges RREQ/RREP mit Ed25519-signierten Routen-Antworten |
| `SignalProtocolService` | `Aether.Security` | Ende-zu-Ende-verschlüsselt Direktnachrichten, Profil-Sync-Nutzdaten und privaten Kanalinhalt mit X3DH + Double Ratchet |
| `IAdaptiveBitrateController` | `Aether.Streaming` | Wählt die höchste nachhaltige Qualitätsstufe (H.264 / H.265 / VP8) basierend auf Live-Bandbreiten-Schätzungen vom aktiven Transport |

---

## 8-Sprachen-SDK

Aether Media wird in 8 Sprachen implementiert, damit es auf jeder Plattform im Ökosystem läuft.

| Sprache | Verzeichnis | Plattform | Media-Engine | Rolle |
|---|---|---|---|---|
| C# (.NET 10) | `src/` | Windows · Linux · macOS | LibVLC (LibVLCSharp) | Referenzimplementierung, vollständiges DI, NuGet-Pakete |
| TypeScript | `typescript/` | Browser · Node 20 | HLS.js · Shaka Player | Web-Player, Feed-Client, soziales SDK |
| Python | `python/` | Jedes Python 3.11+ | mutagen (Metadaten) | Plugin-Engine, Scripting, Metadatenverarbeitung |
| Rust | `rust/` | Jedes Rust-Ziel | `rodio` (Audio) | Hochleistungs-Feed-Engine, Benchmarks |
| Go | `go/` | Jedes Go-1.22-Ziel | — | Social-Graph-Bibliothek |
| Kotlin | `kotlin/` | JVM 21 · Android | media3 / ExoPlayer (Android) | Android-Player; JVM-Social-Graph für serverseitigen Einsatz |
| Swift | `swift/` | macOS 13+ · iOS 16+ | AVFoundation | Apple-Plattform-Player |
| C | `c/` | Jedes C11-Ziel | — | Eingebetteter / Mikrocontroller-Feed und soziale Modelle |

Alle 8 Implementierungen teilen dasselbe Leitungsformat wie `aether-protocol` und erzeugen
interoperable soziale Pakete, die durch sprachübergreifende Fixtures in CI verifiziert werden.

---

## Schnellstart

### C# Desktop (Windows / Linux / macOS)

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media
dotnet run --project samples/Aether.Media.Demo.Console
```

Alle Subsysteme registrieren:

```csharp
services.AddAetherMedia(media =>
    media.AddIdentity()
         .AddContent()
         .AddSocial()
         .AddStreaming()
         .AddAI());
```

Auflösen und verwenden:

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

### TypeScript (Browser)

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

Feed-Client mit lokalem Speicher-Cache:

```typescript
import { FeedClient } from '@bhengubv/aether-media';

const client = new FeedClient('https://relay.aether.network/media');
const items  = await client.getFeed(20, 0);   // limit, offset

for (const item of items) {
    console.log(item.content.title, item.likeCount);
}

await client.markWatched('a3f9...', 45_000);  // contentHash, ms watched
```

### Python (Plugin)

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

## Soziales Protokoll

Die soziale Schicht hat keinen Server. Jedes Follow, Unfollow, jede Inhaltsankündigung und
Reaktion ist ein signiertes `MeshPacket` oder `DtnBundle`, das über welches Radio auch
immer verfügbar ist reist.

**Follows** werden in eine `FollowIntentPayload` (UTF-8 JSON) verpackt, mit dem Signal-Protokoll-Sitzungsschlüssel des Ziel-Creators (X3DH + Double Ratchet) verschlüsselt und als `DtnBundle` adressiert an die Ziel-UHID eingetragen. Die DTN-Schicht speichert das Bundle lokal und stellt es über das Mesh zu, sobald sich ein Pfad zum Ziel öffnet — auch wenn das Stunden dauert. Das Gerät des Creators empfängt das Bundle, verifiziert die Signatur und erhöht die Follower-Anzahl. All dies geschieht ohne dass ein zentraler Server von der Beziehung weiß.

**Inhaltsankündigungen** sind `ContentDescriptor`-Pakete, die vom veröffentlichenden
Gerät gesendet werden. Jedes Gerät, das den Descriptor empfängt, cacht ihn und sendet ihn
an benachbarte Peers weiter (Mesh-Flood mit Dedup). Der `FeedAggregator` auf jedem Gerät
lauscht auf diese Sendungen und zeigt neue Inhalte von gefolgten Creatorn im lokalen Feed an.

**Reaktionen** (Like, Teilen, Super-React, Kommentar) tragen den Inhalts-Hash, den Reaktionstyp
und die genaue Wiedergabeposition in Millisekunden. Sie reisen als Best-Effort-`MeshPacket`s
adressiert an die UHID des Creators — geroutet durch AODV mit signierten Routen-Antworten,
sodass kein gefälschtes Ziel sie abfangen kann. Während eines Live-Streams werden Reaktionen
aggregiert und in Echtzeit auf dem Gerät des Herausgebers angezeigt, ohne das Mesh zu verlassen.

**Profil-Sync** verwendet denselben DTN-Mechanismus wie Follows. Wenn ein Creator seinen
Anzeigenamen, Avatar oder Bio aktualisiert, wird das neue `MediaProfile` mit seinem Ed25519-
Identitätsschlüssel signiert, serialisiert und als DTN-Bundle gesendet. Jedes Gerät, das
es empfängt — direkt oder über Relay — verifiziert die Signatur und aktualisiert seinen
lokalen Cache. Eine offline vorgenommene Profilaktualisierung erreicht Follower das nächste
Mal, wenn eines von ihnen in Funkreichweite kommt.

---

## Repository-Struktur

```
aether-media/
  src/
    Aether.Media.Core/            Domänenmodelle und Schnittstellen (MediaContent, IMediaLibrary usw.)
    Aether.Media.Identity/        Profilverwaltung, Avatar, Profil-Sync
    Aether.Media.Content/         Medienbibliotheks-Scanner, Metadaten-Resolver, LRU-Cache, Vorschaubilder
    Aether.Media.Social/          SocialGraph, FeedAggregator, ReactionService, DiscoveryService
    Aether.Media.Streaming/       LiveStreamPublisher, WatchPartyCoordinator, AbrController
    Aether.Media.AI/              ContentRanker, ContentModerator, CreatorReputationView
    Aether.Media.DependencyInjection/  AddAetherMedia()-Erweiterung + AetherMediaBuilder Fluent-API
    Aether.Media.Desktop/         LibVLCSharp-Integration für Windows / Linux / macOS
  samples/
    Aether.Media.Demo.Console/    Interaktive Konsoldemo mit allen Subsystemen
    Aether.Media.RelayTest/       HTTP-Relay-Hin-und-Rücklauf-Test (erfordert Aether.RelayServer)
  tests/
    Aether.Media.Core.Tests/      Unit-Tests für Domänenmodelle und InMemoryMediaLibrary
    Aether.Media.Social.Tests/    Unit-Tests für SocialGraph und FeedAggregator
  typescript/                     TypeScript-Web-Player und soziales SDK (@bhengubv/aether-media)
    src/
      player/   AetherMediaPlayer (HLS.js + Shaka Player + natives MSE)
      social/   FeedClient, ReactionClient
      identity/ ProfileClient
      streaming/ AetherStreamClient
      models/   TypeScript-Spiegelungen der C#-Domänenmodelle
  python/                         Python-Plugin-Engine und Metadatenbibliothek (aether-media auf PyPI)
    aether_media/
      plugins/  AetherMediaPlugin-Basisklasse, PluginHost
      metadata/ Tag-Leser/-Schreiber (mutagen-Wrapper)
      cli/      Kommandozeilen-Einstiegspunkte
  rust/                           Rust-Feed-Engine (aether-media auf crates.io)
    src/
      feed/     FeedStore, FeedEntry
      social/   SocialGraph, follow/unfollow
      streaming/ StreamAnnounce, Segmentmodelle
      player/   Audio-Wiedergabe via rodio
  go/                             Go-Social-Graph-Bibliothek (github.com/bhengubv/aether-media/go)
    social/     SocialGraph
    player/     Player-Modelle
    feed/       Feed-Modelle
    streaming/  Stream-Modelle
  kotlin/                         Kotlin/JVM-Social-Graph + Android-ExoPlayer-Integration
    src/main/kotlin/
      social/   SocialGraph (ConcurrentHashMap-backed, JVM und Android)
      feed/     Feed-Modelle
      player/   ExoPlayer-Integration (Android; JVM-Tests verwenden nur Core)
      content/  Inhalts-Descriptor-Modelle
      streaming/ Stream-Sitzungsmodelle
    android/    Gradle-Android-Modul mit media3-exoplayer-Abhängigkeit
  swift/                          Swift / Apple-Plattform-Player (SwiftPM-Paket)
    Sources/AetherMedia/
      social/   SocialGraph (actor-basiert, Swift Concurrency)
      player/   AVFoundation-Player
      feed/     Feed-Modelle
      streaming/ Stream-Modelle
  c/                              C11-Feed- und Sozialmodelle für eingebettete Ziele
    include/aether_media/         Öffentliche Header
    src/                          Implementierungen
    tests/                        CTest-basierte Testsuite
  android/                        Android-Gradle-Module
    media/      Haupt-Media-Modul (Kotlin + Jetpack)
    media-tv/   Android-TV-Variante
  docs/                           Architekturnotizen und Designentscheidungen
```

---

## Erstellen

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

## Lizenz

MIT — für immer kostenlos. Die Codec-Engine (LibVLC / FFmpeg / AVFoundation / media3 / ExoPlayer)
wird unverändert über LGPL bzw. Apache 2.0 verwendet. Siehe [LICENSE](LICENSE).
