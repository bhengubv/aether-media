# Aether Media — C Implementation

[English](README.md) · [Français](../docs/i18n/fr/c/README.md) · [Español](../docs/i18n/es/c/README.md) · [العربية](../docs/i18n/ar/c/README.md) · [中文简体](../docs/i18n/zh-CN/c/README.md) · [日本語](../docs/i18n/ja/c/README.md) · [Deutsch](../docs/i18n/de/c/README.md) · [Português (BR)](../docs/i18n/pt-BR/c/README.md) · [Русский](../docs/i18n/ru/c/README.md) · [فارسی](../docs/i18n/fa/c/README.md) · [한국어](../docs/i18n/ko/c/README.md)

A headless, embedded-friendly C99 implementation of Aether Media. Runs on Linux single-board computers, ESP32, nRF52, and any POSIX system. Provides content discovery, social graph management, and (where LibVLC is available) media playback — all over the Aether mesh with no internet connection required.

---

## Requirements

- C99-compatible compiler (GCC 10+, Clang 12+)
- CMake 3.20+
- Optional: LibVLC (for playback; social and feed functions work without it)
- Optional: aether-protocol C library (for mesh transport)

---

## Build

```bash
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --parallel
```

### Build without LibVLC (feed + social only)

```bash
cmake .. -DAETHER_MEDIA_ENABLE_PLAYER=OFF
cmake --build .
```

### Cross-compile for ARM (e.g. Raspberry Pi)

```bash
cmake .. -DCMAKE_TOOLCHAIN_FILE=../cmake/arm-linux-gnueabihf.cmake
cmake --build .
```

---

## Run tests

```bash
cd build
ctest --output-on-failure
```

---

## API overview

Include the single umbrella header:

```c
#include "aethermesh_media/aethermesh_media.h"
```

### Content model

```c
AetherMeshMediaContent content;
memset(&content, 0, sizeof(content));
strncpy(content.title, "My Video", sizeof(content.title) - 1);
content.duration_ms = 300000; /* 5 minutes */

/* Format duration for display */
char buf[16];
aethermesh_format_duration(content.duration_ms, buf, sizeof(buf));
printf("%s\n", buf); /* "5:00" */
```

### Social graph

```c
AetherMeshSocialGraph *graph = aethermesh_social_graph_create();

aethermesh_social_graph_follow(graph, "peer-uhid-abc123");
aethermesh_social_graph_follow(graph, "peer-uhid-def456");

printf("following: %zu\n", aethermesh_social_graph_following_count(graph));

bool is_following = aethermesh_social_graph_is_following(graph, "peer-uhid-abc123");

aethermesh_social_graph_unfollow(graph, "peer-uhid-abc123");
aethermesh_social_graph_destroy(graph);
```

### Feed aggregator

```c
AetherMeshFeedAggregator *feed = aethermesh_feed_aggregator_create(500);

AetherMeshMediaFeedItem item = {0};
strncpy(item.content_hash, "sha256...", sizeof(item.content_hash) - 1);
aethermesh_feed_aggregator_push(feed, &item);

printf("feed size: %zu\n", aethermesh_feed_aggregator_size(feed));
aethermesh_feed_aggregator_destroy(feed);
```

### Player (requires LibVLC)

```c
AetherMeshPlayer *player = aethermesh_player_create();
aethermesh_player_open(player, "aether://content/sha256abc");
aethermesh_player_play(player);
/* ... */
aethermesh_player_stop(player);
aethermesh_player_destroy(player);
```

---

## Project layout

```
c/
├── include/
│   └── aethermesh_media.h      # Public API — single umbrella header
├── src/
│   ├── main.c              # CLI entry point
│   ├── player/             # LibVLC playback wrapper
│   ├── social/             # Social graph implementation
│   └── streaming/          # Aether stream subscription
├── tests/                  # CTest unit tests
└── CMakeLists.txt
```

---

## Wire compatibility

The C implementation is wire-format compatible with the C# reference implementation and all other Aether Media language bindings. Content hashes, profile UHIDs, and feed item structures are identical across all platforms.

---

## Platform notes

| Platform | Player | Social | Feed | Streaming |
|----------|--------|--------|------|-----------|
| Linux (x86-64, ARM) | ✅ LibVLC | ✅ | ✅ | ✅ |
| macOS | ✅ LibVLC | ✅ | ✅ | ✅ |
| ESP32 / nRF52 | ❌ headless | ✅ | ✅ | ✅ |
| Windows (MinGW) | ✅ LibVLC | ✅ | ✅ | ✅ |

---

## License

MIT
