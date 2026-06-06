# Aether Media — C 구현

[English](../../../../c/README.md) · [Français](../../fr/c/README.md) · [Español](../../es/c/README.md) · [العربية](../../ar/c/README.md) · [中文简体](../../zh-CN/c/README.md) · [日本語](../../ja/c/README.md) · [Deutsch](../../de/c/README.md) · [Português (BR)](../../pt-BR/c/README.md) · [Русский](../../ru/c/README.md) · [فارسی](../../fa/c/README.md) · [한국어](README.md)

헤드리스 환경 및 임베디드 시스템을 위한 C99 기반 Aether Media 구현체입니다. Linux 싱글 보드 컴퓨터, ESP32, nRF52, 그리고 POSIX 호환 시스템에서 동작합니다. 인터넷 연결 없이 Aether 메시를 통해 콘텐츠 탐색, 소셜 그래프 관리, 그리고 LibVLC가 제공되는 환경에서 미디어 재생 기능을 제공합니다.

---

## 요구 사항

- C99 호환 컴파일러 (GCC 10+, Clang 12+)
- CMake 3.20+
- 선택 사항: LibVLC (재생용; 소셜 및 피드 기능은 없어도 동작)
- 선택 사항: aether-protocol C 라이브러리 (메시 전송용)

---

## 빌드

```bash
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --parallel
```

### LibVLC 없이 빌드 (피드 + 소셜 전용)

```bash
cmake .. -DAETHER_MEDIA_ENABLE_PLAYER=OFF
cmake --build .
```

### ARM 크로스 컴파일 (예: Raspberry Pi)

```bash
cmake .. -DCMAKE_TOOLCHAIN_FILE=../cmake/arm-linux-gnueabihf.cmake
cmake --build .
```

---

## 테스트 실행

```bash
cd build
ctest --output-on-failure
```

---

## API 개요

단일 umbrella 헤더를 포함하세요:

```c
#include "aethermedia/aethermedia.h"
```

### 콘텐츠 모델

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

### 소셜 그래프

```c
AetherNetSocialGraph *graph = aethernet_social_graph_create();

aethernet_social_graph_follow(graph, "peer-uhid-abc123");
aethernet_social_graph_follow(graph, "peer-uhid-def456");

printf("following: %zu\n", aethernet_social_graph_following_count(graph));

bool is_following = aethernet_social_graph_is_following(graph, "peer-uhid-abc123");

aethernet_social_graph_unfollow(graph, "peer-uhid-abc123");
aethernet_social_graph_destroy(graph);
```

### 피드 집계기

```c
AetherNetFeedAggregator *feed = aethernet_feed_aggregator_create(500);

AetherNetMediaFeedItem item = {0};
strncpy(item.content_hash, "sha256...", sizeof(item.content_hash) - 1);
aethernet_feed_aggregator_push(feed, &item);

printf("feed size: %zu\n", aethernet_feed_aggregator_size(feed));
aethernet_feed_aggregator_destroy(feed);
```

### 플레이어 (LibVLC 필요)

```c
AetherNetPlayer *player = aethernet_player_create();
aethernet_player_open(player, "aether://content/sha256abc");
aethernet_player_play(player);
/* ... */
aethernet_player_stop(player);
aethernet_player_destroy(player);
```

---

## 프로젝트 구조

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

## 와이어 호환성

C 구현체는 C# 참조 구현체 및 다른 모든 Aether Media 언어 바인딩과 와이어 포맷이 호환됩니다. 콘텐츠 해시, 프로필 UHID, 피드 항목 구조체는 모든 플랫폼에서 동일합니다.

---

## 플랫폼 참고 사항

| 플랫폼 | 플레이어 | 소셜 | 피드 | 스트리밍 |
|----------|--------|--------|------|-----------|
| Linux (x86-64, ARM) | ✅ LibVLC | ✅ | ✅ | ✅ |
| macOS | ✅ LibVLC | ✅ | ✅ | ✅ |
| ESP32 / nRF52 | ❌ headless | ✅ | ✅ | ✅ |
| Windows (MinGW) | ✅ LibVLC | ✅ | ✅ | ✅ |

---

## 라이선스

MIT
