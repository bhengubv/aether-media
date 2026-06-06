# Aether Media — Implementação em C

[English](../../../../c/README.md) · [Français](../../fr/c/README.md) · [Español](../../es/c/README.md) · [العربية](../../ar/c/README.md) · [中文简体](../../zh-CN/c/README.md) · [日本語](../../ja/c/README.md) · [Deutsch](../../de/c/README.md) · [Português (BR)](README.md) · [Русский](../../ru/c/README.md) · [فارسی](../../fa/c/README.md) · [한국어](../../ko/c/README.md)

Uma implementação C99 do Aether Media sem interface gráfica, adequada para sistemas embarcados. Funciona em computadores de placa única com Linux, ESP32, nRF52 e qualquer sistema POSIX. Oferece descoberta de conteúdo, gerenciamento de grafo social e — quando o LibVLC estiver disponível — reprodução de mídia, tudo pela mesh Aether sem necessidade de conexão com a internet.

---

## Requisitos

- Compilador compatível com C99 (GCC 10+, Clang 12+)
- CMake 3.20+
- Opcional: LibVLC (para reprodução; as funções de social e feed funcionam sem ele)
- Opcional: biblioteca C do aether-protocol (para transporte via mesh)

---

## Build

```bash
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --parallel
```

### Build sem LibVLC (apenas feed + social)

```bash
cmake .. -DAETHER_MEDIA_ENABLE_PLAYER=OFF
cmake --build .
```

### Compilação cruzada para ARM (ex.: Raspberry Pi)

```bash
cmake .. -DCMAKE_TOOLCHAIN_FILE=../cmake/arm-linux-gnueabihf.cmake
cmake --build .
```

---

## Executar testes

```bash
cd build
ctest --output-on-failure
```

---

## Visão geral da API

Inclua o único cabeçalho abrangente:

```c
#include "aethermedia/aethermedia.h"
```

### Modelo de conteúdo

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

### Grafo social

```c
AetherNetSocialGraph *graph = aethernet_social_graph_create();

aethernet_social_graph_follow(graph, "peer-uhid-abc123");
aethernet_social_graph_follow(graph, "peer-uhid-def456");

printf("following: %zu\n", aethernet_social_graph_following_count(graph));

bool is_following = aethernet_social_graph_is_following(graph, "peer-uhid-abc123");

aethernet_social_graph_unfollow(graph, "peer-uhid-abc123");
aethernet_social_graph_destroy(graph);
```

### Agregador de feed

```c
AetherNetFeedAggregator *feed = aethernet_feed_aggregator_create(500);

AetherNetMediaFeedItem item = {0};
strncpy(item.content_hash, "sha256...", sizeof(item.content_hash) - 1);
aethernet_feed_aggregator_push(feed, &item);

printf("feed size: %zu\n", aethernet_feed_aggregator_size(feed));
aethernet_feed_aggregator_destroy(feed);
```

### Player (requer LibVLC)

```c
AetherNetPlayer *player = aethernet_player_create();
aethernet_player_open(player, "aether://content/sha256abc");
aethernet_player_play(player);
/* ... */
aethernet_player_stop(player);
aethernet_player_destroy(player);
```

---

## Estrutura do projeto

```
c/
├── include/
│   └── aethermedia.h      # API pública — único cabeçalho abrangente
├── src/
│   ├── main.c              # Ponto de entrada da CLI
│   ├── player/             # Wrapper de reprodução LibVLC
│   ├── social/             # Implementação do grafo social
│   └── streaming/          # Subscrição de stream Aether
├── tests/                  # Testes unitários CTest
└── CMakeLists.txt
```

---

## Compatibilidade de formato de wire

A implementação em C é compatível no formato de wire com a implementação de referência em C# e com todos os outros bindings de linguagem do Aether Media. Hashes de conteúdo, UHIDs de perfil e estruturas de itens de feed são idênticos em todas as plataformas.

---

## Notas de plataforma

| Plataforma | Player | Social | Feed | Streaming |
|----------|--------|--------|------|-----------|
| Linux (x86-64, ARM) | ✅ LibVLC | ✅ | ✅ | ✅ |
| macOS | ✅ LibVLC | ✅ | ✅ | ✅ |
| ESP32 / nRF52 | ❌ headless | ✅ | ✅ | ✅ |
| Windows (MinGW) | ✅ LibVLC | ✅ | ✅ | ✅ |

---

## Licença

MIT
