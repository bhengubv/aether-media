# Aether Media — C 実装

[English](../../../../c/README.md) · [Français](../../fr/c/README.md) · [Español](../../es/c/README.md) · [العربية](../../ar/c/README.md) · [中文简体](../../zh-CN/c/README.md) · [日本語](README.md) · [Deutsch](../../de/c/README.md) · [Português (BR)](../../pt-BR/c/README.md) · [Русский](../../ru/c/README.md) · [فارسی](../../fa/c/README.md) · [한국어](../../ko/c/README.md)

Aether Media のヘッドレス対応かつ組み込みフレンドリーな C99 実装です。Linux シングルボードコンピューター、ESP32、nRF52、その他 POSIX システムで動作します。コンテンツの検出、ソーシャルグラフ管理、そして LibVLC が利用可能な環境ではメディア再生を提供します。すべての機能はインターネット接続不要の Aether メッシュ上で動作します。

---

## 要件

- C99 互換コンパイラー（GCC 10 以降、Clang 12 以降）
- CMake 3.20 以降
- オプション: LibVLC（再生に必要。ソーシャルおよびフィード機能はなくても動作）
- オプション: aether-protocol C ライブラリ（メッシュトランスポート用）

---

## ビルド

```bash
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --parallel
```

### LibVLC なしでのビルド（フィード + ソーシャルのみ）

```bash
cmake .. -DAETHER_MEDIA_ENABLE_PLAYER=OFF
cmake --build .
```

### ARM 向けクロスコンパイル（例: Raspberry Pi）

```bash
cmake .. -DCMAKE_TOOLCHAIN_FILE=../cmake/arm-linux-gnueabihf.cmake
cmake --build .
```

---

## テストの実行

```bash
cd build
ctest --output-on-failure
```

---

## API 概要

単一のアンブレラヘッダーをインクルードしてください。

```c
#include "aethermedia/aethermedia.h"
```

### コンテンツモデル

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

### ソーシャルグラフ

```c
AetherNetSocialGraph *graph = aethernet_social_graph_create();

aethernet_social_graph_follow(graph, "peer-uhid-abc123");
aethernet_social_graph_follow(graph, "peer-uhid-def456");

printf("following: %zu\n", aethernet_social_graph_following_count(graph));

bool is_following = aethernet_social_graph_is_following(graph, "peer-uhid-abc123");

aethernet_social_graph_unfollow(graph, "peer-uhid-abc123");
aethernet_social_graph_destroy(graph);
```

### フィードアグリゲーター

```c
AetherNetFeedAggregator *feed = aethernet_feed_aggregator_create(500);

AetherNetMediaFeedItem item = {0};
strncpy(item.content_hash, "sha256...", sizeof(item.content_hash) - 1);
aethernet_feed_aggregator_push(feed, &item);

printf("feed size: %zu\n", aethernet_feed_aggregator_size(feed));
aethernet_feed_aggregator_destroy(feed);
```

### プレイヤー（LibVLC 必須）

```c
AetherNetPlayer *player = aethernet_player_create();
aethernet_player_open(player, "aether://content/sha256abc");
aethernet_player_play(player);
/* ... */
aethernet_player_stop(player);
aethernet_player_destroy(player);
```

---

## プロジェクト構成

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

## ワイヤーフォーマット互換性

C 実装は C# リファレンス実装および他のすべての Aether Media 言語バインディングとワイヤーフォーマットで互換性があります。コンテンツハッシュ、プロファイル UHID、フィードアイテム構造体はすべてのプラットフォームで同一です。

---

## プラットフォーム対応状況

| プラットフォーム | プレイヤー | ソーシャル | フィード | ストリーミング |
|----------|--------|--------|------|-----------|
| Linux (x86-64、ARM) | ✅ LibVLC | ✅ | ✅ | ✅ |
| macOS | ✅ LibVLC | ✅ | ✅ | ✅ |
| ESP32 / nRF52 | ❌ ヘッドレス | ✅ | ✅ | ✅ |
| Windows (MinGW) | ✅ LibVLC | ✅ | ✅ | ✅ |

---

## ライセンス

MIT
