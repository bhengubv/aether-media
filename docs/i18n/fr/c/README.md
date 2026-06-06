# Aether Media — Implémentation C

[English](../../../../c/README.md) · [Français](README.md) · [Español](../../es/c/README.md) · [العربية](../../ar/c/README.md) · [中文简体](../../zh-CN/c/README.md) · [日本語](../../ja/c/README.md) · [Deutsch](../../de/c/README.md) · [Português (BR)](../../pt-BR/c/README.md) · [Русский](../../ru/c/README.md) · [فارسی](../../fa/c/README.md) · [한국어](../../ko/c/README.md)

Une implémentation C99 sans interface graphique, adaptée aux systèmes embarqués, d'Aether Media. Fonctionne sur des ordinateurs monocartes Linux, ESP32, nRF52 et tout système POSIX. Fournit la découverte de contenu, la gestion du graphe social et (lorsque LibVLC est disponible) la lecture multimédia — le tout sur le maillage Aether sans connexion internet requise.

---

## Prérequis

- Compilateur compatible C99 (GCC 10+, Clang 12+)
- CMake 3.20+
- Optionnel : LibVLC (pour la lecture ; les fonctions sociales et de fil fonctionnent sans lui)
- Optionnel : bibliothèque C aether-protocol (pour le transport par maillage)

---

## Compilation

```bash
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --parallel
```

### Compilation sans LibVLC (fil + social uniquement)

```bash
cmake .. -DAETHER_MEDIA_ENABLE_PLAYER=OFF
cmake --build .
```

### Compilation croisée pour ARM (ex. Raspberry Pi)

```bash
cmake .. -DCMAKE_TOOLCHAIN_FILE=../cmake/arm-linux-gnueabihf.cmake
cmake --build .
```

---

## Exécuter les tests

```bash
cd build
ctest --output-on-failure
```

---

## Aperçu de l'API

Incluez l'en-tête chapeau unique :

```c
#include "aethermedia/aethermedia.h"
```

### Modèle de contenu

```c
AetherNetMediaContent content;
memset(&content, 0, sizeof(content));
strncpy(content.title, "My Video", sizeof(content.title) - 1);
content.duration_ms = 300000; /* 5 minutes */

/* Format duration for display */
char buf[16];
aethernet_format_duration(content.duration_ms, buf, sizeof(buf));
printf("%s\n", buf); /* "5:00" */
```

### Graphe social

```c
AetherNetSocialGraph *graph = aethernet_social_graph_create();

aethernet_social_graph_follow(graph, "peer-uhid-abc123");
aethernet_social_graph_follow(graph, "peer-uhid-def456");

printf("following: %zu\n", aethernet_social_graph_following_count(graph));

bool is_following = aethernet_social_graph_is_following(graph, "peer-uhid-abc123");

aethernet_social_graph_unfollow(graph, "peer-uhid-abc123");
aethernet_social_graph_destroy(graph);
```

### Agrégateur de fil

```c
AetherNetFeedAggregator *feed = aethernet_feed_aggregator_create(500);

AetherNetMediaFeedItem item = {0};
strncpy(item.content_hash, "sha256...", sizeof(item.content_hash) - 1);
aethernet_feed_aggregator_push(feed, &item);

printf("feed size: %zu\n", aethernet_feed_aggregator_size(feed));
aethernet_feed_aggregator_destroy(feed);
```

### Lecteur (nécessite LibVLC)

```c
AetherNetPlayer *player = aethernet_player_create();
aethernet_player_open(player, "aether://content/sha256abc");
aethernet_player_play(player);
/* ... */
aethernet_player_stop(player);
aethernet_player_destroy(player);
```

---

## Structure du projet

```
c/
├── include/
│   └── aethermedia.h      # Public API — single umbrella header
├── src/
│   ├── main.c              # CLI entry point
│   ├── player/             # LibVLC playback wrapper
│   ├── social/             # Social graph implementation
│   └── streaming/          # Aether stream subscription
├── tests/                  # CTest unit tests
└── CMakeLists.txt
```

---

## Compatibilité du format filaire

L'implémentation C est compatible au niveau du format filaire avec l'implémentation de référence C# et toutes les autres liaisons de langage Aether Media. Les hachages de contenu, les UHIDs de profil et les structures d'éléments de fil sont identiques sur toutes les plateformes.

---

## Notes par plateforme

| Plateforme | Lecteur | Social | Fil | Streaming |
|------------|---------|--------|-----|-----------|
| Linux (x86-64, ARM) | ✅ LibVLC | ✅ | ✅ | ✅ |
| macOS | ✅ LibVLC | ✅ | ✅ | ✅ |
| ESP32 / nRF52 | ❌ sans interface | ✅ | ✅ | ✅ |
| Windows (MinGW) | ✅ LibVLC | ✅ | ✅ | ✅ |

---

## Licence

MIT
