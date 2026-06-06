# Aether Media — C 实现

[English](../../../../c/README.md) · [Français](../../fr/c/README.md) · [Español](../../es/c/README.md) · [العربية](../../ar/c/README.md) · [中文简体](README.md) · [日本語](../../ja/c/README.md) · [Deutsch](../../de/c/README.md) · [Português (BR)](../../pt-BR/c/README.md) · [Русский](../../ru/c/README.md) · [فارسی](../../fa/c/README.md) · [한국어](../../ko/c/README.md)

Aether Media 的无头、嵌入式友好型 C99 实现。可在 Linux 单板计算机、ESP32、nRF52 及任何 POSIX 系统上运行。提供内容发现、社交图谱管理，以及（在 LibVLC 可用时的）媒体播放功能——均通过 Aether 网状网络运行，无需网络连接。

---

## 环境要求

- 兼容 C99 的编译器（GCC 10+，Clang 12+）
- CMake 3.20+
- 可选：LibVLC（用于播放；社交和 Feed 功能无需此项）
- 可选：aether-protocol C 库（用于网状传输）

---

## 构建

```bash
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --parallel
```

### 不含 LibVLC 的构建（仅 Feed + 社交）

```bash
cmake .. -DAETHER_MEDIA_ENABLE_PLAYER=OFF
cmake --build .
```

### 针对 ARM 的交叉编译（例如 Raspberry Pi）

```bash
cmake .. -DCMAKE_TOOLCHAIN_FILE=../cmake/arm-linux-gnueabihf.cmake
cmake --build .
```

---

## 运行测试

```bash
cd build
ctest --output-on-failure
```

---

## API 概览

引入单一总括头文件：

```c
#include "aethernet_media/aethernet_media.h"
```

### 内容模型

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

### 社交图谱

```c
AetherNetSocialGraph *graph = aethernet_social_graph_create();

aethernet_social_graph_follow(graph, "peer-uhid-abc123");
aethernet_social_graph_follow(graph, "peer-uhid-def456");

printf("following: %zu\n", aethernet_social_graph_following_count(graph));

bool is_following = aethernet_social_graph_is_following(graph, "peer-uhid-abc123");

aethernet_social_graph_unfollow(graph, "peer-uhid-abc123");
aethernet_social_graph_destroy(graph);
```

### Feed 聚合器

```c
AetherNetFeedAggregator *feed = aethernet_feed_aggregator_create(500);

AetherNetMediaFeedItem item = {0};
strncpy(item.content_hash, "sha256...", sizeof(item.content_hash) - 1);
aethernet_feed_aggregator_push(feed, &item);

printf("feed size: %zu\n", aethernet_feed_aggregator_size(feed));
aethernet_feed_aggregator_destroy(feed);
```

### 播放器（需要 LibVLC）

```c
AetherNetPlayer *player = aethernet_player_create();
aethernet_player_open(player, "aether://content/sha256abc");
aethernet_player_play(player);
/* ... */
aethernet_player_stop(player);
aethernet_player_destroy(player);
```

---

## 项目结构

```
c/
├── include/
│   └── aethernet_media.h      # Public API — single umbrella header
├── src/
│   ├── main.c              # CLI entry point
│   ├── player/             # LibVLC playback wrapper
│   ├── social/             # Social graph implementation
│   └── streaming/          # Aether stream subscription
├── tests/                  # CTest unit tests
└── CMakeLists.txt
```

---

## 线路格式兼容性

C 实现与 C# 参考实现及所有其他 Aether Media 语言绑定在线路格式上完全兼容。内容哈希、配置文件 UHID 和 Feed 条目结构在所有平台上保持一致。

---

## 平台说明

| 平台 | 播放器 | 社交 | Feed | 流媒体 |
|----------|--------|--------|------|-----------|
| Linux (x86-64, ARM) | ✅ LibVLC | ✅ | ✅ | ✅ |
| macOS | ✅ LibVLC | ✅ | ✅ | ✅ |
| ESP32 / nRF52 | ❌ 无头模式 | ✅ | ✅ | ✅ |
| Windows (MinGW) | ✅ LibVLC | ✅ | ✅ | ✅ |

---

## 许可证

MIT
