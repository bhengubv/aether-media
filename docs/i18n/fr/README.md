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

> Un réseau social décentralisé et un lecteur multimédia construits sur le protocole maillage Aether.
> Aucune connexion internet requise. Aucun serveur central. Aucun propriétaire d'entreprise.

Deux téléphones dans la même pièce — sans Wi-Fi, sans données mobiles — peuvent se découvrir mutuellement,
partager des médias, diffuser de la vidéo en direct, et interagir socialement via BLE, Wi-Fi Direct, NearLink,
LoRa, ou relais HTTP.

---

## Pourquoi Aether Media ?

**Diffusez un concert en direct sur les téléphones du public — sans internet, sans CDN, sans frais de streaming.**

L'appareil de l'artiste diffuse via Wi-Fi Direct. Chaque téléphone à portée reçoit le flux et le relaie plus loin via BLE. Les réactions (likes, super-réactions, commentaires à la position exacte de lecture) remontent par le même chemin. Pas de mise en tampon depuis un centre de données à 10 000 km. Aucun compte requis pour le public.

```
  [Artiste] ──WiFi Direct──▶ [Rang 1] ──BLE──▶ [Rang 2] ──NearLink──▶ [Rang 3]
                 1080p en direct          relayé, chiffré        relayé, chiffré
```

**Suivez un créateur hors ligne. Recevez son contenu quand il revient à portée.**

Les abonnements sont livrés via la couche DTN (Delay-Tolerant Networking) de stockage-et-transmission différée d'Aether. Si l'appareil du créateur n'est pas joignable maintenant, l'intention d'abonnement attend — jusqu'à 72 heures — et se livre dès qu'une route s'ouvre. Pas d'infrastructure de notification push, pas de serveur d'application.

**Regardez un film ensemble à travers le maillage.**

Quelqu'un sur le maillage a le fichier. Le coordinateur de la soirée cinéma synchronise lecture, pause et avance sur chaque appareil avec compensation RTT. Les réactions s'exécutent en temps réel à l'horodatage exact dans la vidéo. Si l'appareil de l'hôte se déconnecte en plein film, la session migre automatiquement vers le prochain pair disponible.

---

### Comparaison avec les lecteurs et réseaux existants

| Capacité | VLC | Spotify | Streambert | Aether Media |
|---|:---:|:---:|:---:|:---:|
| Fonctionne hors ligne (sans internet) | Lecture seulement | Non | Non | **Oui — découvrir, diffuser, réagir** |
| Fonctionne sans compte d'application | Oui | Non | Non | **Oui** |
| Relayage maillage (sauts entre appareils) | Non | Non | Non | **Oui** |
| Streaming en direct, sans CDN | Non | Non | Partiel | **Oui — BLE / Wi-Fi Direct / NearLink** |
| Réactions sociales sur la timeline | Non | Non | Non | **Oui — à la position exacte de lecture** |
| Graphe de suivi sans serveur | Non | Non | Non | **Oui — livré par DTN** |
| Latence inférieure à la seconde dans la même pièce | N/A | Non | Non | **Oui — NearLink 20 µs** |
| Fonctionne sur microcontrôleur / C11 | Non | Non | Non | **Oui — implémentation C** |
| SDK 8 langages | Non | Non | Non | **Oui** |

---

## Comment ça fonctionne

**Étape 1 — Découverte maillage.** Les appareils diffusent leur Aether Tag (un identifiant court comme `@sam.5jk2`) via les annonces BLE et les réponses de sonde Wi-Fi Direct. Aucune adresse IP ni compte n'est nécessaire. La couche transport promeut automatiquement NearLink (600 m, 12 Mbps, 20 µs) sur les appareils qui en disposent, bascule sur Wi-Fi Direct (200 m, 250 Mbps), puis BLE (100 m, 1 Mbps), puis LoRa-sur-BLE (1,3 km), et enfin le relais HTTP en dernier recours.

