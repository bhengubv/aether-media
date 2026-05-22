# Aether Media — Implementación en C

[English](../../../../c/README.md) · [Français](../../fr/c/README.md) · [Español](README.md) · [العربية](../../ar/c/README.md) · [中文简体](../../zh-CN/c/README.md) · [日本語](../../ja/c/README.md) · [Deutsch](../../de/c/README.md) · [Português (BR)](../../pt-BR/c/README.md) · [Русский](../../ru/c/README.md) · [فارسی](../../fa/c/README.md) · [한국어](../../ko/c/README.md)

Una implementación C99 de Aether Media sin interfaz gráfica y apta para entornos embebidos. Funciona en computadoras de placa única Linux, ESP32, nRF52 y cualquier sistema POSIX. Proporciona descubrimiento de contenido, gestión del grafo social y (cuando LibVLC está disponible) reproducción multimedia — todo sobre la malla de Aether sin necesidad de conexión a internet.

---

## Requisitos

- Compilador compatible con C99 (GCC 10+, Clang 12+)
- CMake 3.20+
- Opcional: LibVLC (para reproducción; las funciones sociales y de feed funcionan sin él)
- Opcional: biblioteca C de aether-protocol (para transporte en malla)

---

## Compilación

```bash
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --parallel
```

### Compilar sin LibVLC (solo feed + social)

```bash
cmake .. -DAETHER_MEDIA_ENABLE_PLAYER=OFF
cmake --build .
```

### Compilación cruzada para ARM (p. ej., Raspberry Pi)

```bash
cmake .. -DCMAKE_TOOLCHAIN_FILE=../cmake/arm-linux-gnueabihf.cmake
cmake --build .
```

---

## Ejecutar pruebas

```bash
cd build
ctest --output-on-failure
```

---

## Descripción general de la API

Incluya el encabezado paraguas único:

```c
#include "aether_media/aether_media.h"
```

### Modelo de contenido

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

### Grafo social

```c
AetherSocialGraph *graph = aether_social_graph_create();

aether_social_graph_follow(graph, "peer-uhid-abc123");
aether_social_graph_follow(graph, "peer-uhid-def456");

printf("following: %zu\n", aether_social_graph_following_count(graph));

bool is_following = aether_social_graph_is_following(graph, "peer-uhid-abc123");

aether_social_graph_unfollow(graph, "peer-uhid-abc123");
aether_social_graph_destroy(graph);
```

### Agregador de feed

```c
AetherFeedAggregator *feed = aether_feed_aggregator_create(500);

AetherMediaFeedItem item = {0};
strncpy(item.content_hash, "sha256...", sizeof(item.content_hash) - 1);
aether_feed_aggregator_push(feed, &item);

printf("feed size: %zu\n", aether_feed_aggregator_size(feed));
aether_feed_aggregator_destroy(feed);
```

### Reproductor (requiere LibVLC)

```c
AetherPlayer *player = aether_player_create();
aether_player_open(player, "aether://content/sha256abc");
aether_player_play(player);
/* ... */
aether_player_stop(player);
aether_player_destroy(player);
```

---

## Estructura del proyecto

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

## Compatibilidad de formato de cable

La implementación en C es compatible en formato de cable con la implementación de referencia en C# y todas las demás implementaciones de Aether Media en otros lenguajes. Los hashes de contenido, los UHIDs de perfiles y las estructuras de elementos de feed son idénticos en todas las plataformas.

---

## Notas por plataforma

| Plataforma | Reproductor | Social | Feed | Streaming |
|------------|-------------|--------|------|-----------|
| Linux (x86-64, ARM) | ✅ LibVLC | ✅ | ✅ | ✅ |
| macOS | ✅ LibVLC | ✅ | ✅ | ✅ |
| ESP32 / nRF52 | ❌ headless | ✅ | ✅ | ✅ |
| Windows (MinGW) | ✅ LibVLC | ✅ | ✅ | ✅ |

---

## Licencia

MIT
