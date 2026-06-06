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

> Uma rede social descentralizada e player construídos sobre o protocolo mesh Aether.
> Sem internet. Sem servidor central. Sem dono corporativo.

Dois celulares no mesmo ambiente — sem Wi-Fi, sem dados móveis — podem se descobrir
mutuamente, compartilhar mídia, transmitir vídeo ao vivo e interagir socialmente via BLE,
Wi-Fi Direct, NearLink, LoRa ou relay HTTP.

---

## Por que Aether Media?

**Transmita um show ao vivo para os telefones da audiência — sem internet, sem CDN, sem taxa de streaming.**

O dispositivo do artista transmite via Wi-Fi Direct. Todos os telefones ao alcance recebem o stream e o retransmitem via BLE. Reações (curtidas, super-reações, comentários na posição exata de reprodução) viajam de volta pelo mesmo caminho. Sem buffering de um data center a 10.000 km de distância. Sem necessidade de conta para a audiência.

```
  [Performer] ──WiFi Direct──▶ [Row 1] ──BLE──▶ [Row 2] ──NearLink──▶ [Row 3]
                 1080p live           relayed, encrypted        relayed, encrypted
```

**Siga um criador que está offline. Receba o conteúdo quando ele voltar ao alcance.**

As solicitações de follow são entregues pela camada DTN (Delay-Tolerant Networking) store-and-forward do Aether. Se o dispositivo do criador não estiver acessível agora, a intenção de follow aguarda — até 72 horas — e é entregue no momento em que uma rota se abrir. Sem infraestrutura de push notification, sem servidor de app.

**Assista a um filme juntos pela mesh.**

Alguém na mesh tem o arquivo. O coordenador do Watch Party sincroniza play, pause e avanço em todos os dispositivos com compensação de RTT. As reações disparam em tempo real no timestamp exato do vídeo. Se o dispositivo do host ficar offline no meio do filme, a sessão migra automaticamente para o próximo peer disponível.

---

### Comparação com players e redes existentes

| Capacidade | VLC | Spotify | Streambert | Aether Media |
|---|:---:|:---:|:---:|:---:|
| Funciona offline (sem internet) | Apenas reprodução | Não | Não | **Sim — descobrir, transmitir, reagir** |
| Funciona sem conta no app | Sim | Não | Não | **Sim** |
| Relay em mesh (saltos entre dispositivos) | Não | Não | Não | **Sim** |
| Streaming ao vivo, sem CDN | Não | Não | Parcial | **Sim — BLE / Wi-Fi Direct / NearLink** |
| Reações sociais na linha do tempo | Não | Não | Não | **Sim — na posição exata de reprodução** |
| Gráfico de follows sem servidor | Não | Não | Não | **Sim — entregue via DTN** |
| Latência abaixo de 1 segundo no mesmo ambiente | N/A | Não | Não | **Sim — NearLink 20 µs** |
| Roda em microcontrolador / C11 | Não | Não | Não | **Sim — implementação C** |
| SDK para 8 linguagens | Não | Não | Não | **Sim** |

---

## Como Funciona

**Etapa 1 — Descoberta na mesh.** Os dispositivos transmitem sua Aether Tag (um identificador de identidade curto como `@sam.5jk2`) via anúncios BLE e respostas de probe Wi-Fi Direct. Nenhum endereço IP ou conta é necessário. A camada de transporte automaticamente promove NearLink (600 m, 12 Mbps, 20 µs) em dispositivos que o possuem, faz fallback para Wi-Fi Direct (200 m, 250 Mbps), depois BLE (100 m, 1 Mbps), depois LoRa-over-BLE (1.3 km) e, finalmente, relay HTTP como último recurso.

