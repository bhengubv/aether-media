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

> Una red social descentralizada y un reproductor construidos sobre el protocolo de malla Aether.
> No se requiere internet. Sin servidor central. Sin propietario corporativo.

Dos teléfonos en la misma habitación — sin Wi-Fi, sin datos móviles — pueden descubrirse mutuamente,
compartir medios, transmitir video en vivo y reaccionar socialmente mediante BLE, Wi-Fi Direct, NearLink,
LoRa o relay HTTP.

---

## ¿Por qué Aether Media?

**Transmite un concierto en vivo a los teléfonos del público — sin internet, sin CDN, sin tarifa de streaming.**

El dispositivo del artista transmite por Wi-Fi Direct. Cada teléfono en rango recibe el stream y lo retransmite más lejos por BLE. Las reacciones (likes, super-reacciones, comentarios en la posición exacta de reproducción) viajan de vuelta por el mismo camino. Sin buffering desde un centro de datos a 10,000 km de distancia. Sin cuenta requerida para el público.

```
  [Artista] ──WiFi Direct──▶ [Fila 1] ──BLE──▶ [Fila 2] ──NearLink──▶ [Fila 3]
                 1080p en vivo           retransmitido, cifrado        retransmitido, cifrado
```

**Sigue a un creador que está sin conexión. Recibe su contenido cuando vuelva a estar en rango.**

Los seguimientos se entregan a través de la capa DTN (Redes Tolerantes a Demoras) store-and-forward de Aether. Si el dispositivo del creador no es accesible ahora, la intención de seguimiento espera — hasta 72 horas — y se entrega en el momento en que se abre una ruta. Sin infraestructura de notificaciones push, sin servidor de aplicaciones.

**Ve una película juntos a través de la malla.**

Alguien en la malla tiene el archivo. El coordinador de Watch Party sincroniza play, pausa y avance en todos los dispositivos con compensación RTT. Las reacciones se disparan en tiempo real en la marca de tiempo exacta del video. Si el dispositivo del anfitrión se desconecta a mitad de la película, la sesión migra automáticamente al siguiente peer disponible.

---

### Comparación con reproductores y redes existentes

| Capacidad | VLC | Spotify | Streambert | Aether Media |
|---|:---:|:---:|:---:|:---:|
| Funciona sin conexión (sin internet) | Solo reproducción | No | No | **Sí — descubrir, transmitir, reaccionar** |
| Funciona sin cuenta de aplicación | Sí | No | No | **Sí** |
| Relay de malla (saltar por dispositivos) | No | No | No | **Sí** |
| Streaming en vivo, sin CDN | No | No | Parcial | **Sí — BLE / Wi-Fi Direct / NearLink** |
| Reacciones sociales en la línea de tiempo | No | No | No | **Sí — en la posición exacta de reproducción** |
| Grafo de seguimiento sin servidor | No | No | No | **Sí — entregado por DTN** |
| Latencia inferior a un segundo en la misma sala | N/A | No | No | **Sí — NearLink 20 µs** |
| Se ejecuta en microcontrolador / C11 | No | No | No | **Sí — implementación en C** |
| SDK de 8 lenguajes | No | No | No | **Sí** |

---

## Cómo Funciona

**Paso 1 — Descubrimiento en la malla.** Los dispositivos difunden su Aether Tag (un identificador de identidad corto como `@sam.5jk2`) a través de anuncios BLE y respuestas de sonda Wi-Fi Direct. No se necesita dirección IP ni cuenta. La capa de transporte promueve automáticamente NearLink (600 m, 12 Mbps, 20 µs) en los dispositivos que lo tienen, recurre a Wi-Fi Direct (200 m, 250 Mbps), luego BLE (100 m, 1 Mbps), luego LoRa-over-BLE (1.3 km), y finalmente relay HTTP como último recurso.