**Étape 2 — Adressage de contenu.** Chaque élément multimédia est identifié par son hachage de contenu SHA-256 — pas par une URL ou un chemin de serveur. Un `ContentDescriptor` (hash + nom + type MIME + manifeste de fragments) est diffusé sur le maillage. Tout appareil qui a le fichier peut servir des fragments à tout appareil qui en a besoin. Il n'y a pas de serveur d'origine. Les fichiers peuvent être assemblés depuis des fragments détenus par différents pairs simultanément, à la manière de BitTorrent.

**Étape 3 — Couche sociale.** Les abonnements, réactions et mises à jour de profil sont encodés en charges utiles JSON signées et envoyés soit comme bundles DTN (pour une livraison tolérante aux pannes) soit comme `MeshPacket`s en best-effort (pour les réactions à faible latence pendant les streams en direct). Le `SocialGraph` suit qui vous suivez. Le `FeedAggregator` écoute les paquets `StreamAnnounce` et `ContentAnnounce` des créateurs suivis et assemble un fil chronologique — entièrement depuis les événements maillage, sans serveur de fil.

---

## Quelles Fonctionnalités Aether Sont Utilisées

Aether Media est construit sur [aether-protocol](https://github.com/bhengubv/aether-protocol) et utilise ces interfaces :

| Interface Aether | Paquet | Comment Aether Media l'utilise |
|---|---|---|
| `ITransportService` | `Aether.Transport` | Envoie des trames vidéo/audio encodées, des réactions et des intentions d'abonnement sur le maillage (BLE / Wi-Fi Direct / NearLink / LoRa / relais HTTP) |
| `IStreamingService` | `Aether.Streaming` | Diffuse `StreamAnnounce` lors du démarrage en direct ; `FeedAggregator` s'abonne aux événements `StreamAnnounced` et `StreamEnded` pour maintenir le fil de stream en direct |
| `IContentService` | `Aether.Content` | Publie des `ContentDescriptor`s pour les médias téléchargés ; `FeedAggregator` s'abonne à `ContentAnnounced` pour la découverte VOD |
| `IDtnService` | `Aether.Dtn` | Livre les intentions d'abonnement durablement aux créateurs hors ligne ; les bundles attendent jusqu'à 72h pour une route |
| `IMeshSender` | `Aether.Messaging` | Envoie des paquets de désabonnement en best-effort et des réactions en direct sur le maillage sans surcharge DTN |
| `IRoutingService` | `Aether.Routing` | Livraison consciente de la route des paquets sociaux ; RREQ/RREP de style AODV avec réponses de route signées Ed25519 |
| `SignalProtocolService` | `Aether.Security` | Chiffre de bout en bout les messages directs, les charges utiles de synchronisation de profil et le contenu de canal privé avec X3DH + Double Ratchet |
| `IAdaptiveBitrateController` | `Aether.Streaming` | Sélectionne le barreau de qualité le plus élevé soutenable (H.264 / H.265 / VP8) basé sur les estimations de bande passante en direct du transport actif |

---

## SDK 8 Langages

Aether Media embarque des implémentations en 8 langages pour fonctionner sur chaque plateforme de l'écosystème.

| Langage | Répertoire | Plateforme | Moteur Multimédia | Rôle |
|---|---|---|---|---|
| C# (.NET 10) | `src/` | Windows · Linux · macOS | LibVLC (LibVLCSharp) | Implémentation de référence, DI complète, paquets NuGet |
| TypeScript | `typescript/` | Navigateur · Node 20 | HLS.js · Shaka Player | Lecteur web, client de fil, SDK social |
| Python | `python/` | Tout Python 3.11+ | mutagen (métadonnées) | Moteur de plugins, scripts, traitement de métadonnées |
| Rust | `rust/` | Toute cible Rust | `rodio` (audio) | Moteur de fil haute performance, benchmarks |
| Go | `go/` | Toute cible Go 1.22 | — | Bibliothèque de graphe social |
| Kotlin | `kotlin/` | JVM 21 · Android | media3 / ExoPlayer (Android) | Lecteur Android ; graphe social JVM pour usage côté serveur |
| Swift | `swift/` | macOS 13+ · iOS 16+ | AVFoundation | Lecteur plateforme Apple |
| C | `c/` | Toute cible C11 | — | Modèles de fil et social embarqués / microcontrôleur |

Les 8 implémentations partagent le même format fil qu'`aether-protocol` et produisent
des paquets sociaux interopérables vérifiés par des fixtures cross-langages dans la CI.

---

## Démarrage Rapide

### C# Bureau (Windows / Linux / macOS)

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media
dotnet run --project samples/Aether.Media.Demo.Console
```

Enregistrer tous les sous-systèmes :

```csharp
services.AddAetherMedia(media =>
    media.AddIdentity()
         .AddContent()
         .AddSocial()
         .AddStreaming()
         .AddAI());
```

Résoudre et utiliser :

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

### TypeScript (Navigateur)

```typescript
import { AetherMediaPlayer } from '@bhengubv/aether-media';

const video  = document.querySelector('video') as HTMLVideoElement;
const player = new AetherMediaPlayer(video);

// Charger un flux HLS publié par un pair sur le maillage
await player.load('aether://stream/KXJB7-MN2P4');
await player.play();

// Alimenter des segments maillage bruts directement dans le pipeline MSE
player.feedSegment(encodedBytes, 'video/mp4; codecs="avc1.42E01E"');
```

Client de fil avec cache de stockage local :

```typescript
import { FeedClient } from '@bhengubv/aether-media';

const client = new FeedClient('https://relay.aether.network/media');
const items  = await client.getFeed(20, 0);   // limite, décalage

for (const item of items) {
    console.log(item.content.title, item.likeCount);
}

await client.markWatched('a3f9...', 45_000);  // contentHash, ms regardés
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
        print(f"Chargé : {content.title}  ({content.formatted_duration})")

    def on_reaction_received(self, reaction: MediaReaction) -> None:
        print(f"Réaction : {reaction.type.name} à {reaction.position_ms} ms")
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
println!("Le fil contient {} élément(s)", store.len());
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
printf("Suivi : %d\n", aether_social_graph_is_following(graph, "KXJB7-MN2P4")); // 1
aether_social_graph_destroy(graph);
```

---

## Protocole Social

La couche sociale n'a pas de serveur. Chaque abonnement, désabonnement, annonce de contenu et
réaction est un `MeshPacket` ou `DtnBundle` signé qui transite par la radio disponible.

**Les abonnements** sont enveloppés dans un `FollowIntentPayload` (JSON UTF-8), chiffrés avec la
clé de session Signal Protocol du créateur cible (X3DH + Double Ratchet), et validés
en tant que `DtnBundle` adressé au UHID cible. La couche DTN stocke le bundle localement
et le livre sur le maillage dès qu'un chemin vers la cible s'ouvre — même si cela prend
des heures. L'appareil du créateur reçoit le bundle, vérifie la signature, et incrémente
son nombre d'abonnés. Tout cela se produit sans qu'aucun serveur central ne connaisse la
relation.

**Les annonces de contenu** sont des paquets `ContentDescriptor` diffusés par l'appareil publiant.
Chaque appareil qui reçoit le descripteur le met en cache et le rediffuse aux pairs
à proximité (inondation maillage avec déduplication). Le `FeedAggregator` sur chaque appareil écoute
ces diffusions et met en avant le nouveau contenu des créateurs suivis dans le fil local.

**Les réactions** (like, partage, super-réaction, commentaire) portent le hash du contenu, le type
de réaction et la position exacte de lecture en millisecondes. Elles transitent comme des `MeshPacket`s
en best-effort adressés au UHID du créateur — routés par AODV avec des réponses de route signées,
de sorte qu'aucune fausse destination ne peut les intercepter. Pendant un stream en direct, les réactions
sont agrégées et affichées en temps réel sur l'appareil de l'éditeur sans quitter le maillage.

**La synchronisation de profil** utilise le même mécanisme DTN que les abonnements. Quand un créateur met à jour
son nom d'affichage, son avatar ou sa biographie, le nouveau `MediaProfile` est signé avec sa clé d'identité
Ed25519, sérialisé, et diffusé comme bundle DTN. Tout appareil qui le reçoit — directement ou via relais —
vérifie la signature et met à jour son cache local. Une mise à jour de profil effectuée hors ligne atteint
les abonnés la prochaine fois que l'un d'eux se trouve à portée radio.

---

## Structure du Dépôt

```
aether-media/
  src/
    Aether.Media.Core/            Modèles de domaine et interfaces (MediaContent, IMediaLibrary, etc.)
    Aether.Media.Identity/        Gestion de profil, avatar, synchronisation de profil
    Aether.Media.Content/         Scanner de bibliothèque multimédia, résolveur de métadonnées, cache LRU, miniatures
    Aether.Media.Social/          SocialGraph, FeedAggregator, ReactionService, DiscoveryService
    Aether.Media.Streaming/       LiveStreamPublisher, WatchPartyCoordinator, AbrController
    Aether.Media.AI/              ContentRanker, ContentModerator, CreatorReputationView
    Aether.Media.DependencyInjection/  Extension AddAetherMedia() + API fluent AetherMediaBuilder
    Aether.Media.Desktop/         Intégration LibVLCSharp pour Windows / Linux / macOS
  samples/
    Aether.Media.Demo.Console/    Démo console interactive montrant tous les sous-systèmes
    Aether.Media.RelayTest/       Test d'aller-retour relais HTTP (nécessite Aether.RelayServer)
  tests/
    Aether.Media.Core.Tests/      Tests unitaires pour les modèles de domaine et InMemoryMediaLibrary
    Aether.Media.Social.Tests/    Tests unitaires pour SocialGraph et FeedAggregator
  typescript/                     Lecteur web TypeScript et SDK social (@bhengubv/aether-media)
    src/
      player/   AetherMediaPlayer (HLS.js + Shaka Player + MSE natif)
      social/   FeedClient, ReactionClient
      identity/ ProfileClient
      streaming/ AetherStreamClient
      models/   Miroirs TypeScript des modèles de domaine C#
  python/                         Moteur de plugins Python et bibliothèque de métadonnées (aether-media sur PyPI)
    aether_media/
      plugins/  Classe de base AetherMediaPlugin, PluginHost
      metadata/ Lecteur/écrivain de balises (wrapper mutagen)
      cli/      Points d'entrée en ligne de commande
  rust/                           Moteur de fil Rust (aether-media sur crates.io)
    src/
      feed/     FeedStore, FeedEntry
      social/   SocialGraph, follow/unfollow
      streaming/ StreamAnnounce, modèles de segments
      player/   Lecture audio via rodio
  go/                             Bibliothèque de graphe social Go (github.com/bhengubv/aether-media/go)
    social/     SocialGraph
    player/     Modèles de lecteur
    feed/       Modèles de fil
    streaming/  Modèles de stream
  kotlin/                         Graphe social Kotlin/JVM + intégration Android ExoPlayer
    src/main/kotlin/
      social/   SocialGraph (sauvegardé par ConcurrentHashMap, JVM et Android)
      feed/     Modèles de fil
      player/   Intégration ExoPlayer (Android ; les tests JVM n'utilisent que le cœur)
      content/  Modèles de descripteur de contenu
      streaming/ Modèles de session de stream
    android/    Module Gradle Android avec dépendance media3-exoplayer
  swift/                          Lecteur plateforme Swift / Apple (paquet SwiftPM)
    Sources/AetherMedia/
      social/   SocialGraph (basé sur les acteurs, Swift Concurrency)
      player/   Lecteur AVFoundation
      feed/     Modèles de fil
      streaming/ Modèles de stream
  c/                              Modèles de fil et social C11 pour cibles embarquées
    include/aether_media/         En-têtes publics
    src/                          Implémentations
    tests/                        Suite de tests basée sur CTest
  android/                        Modules Gradle Android
    media/      Module multimédia principal (Kotlin + Jetpack)
    media-tv/   Variante Android TV
  docs/                           Notes d'architecture et décisions de conception
```

---

## Construction

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

## Licence

MIT — gratuit pour toujours. Le moteur de codec (LibVLC / FFmpeg / AVFoundation / media3 / ExoPlayer)
est utilisé via LGPL et Apache 2.0 respectivement, sans modification. Voir [LICENSE](LICENSE).