**Etapa 2 — Endereçamento de conteúdo.** Cada mídia é identificada pelo seu hash de conteúdo SHA-256 — não por uma URL ou caminho de servidor. Um `ContentDescriptor` (hash + nome + tipo MIME + manifesto de chunks) é transmitido pela mesh. Qualquer dispositivo que tenha o arquivo pode servir chunks para qualquer dispositivo que precise. Não há servidor de origem. Os arquivos podem ser montados a partir de fragmentos mantidos por diferentes peers simultaneamente, no estilo BitTorrent.

**Etapa 3 — Camada social.** Follows, reações e atualizações de perfil são codificados como payloads JSON assinados e enviados como bundles DTN (para entrega tolerante a offline) ou `MeshPacket`s best-effort (para reações de baixa latência durante streams ao vivo). O `SocialGraph` rastreia quem você segue. O `FeedAggregator` ouve pacotes `StreamAnnounce` e `ContentAnnounce` de criadores seguidos e monta um feed cronológico — totalmente a partir de eventos da mesh, sem servidor de feed.

---

## Quais Funcionalidades do Aether São Usadas

O Aether Media é construído sobre [aether-protocol](https://github.com/bhengubv/aether-protocol) e usa estas interfaces:

| Interface Aether | Pacote | Como o Aether Media a usa |
|---|---|---|
| `ITransportService` | `AetherNet.Transport` | Envia frames de vídeo/áudio codificados, reações e intenções de follow pela mesh (BLE / Wi-Fi Direct / NearLink / LoRa / relay HTTP) |
| `IStreamingService` | `AetherNet.Streaming` | Transmite `StreamAnnounce` ao ir ao vivo; `FeedAggregator` assina os eventos `StreamAnnounced` e `StreamEnded` para manter o feed de live streams |
| `IContentService` | `AetherNet.Content` | Publica `ContentDescriptor`s para mídia enviada; `FeedAggregator` assina `ContentAnnounced` para descoberta de VOD |
| `IDtnService` | `AetherNet.Dtn` | Entrega intenções de follow de forma durável a criadores offline; bundles aguardam até 72h por uma rota |
| `IMeshSender` | `AetherNet.Messaging` | Envia pacotes de unfollow e reações ao vivo best-effort pela mesh sem overhead do DTN |
| `IRoutingService` | `AetherNet.Routing` | Entrega com consciência de rota de pacotes sociais; RREQ/RREP no estilo AODV com respostas de rota assinadas por Ed25519 |
| `SignalProtocolService` | `AetherNet.Security` | Criptografa de ponta a ponta mensagens diretas, payloads de sincronização de perfil e conteúdo de canais privados com X3DH + Double Ratchet |
| `IAdaptiveBitrateController` | `AetherNet.Streaming` | Seleciona o degrau de qualidade sustentável mais alto (H.264 / H.265 / VP8) com base em estimativas de largura de banda ao vivo do transporte ativo |

---

## SDK para 8 Linguagens

O Aether Media vem com implementações em 8 linguagens para rodar em todas as plataformas do ecossistema.

| Linguagem | Diretório | Plataforma | Engine de Mídia | Função |
|---|---|---|---|---|
| C# (.NET 10) | `src/` | Windows · Linux · macOS | LibVLC (LibVLCSharp) | Implementação de referência, DI completo, pacotes NuGet |
| TypeScript | `typescript/` | Browser · Node 20 | HLS.js · Shaka Player | Player web, cliente de feed, SDK social |
| Python | `python/` | Qualquer Python 3.11+ | mutagen (metadados) | Motor de plugins, scripting, processamento de metadados |
| Rust | `rust/` | Qualquer alvo Rust | `rodio` (áudio) | Motor de feed de alta performance, benchmarks |
| Go | `go/` | Qualquer alvo Go 1.22 | — | Biblioteca de gráfico social |
| Kotlin | `kotlin/` | JVM 21 · Android | media3 / ExoPlayer (Android) | Player Android; gráfico social JVM para uso server-side |
| Swift | `swift/` | macOS 13+ · iOS 16+ | AVFoundation | Player para plataformas Apple |
| C | `c/` | Qualquer alvo C11 | — | Feed embutido / microcontrolador e modelos sociais |

Todas as 8 implementações compartilham o mesmo formato wire do `aether-protocol` e produzem
pacotes sociais interoperáveis verificados por fixtures multilinguagem no CI.

---

## Início Rápido

### C# Desktop (Windows / Linux / macOS)

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media
dotnet run --project samples/AetherNet.Media.Demo.Console
```

Registre todos os subsistemas:

```csharp
services.AddAetherNetMedia(media =>
    media.AddIdentity()
         .AddContent()
         .AddSocial()
         .AddStreaming()
         .AddAI());
```

Resolva e use:

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
import { AetherNetMediaPlayer } from '@bhengubv/aether-media';

const video  = document.querySelector('video') as HTMLVideoElement;
const player = new AetherNetMediaPlayer(video);

// Load an HLS stream published by a peer on the mesh
await player.load('aether://stream/KXJB7-MN2P4');
await player.play();

// Feed raw mesh segments directly into the MSE pipeline
player.feedSegment(encodedBytes, 'video/mp4; codecs="avc1.42E01E"');
```

Cliente de feed com cache de armazenamento local:

```typescript
import { FeedClient } from '@bhengubv/aether-media';

const client = new FeedClient('https://relay.aethernet.network/media');
const items  = await client.getFeed(20, 0);   // limit, offset

for (const item of items) {
    console.log(item.content.title, item.likeCount);
}

await client.markWatched('a3f9...', 45_000);  // contentHash, ms watched
```

### Python (Plugin)

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

## Protocolo Social

A camada social não tem servidor. Cada follow, unfollow, anúncio de conteúdo e
reação é um `MeshPacket` ou `DtnBundle` assinado que viaja pelo rádio disponível.

**Follows** são envoltos em um `FollowIntentPayload` (JSON UTF-8), criptografados com a
chave de sessão Signal Protocol do criador alvo (X3DH + Double Ratchet) e confirmados
como um `DtnBundle` endereçado ao UHID alvo. A camada DTN armazena o bundle localmente
e o entrega pela mesh sempre que um caminho para o alvo se abrir — mesmo que isso leve
horas. O dispositivo do criador recebe o bundle, verifica a assinatura e incrementa seu
contador de seguidores. Tudo isso acontece sem que nenhum servidor central saiba da
relação.

**Anúncios de conteúdo** são pacotes `ContentDescriptor` transmitidos pelo dispositivo
publicador. Cada dispositivo que recebe o descriptor o armazena em cache e o retransmite
para peers próximos (flood em mesh com dedup). O `FeedAggregator` em cada dispositivo
ouve essas transmissões e apresenta novo conteúdo de criadores seguidos no feed local.

**Reações** (curtir, compartilhar, super-reagir, comentar) carregam o hash do conteúdo,
o tipo de reação e a posição exata de reprodução em milissegundos. Viajam como
`MeshPacket`s best-effort endereçados ao UHID do criador — roteados pelo AODV com
respostas de rota assinadas, para que nenhum destino falso possa interceptá-las. Durante
um stream ao vivo, as reações são agregadas e exibidas em tempo real no dispositivo do
publicador sem sair da mesh.

**Sincronização de perfil** usa o mesmo mecanismo DTN dos follows. Quando um criador
atualiza seu nome de exibição, avatar ou bio, o novo `MediaProfile` é assinado com sua
chave de identidade Ed25519, serializado e transmitido como um bundle DTN. Qualquer
dispositivo que o receber — diretamente ou via relay — verifica a assinatura e atualiza
seu cache local. Uma atualização de perfil feita offline chega aos seguidores na próxima
vez que qualquer um deles estiver dentro do alcance de rádio.

---

## Estrutura do Repositório

```
aether-media/
  src/
    AetherNet.Media.Core/            Modelos de domínio e interfaces (MediaContent, IMediaLibrary, etc.)
    AetherNet.Media.Identity/        Gerenciamento de perfil, avatar, sincronização de perfil
    AetherNet.Media.Content/         Scanner de biblioteca de mídia, resolvedor de metadados, cache LRU, thumbnails
    AetherNet.Media.Social/          SocialGraph, FeedAggregator, ReactionService, DiscoveryService
    AetherNet.Media.Streaming/       LiveStreamPublisher, WatchPartyCoordinator, AbrController
    AetherNet.Media.AI/              ContentRanker, ContentModerator, CreatorReputationView
    AetherNet.Media.DependencyInjection/  Extensão AddAetherNetMedia() + API fluente AetherNetMediaBuilder
    AetherNet.Media.Desktop/         Integração LibVLCSharp para Windows / Linux / macOS
  samples/
    AetherNet.Media.Demo.Console/    Demo interativo em console mostrando todos os subsistemas
    AetherNet.Media.RelayTest/       Teste de ida e volta via relay HTTP (requer AetherNet.RelayServer)
  tests/
    AetherNet.Media.Core.Tests/      Testes unitários para modelos de domínio e InMemoryMediaLibrary
    AetherNet.Media.Social.Tests/    Testes unitários para SocialGraph e FeedAggregator
  typescript/                     Player web TypeScript e SDK social (@bhengubv/aether-media)
    src/
      player/   AetherNetMediaPlayer (HLS.js + Shaka Player + MSE nativo)
      social/   FeedClient, ReactionClient
      identity/ ProfileClient
      streaming/ AetherNetStreamClient
      models/   Espelhos TypeScript dos modelos de domínio C#
  python/                         Motor de plugins Python e biblioteca de metadados (aether-media no PyPI)
    aethernet_media/
      plugins/  Classe base AetherNetMediaPlugin, PluginHost
      metadata/ Leitor/gravador de tags (wrapper mutagen)
      cli/      Pontos de entrada de linha de comando
  rust/                           Motor de feed Rust (aether-media no crates.io)
    src/
      feed/     FeedStore, FeedEntry
      social/   SocialGraph, follow/unfollow
      streaming/ StreamAnnounce, modelos de segmento
      player/   Reprodução de áudio via rodio
  go/                             Biblioteca de gráfico social Go (github.com/bhengubv/aether-media/go)
    social/     SocialGraph
    player/     Modelos de player
    feed/       Modelos de feed
    streaming/  Modelos de stream
  kotlin/                         Gráfico social Kotlin/JVM + integração Android ExoPlayer
    src/main/kotlin/
      social/   SocialGraph (baseado em ConcurrentHashMap, JVM e Android)
      feed/     Modelos de feed
      player/   Integração ExoPlayer (Android; testes JVM usam apenas o núcleo)
      content/  Modelos de descriptor de conteúdo
      streaming/ Modelos de sessão de stream
    android/    Módulo Gradle Android com dependência media3-exoplayer
  swift/                          Player Swift / plataformas Apple (pacote SwiftPM)
    Sources/AetherNetMedia/
      social/   SocialGraph (baseado em actor, Swift Concurrency)
      player/   Player AVFoundation
      feed/     Modelos de feed
      streaming/ Modelos de stream
  c/                              Modelos de feed e sociais C11 para alvos embarcados
    include/aethernet_media/         Cabeçalhos públicos
    src/                          Implementações
    tests/                        Suite de testes baseada em CTest
  android/                        Módulos Gradle Android
    media/      Módulo de mídia principal (Kotlin + Jetpack)
    media-tv/   Variante Android TV
  docs/                           Notas de arquitetura e decisões de design
```

---

## Compilação

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

## Licença

MIT — gratuito para sempre. O motor de codec (LibVLC / FFmpeg / AVFoundation / media3 / ExoPlayer)
é usado via LGPL e Apache 2.0 respectivamente, sem modificações. Veja [LICENSE](LICENSE).
