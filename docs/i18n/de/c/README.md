# Aether Media — C Implementation

[English](../../../../c/README.md) · [Français](../../fr/c/README.md) · [Español](../../es/c/README.md) · [العربية](../../ar/c/README.md) · [中文简体](../../zh-CN/c/README.md) · [日本語](../../ja/c/README.md) · [Deutsch](README.md) · [Português (BR)](../../pt-BR/c/README.md) · [Русский](../../ru/c/README.md) · [فارسی](../../fa/c/README.md) · [한국어](../../ko/c/README.md)

Eine kopflose, eingebettungsfreundliche C99-Implementierung von Aether Media. Läuft auf Linux-Einplatinencomputern, ESP32, nRF52 und jedem POSIX-System. Bietet Inhaltserkennung, Verwaltung des sozialen Graphen und — sofern LibVLC verfügbar ist — Medienwiedergabe, alles über das Aether-Mesh ohne Internetverbindung.

---

## Voraussetzungen

- C99-kompatibler Compiler (GCC 10+, Clang 12+)
- CMake 3.20+
- Optional: LibVLC (für die Wiedergabe; soziale Funktionen und Feed funktionieren ohne LibVLC)
- Optional: aether-protocol C-Bibliothek (für Mesh-Transport)

---

## Build

```bash
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --parallel
```

### Build ohne LibVLC (nur Feed und soziale Funktionen)

```bash
cmake .. -DAETHER_MEDIA_ENABLE_PLAYER=OFF
cmake --build .
```

### Cross-Kompilierung für ARM (z. B. Raspberry Pi)

```bash
cmake .. -DCMAKE_TOOLCHAIN_FILE=../cmake/arm-linux-gnueabihf.cmake
cmake --build .
```

---

## Tests ausführen

```bash
cd build
ctest --output-on-failure
```

---

## API-Übersicht

Binden Sie den einzigen Umbrella-Header ein:

```c
#include "aether_media/aether_media.h"
```

### Inhaltsmodell

```c
AetherMediaContent content;
memset(&content, 0, sizeof(content));
strncpy(content.title, "My Video", sizeof(content.title) - 1);
content.duration_ms = 300000; /* 5 minutes */

/* Format duration for display */
char buf[16];
aether_format_duration(content.duration_ms, buf, sizeof(buf));
printf("%s\n", buf); /* "5:00" */
```

### Sozialer Graph

```c
AetherSocialGraph *graph = aether_social_graph_create();

aether_social_graph_follow(graph, "peer-uhid-abc123");
aether_social_graph_follow(graph, "peer-uhid-def456");

printf("following: %zu\n", aether_social_graph_following_count(graph));

bool is_following = aether_social_graph_is_following(graph, "peer-uhid-abc123");

aether_social_graph_unfollow(graph, "peer-uhid-abc123");
aether_social_graph_destroy(graph);
```

### Feed-Aggregator

```c
AetherFeedAggregator *feed = aether_feed_aggregator_create(500);

AetherMediaFeedItem item = {0};
strncpy(item.content_hash, "sha256...", sizeof(item.content_hash) - 1);
aether_feed_aggregator_push(feed, &item);

printf("feed size: %zu\n", aether_feed_aggregator_size(feed));
aether_feed_aggregator_destroy(feed);
```

### Player (erfordert LibVLC)

```c
AetherPlayer *player = aether_player_create();
aether_player_open(player, "aether://content/sha256abc");
aether_player_play(player);
/* ... */
aether_player_stop(player);
aether_player_destroy(player);
```

---

## Projektstruktur

```
c/
├── include/
│   └── aether_media.h      # Public API — single umbrella header
├── src/
│   ├── main.c              # CLI entry point
│   ├── player/             # LibVLC playback wrapper
│   ├── social/             # Social graph implementation
│   └── streaming/          # Aether stream subscription
├── tests/                  # CTest unit tests
└── CMakeLists.txt
```

---

## Wire-Kompatibilität

Die C-Implementierung ist wire-format-kompatibel mit der C#-Referenzimplementierung und allen anderen Aether-Media-Sprachbindungen. Inhalts-Hashes, Profil-UHIDs und Feed-Element-Strukturen sind auf allen Plattformen identisch.

---

## Plattformhinweise

| Plattform | Player | Sozial | Feed | Streaming |
|-----------|--------|--------|------|-----------|
| Linux (x86-64, ARM) | ✅ LibVLC | ✅ | ✅ | ✅ |
| macOS | ✅ LibVLC | ✅ | ✅ | ✅ |
| ESP32 / nRF52 | ❌ kopflos | ✅ | ✅ | ✅ |
| Windows (MinGW) | ✅ LibVLC | ✅ | ✅ | ✅ |

---

## Lizenz

MIT
