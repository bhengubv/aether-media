# Aether Media — C Implementation

[English](../../../../c/README.md) · [Français](../../fr/c/README.md) · [Español](../../es/c/README.md) · [العربية](../../ar/c/README.md) · [中文简体](../../zh-CN/c/README.md) · [日本語](../../ja/c/README.md) · [Deutsch](../../de/c/README.md) · [Português (BR)](../../pt-BR/c/README.md) · [Русский](README.md) · [فارسی](../../fa/c/README.md) · [한국어](../../ko/c/README.md)

Безголовая, встраиваемая реализация Aether Media на C99. Работает на одноплатных компьютерах Linux, ESP32, nRF52 и любых POSIX-системах. Обеспечивает обнаружение контента, управление социальным графом и (при наличии LibVLC) воспроизведение медиа — всё через меш-сеть Aether без подключения к интернету.

---

## Требования

- Компилятор, совместимый с C99 (GCC 10+, Clang 12+)
- CMake 3.20+
- Опционально: LibVLC (для воспроизведения; социальные функции и лента работают без него)
- Опционально: библиотека aether-protocol для C (для меш-транспорта)

---

## Сборка

```bash
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --parallel
```

### Сборка без LibVLC (только лента и социальные функции)

```bash
cmake .. -DAETHER_MEDIA_ENABLE_PLAYER=OFF
cmake --build .
```

### Кросс-компиляция для ARM (например, Raspberry Pi)

```bash
cmake .. -DCMAKE_TOOLCHAIN_FILE=../cmake/arm-linux-gnueabihf.cmake
cmake --build .
```

---

## Запуск тестов

```bash
cd build
ctest --output-on-failure
```

---

## Обзор API

Подключите единственный заголовочный файл-обёртку:

```c
#include "aethermesh_media/aethermesh_media.h"
```

### Модель контента

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

### Социальный граф

```c
AetherMeshSocialGraph *graph = aethermesh_social_graph_create();

aethermesh_social_graph_follow(graph, "peer-uhid-abc123");
aethermesh_social_graph_follow(graph, "peer-uhid-def456");

printf("following: %zu\n", aethermesh_social_graph_following_count(graph));

bool is_following = aethermesh_social_graph_is_following(graph, "peer-uhid-abc123");

aethermesh_social_graph_unfollow(graph, "peer-uhid-abc123");
aethermesh_social_graph_destroy(graph);
```

### Агрегатор ленты

```c
AetherMeshFeedAggregator *feed = aethermesh_feed_aggregator_create(500);

AetherMeshMediaFeedItem item = {0};
strncpy(item.content_hash, "sha256...", sizeof(item.content_hash) - 1);
aethermesh_feed_aggregator_push(feed, &item);

printf("feed size: %zu\n", aethermesh_feed_aggregator_size(feed));
aethermesh_feed_aggregator_destroy(feed);
```

### Плеер (требует LibVLC)

```c
AetherMeshPlayer *player = aethermesh_player_create();
aethermesh_player_open(player, "aether://content/sha256abc");
aethermesh_player_play(player);
/* ... */
aethermesh_player_stop(player);
aethermesh_player_destroy(player);
```

---

## Структура проекта

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

## Совместимость на уровне протокола

Реализация на C совместима на уровне формата данных с эталонной реализацией на C# и всеми остальными языковыми привязками Aether Media. Хэши контента, UHID профилей и структуры элементов ленты идентичны на всех платформах.

---

## Примечания по платформам

| Платформа | Плеер | Социальные функции | Лента | Стриминг |
|-----------|-------|---------------------|-------|---------|
| Linux (x86-64, ARM) | ✅ LibVLC | ✅ | ✅ | ✅ |
| macOS | ✅ LibVLC | ✅ | ✅ | ✅ |
| ESP32 / nRF52 | ❌ headless | ✅ | ✅ | ✅ |
| Windows (MinGW) | ✅ LibVLC | ✅ | ✅ | ✅ |

---

## Лицензия

MIT