**Paso 2 — Direccionamiento de contenido.** Cada pieza de media se identifica por su hash de contenido SHA-256 — no por una URL ni una ruta de servidor. Un `ContentDescriptor` (hash + nombre + tipo MIME + manifiesto de fragmentos) se difunde sobre la malla. Cualquier dispositivo que tenga el archivo puede servir fragmentos a cualquier dispositivo que los necesite. No hay servidor de origen. Los archivos pueden ensamblarse a partir de fragmentos en poder de diferentes peers simultáneamente, al estilo BitTorrent.

**Paso 3 — Capa social.** Los seguimientos, reacciones y actualizaciones de perfil se codifican como payloads JSON firmados y se envían como bundles DTN (para entrega tolerante a desconexiones) o como `MeshPacket`s de mejor esfuerzo (para reacciones de baja latencia durante transmisiones en vivo). El `SocialGraph` rastrea a quién sigues. El `FeedAggregator` escucha paquetes `StreamAnnounce` y `ContentAnnounce` de los creadores seguidos y ensambla un feed cronológico — completamente a partir de eventos de malla, sin servidor de feed.

---

## Qué Funcionalidades de Aether se Utilizan

Aether Media está construido sobre [aether-protocol](https://github.com/bhengubv/aether-protocol) y usa estas interfaces:

| Interfaz Aether | Paquete | Cómo Aether Media la usa |
|---|---|---|
| `ITransportService` | `Aether.Transport` | Envía frames de video/audio codificados, reacciones e intenciones de seguimiento sobre la malla (BLE / Wi-Fi Direct / NearLink / LoRa / relay HTTP) |
| `IStreamingService` | `Aether.Streaming` | Difunde `StreamAnnounce` al iniciar en vivo; `FeedAggregator` se suscribe a eventos `StreamAnnounced` y `StreamEnded` para mantener el feed de transmisión en vivo |
| `IContentService` | `Aether.Content` | Publica `ContentDescriptor`s para media subida; `FeedAggregator` se suscribe a `ContentAnnounced` para descubrimiento VOD |
| `IDtnService` | `Aether.Dtn` | Entrega intenciones de seguimiento de forma duradera a creadores sin conexión; los bundles esperan hasta 72 h para una ruta |
| `IMeshSender` | `Aether.Messaging` | Envía paquetes de dejar de seguir y reacciones en vivo sobre la malla sin sobrecarga DTN |
| `IRoutingService` | `Aether.Routing` | Entrega de paquetes sociales con conocimiento de ruta; RREQ/RREP estilo AODV con respuestas de ruta firmadas Ed25519 |
| `SignalProtocolService` | `Aether.Security` | Cifra de extremo a extremo los mensajes directos, payloads de sincronización de perfil y contenido de canales privados con X3DH + Double Ratchet |
| `IAdaptiveBitrateController` | `Aether.Streaming` | Selecciona el peldaño de calidad más alto sostenible (H.264 / H.265 / VP8) basado en estimaciones de ancho de banda en vivo del transporte activo |

---

## SDK de 8 Lenguajes

Aether Media incluye implementaciones en 8 lenguajes para que funcione en todas las plataformas del ecosistema.

| Lenguaje | Directorio | Plataforma | Motor multimedia | Rol |
|---|---|---|---|---|
| C# (.NET 10) | `src/` | Windows · Linux · macOS | LibVLC (LibVLCSharp) | Implementación de referencia, DI completo, paquetes NuGet |
| TypeScript | `typescript/` | Navegador · Node 20 | HLS.js · Shaka Player | Reproductor web, cliente de feed, SDK social |
| Python | `python/` | Cualquier Python 3.11+ | mutagen (metadatos) | Motor de plugins, scripting, procesamiento de metadatos |
| Rust | `rust/` | Cualquier objetivo Rust | `rodio` (audio) | Motor de feed de alto rendimiento, benchmarks |
| Go | `go/` | Cualquier objetivo Go 1.22 | — | Biblioteca de grafo social |
| Kotlin | `kotlin/` | JVM 21 · Android | media3 / ExoPlayer (Android) | Reproductor Android; grafo social JVM para uso del lado servidor |
| Swift | `swift/` | macOS 13+ · iOS 16+ | AVFoundation | Reproductor para plataformas Apple |
| C | `c/` | Cualquier objetivo C11 | — | Modelos de feed y sociales integrados / microcontrolador |

Las 8 implementaciones comparten el mismo formato de cable que `aether-protocol` y producen
paquetes sociales interoperables verificados por fixtures multilenguaje en CI.

---

## Inicio Rápido

### C# Escritorio (Windows / Linux / macOS)

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media
dotnet run --project samples/Aether.Media.Demo.Console
```

Registra todos los subsistemas:

```csharp
services.AddAetherMedia(media =>
    media.AddIdentity()
         .AddContent()
         .AddSocial()
         .AddStreaming()
         .AddAI());
```

Resuelve y usa:

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

### TypeScript (Navegador)

```typescript
import { AetherMediaPlayer } from '@bhengubv/aether-media';

const video  = document.querySelector('video') as HTMLVideoElement;
const player = new AetherMediaPlayer(video);

// Carga un stream HLS publicado por un peer en la malla
await player.load('aether://stream/KXJB7-MN2P4');
await player.play();

// Alimenta segmentos de malla crudos directamente en el pipeline MSE
player.feedSegment(encodedBytes, 'video/mp4; codecs="avc1.42E01E"');
```

Cliente de feed con caché de almacenamiento local:

```typescript
import { FeedClient } from '@bhengubv/aether-media';

const client = new FeedClient('https://relay.aether.network/media');
const items  = await client.getFeed(20, 0);   // límite, desplazamiento

for (const item of items) {
    console.log(item.content.title, item.likeCount);
}

await client.markWatched('a3f9...', 45_000);  // contentHash, ms reproducidos
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

## Protocolo Social

La capa social no tiene servidor. Cada seguimiento, dejar de seguir, anuncio de contenido y
reacción es un `MeshPacket` o `DtnBundle` firmado que viaja por cualquier radio
disponible.

**Los seguimientos** se envuelven en un `FollowIntentPayload` (JSON UTF-8), se cifran con la
clave de sesión Signal Protocol del creador objetivo (X3DH + Double Ratchet) y se confirman
como un `DtnBundle` dirigido al UHID objetivo. La capa DTN almacena el bundle localmente
y lo entrega por la malla cuando se abre un camino hacia el objetivo — incluso si eso toma
horas. El dispositivo del creador recibe el bundle, verifica la firma e incrementa
su conteo de seguidores. Todo esto sucede sin que ningún servidor central conozca la
relación.

**Los anuncios de contenido** son paquetes `ContentDescriptor` difundidos por el dispositivo
publicador. Cada dispositivo que recibe el descriptor lo almacena en caché y lo redifunde a
peers cercanos (inundación de malla con deduplicación). El `FeedAggregator` de cada dispositivo escucha
estas difusiones y muestra nuevo contenido de los creadores seguidos en el feed local.

**Las reacciones** (like, compartir, super-reacción, comentario) llevan el hash del contenido, el tipo de
reacción y la posición exacta de reproducción en milisegundos. Viajan como `MeshPacket`s de mejor
esfuerzo dirigidos al UHID del creador — enrutados por AODV con respuestas de ruta firmadas, por lo que
ningún destino falso puede interceptarlos. Durante una transmisión en vivo, las reacciones
se agregan y se muestran en tiempo real en el dispositivo del publicador sin abandonar
la malla.

**La sincronización de perfil** usa el mismo mecanismo DTN que los seguimientos. Cuando un creador actualiza su
nombre de pantalla, avatar o biografía, el nuevo `MediaProfile` se firma con su clave de
identidad Ed25519, se serializa y se difunde como un bundle DTN. Cualquier dispositivo que lo reciba
— directamente o mediante relay — verifica la firma y actualiza su caché local. Una
actualización de perfil realizada sin conexión llega a los seguidores la próxima vez que cualquiera de ellos esté
dentro del rango de radio.

---

## Estructura del Repositorio

```
aether-media/
  src/
    Aether.Media.Core/            Modelos de dominio e interfaces (MediaContent, IMediaLibrary, etc.)
    Aether.Media.Identity/        Gestión de perfil, avatar, sincronización de perfil
    Aether.Media.Content/         Escáner de biblioteca multimedia, resolvedor de metadatos, caché LRU, miniaturas
    Aether.Media.Social/          SocialGraph, FeedAggregator, ReactionService, DiscoveryService
    Aether.Media.Streaming/       LiveStreamPublisher, WatchPartyCoordinator, AbrController
    Aether.Media.AI/              ContentRanker, ContentModerator, CreatorReputationView
    Aether.Media.DependencyInjection/  Extensión AddAetherMedia() + API fluida AetherMediaBuilder
    Aether.Media.Desktop/         Integración LibVLCSharp para Windows / Linux / macOS
  samples/
    Aether.Media.Demo.Console/    Demo interactiva de consola que muestra todos los subsistemas
    Aether.Media.RelayTest/       Prueba de viaje de ida y vuelta de relay HTTP (requiere Aether.RelayServer)
  tests/
    Aether.Media.Core.Tests/      Pruebas unitarias para modelos de dominio e InMemoryMediaLibrary
    Aether.Media.Social.Tests/    Pruebas unitarias para SocialGraph y FeedAggregator
  typescript/                     Reproductor web TypeScript y SDK social (@bhengubv/aether-media)
    src/
      player/   AetherMediaPlayer (HLS.js + Shaka Player + MSE nativo)
      social/   FeedClient, ReactionClient
      identity/ ProfileClient
      streaming/ AetherStreamClient
      models/   Espejos TypeScript de los modelos de dominio C#
  python/                         Motor de plugins Python y biblioteca de metadatos (aether-media en PyPI)
    aether_media/
      plugins/  Clase base AetherMediaPlugin, PluginHost
      metadata/ Lector/escritor de etiquetas (wrapper de mutagen)
      cli/      Puntos de entrada de línea de comandos
  rust/                           Motor de feed Rust (aether-media en crates.io)
    src/
      feed/     FeedStore, FeedEntry
      social/   SocialGraph, follow/unfollow
      streaming/ StreamAnnounce, modelos de segmentos
      player/   Reproducción de audio mediante rodio
  go/                             Biblioteca de grafo social Go (github.com/bhengubv/aether-media/go)
    social/     SocialGraph
    player/     Modelos de reproductor
    feed/       Modelos de feed
    streaming/  Modelos de stream
  kotlin/                         Grafo social Kotlin/JVM + integración Android ExoPlayer
    src/main/kotlin/
      social/   SocialGraph (respaldado por ConcurrentHashMap, JVM y Android)
      feed/     Modelos de feed
      player/   Integración ExoPlayer (Android; las pruebas JVM usan solo el núcleo)
      content/  Modelos de descriptor de contenido
      streaming/ Modelos de sesión de stream
    android/    Módulo Gradle Android con dependencia media3-exoplayer
  swift/                          Reproductor Swift / plataforma Apple (paquete SwiftPM)
    Sources/AetherMedia/
      social/   SocialGraph (basado en actor, Swift Concurrency)
      player/   Reproductor AVFoundation
      feed/     Modelos de feed
      streaming/ Modelos de stream
  c/                              Modelos de feed y sociales C11 para objetivos embebidos
    include/aether_media/         Cabeceras públicas
    src/                          Implementaciones
    tests/                        Suite de pruebas basada en CTest
  android/                        Módulos Gradle Android
    media/      Módulo multimedia principal (Kotlin + Jetpack)
    media-tv/   Variante Android TV
  docs/                           Notas de arquitectura y decisiones de diseño
```

---

## Compilación

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

## Licencia

MIT — gratis para siempre. El motor de códecs (LibVLC / FFmpeg / AVFoundation / media3 / ExoPlayer)
se usa mediante LGPL y Apache 2.0 respectivamente, sin modificaciones. Consulta [LICENSE](LICENSE).
