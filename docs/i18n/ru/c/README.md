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
#include "aethermedia/aethermedia.h"
```

### Модель контента

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

### Социальный граф

```c
AetherNetSocialGraph *graph = aethernet_social_graph_create();

aethernet_social_graph_follow(graph, "peer-uhid-abc123");
aethernet_social_graph_follow(graph, "peer-uhid-def456");

printf("following: %zu\n", aethernet_social_graph_following_count(graph));

bool is_following = aethernet_social_graph_is_following(graph, "peer-uhid-abc123");

aethernet_social_graph_unfollow(graph, "peer-uhid-abc123");
aethernet_social_graph_destroy(graph);
```

### Агрегатор ленты

```c
AetherNetFeedAggregator *feed = aethernet_feed_aggregator_create(500);

AetherNetMediaFeedItem item = {0};
strncpy(item.content_hash, "sha256...", sizeof(item.content_hash) - 1);
aethernet_feed_aggregator_push(feed, &item);

printf("feed size: %zu\n", aethernet_feed_aggregator_size(feed));
aethernet_feed_aggregator_destroy(feed);
```

### Плеер (требует LibVLC)

```c
AetherNetPlayer *player = aethernet_player_create();
aethernet_player_open(player, "aether://content/sha256abc");
aethernet_player_play(player);
/* ... */
aethernet_player_stop(player);
aethernet_player_destroy(player);
```

---

## Структура проекта

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
