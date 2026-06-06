<div dir="rtl">

# Aether Media — پیاده‌سازی C

[English](../../../../c/README.md) · [Français](../../fr/c/README.md) · [Español](../../es/c/README.md) · [العربية](../../ar/c/README.md) · [中文简体](../../zh-CN/c/README.md) · [日本語](../../ja/c/README.md) · [Deutsch](../../de/c/README.md) · [Português (BR)](../../pt-BR/c/README.md) · [Русский](../../ru/c/README.md) · [فارسی](README.md) · [한국어](../../ko/c/README.md)

یک پیاده‌سازی C99 بدون رابط گرافیکی و مناسب برای سیستم‌های جاسازی‌شده از Aether Media. روی رایانه‌های تک‌برده لینوکسی، ESP32، nRF52 و هر سیستم POSIX اجرا می‌شود. کشف محتوا، مدیریت گراف اجتماعی و (در صورت وجود LibVLC) پخش رسانه را — همه از طریق مِش Aether و بدون نیاز به اینترنت — فراهم می‌کند.

---

## پیش‌نیازها

- کامپایلر سازگار با C99 (GCC 10+، Clang 12+)
- CMake 3.20+
- اختیاری: LibVLC (برای پخش؛ توابع اجتماعی و فید بدون آن هم کار می‌کنند)
- اختیاری: کتابخانه C پروتکل aether (برای انتقال مِش)

---

## ساخت

```bash
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --parallel
```

### ساخت بدون LibVLC (فقط فید + اجتماعی)

```bash
cmake .. -DAETHER_MEDIA_ENABLE_PLAYER=OFF
cmake --build .
```

### کامپایل متقاطع برای ARM (مثلاً Raspberry Pi)

```bash
cmake .. -DCMAKE_TOOLCHAIN_FILE=../cmake/arm-linux-gnueabihf.cmake
cmake --build .
```

---

## اجرای آزمون‌ها

```bash
cd build
ctest --output-on-failure
```

---

## مرور کلی API

هدر یکپارچه را وارد کنید:

```c
#include "aethernet_media/aethernet_media.h"
```

### مدل محتوا

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

### گراف اجتماعی

```c
AetherNetSocialGraph *graph = aethernet_social_graph_create();

aethernet_social_graph_follow(graph, "peer-uhid-abc123");
aethernet_social_graph_follow(graph, "peer-uhid-def456");

printf("following: %zu\n", aethernet_social_graph_following_count(graph));

bool is_following = aethernet_social_graph_is_following(graph, "peer-uhid-abc123");

aethernet_social_graph_unfollow(graph, "peer-uhid-abc123");
aethernet_social_graph_destroy(graph);
```

### تجمیع‌کننده فید

```c
AetherNetFeedAggregator *feed = aethernet_feed_aggregator_create(500);

AetherNetMediaFeedItem item = {0};
strncpy(item.content_hash, "sha256...", sizeof(item.content_hash) - 1);
aethernet_feed_aggregator_push(feed, &item);

printf("feed size: %zu\n", aethernet_feed_aggregator_size(feed));
aethernet_feed_aggregator_destroy(feed);
```

### پخش‌کننده (نیازمند LibVLC)

```c
AetherNetPlayer *player = aethernet_player_create();
aethernet_player_open(player, "aether://content/sha256abc");
aethernet_player_play(player);
/* ... */
aethernet_player_stop(player);
aethernet_player_destroy(player);
```

---

## ساختار پروژه

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

## سازگاری فرمت سیمی

پیاده‌سازی C از نظر فرمت سیمی با پیاده‌سازی مرجع C# و تمام اتصالات زبانی دیگر Aether Media سازگار است. هش‌های محتوا، UHID پروفایل‌ها و ساختارهای آیتم فید در تمام پلتفرم‌ها یکسان هستند.

---

## یادداشت‌های پلتفرم

| پلتفرم | پخش‌کننده | اجتماعی | فید | استریمینگ |
|----------|--------|--------|------|-----------|
| Linux (x86-64, ARM) | ✅ LibVLC | ✅ | ✅ | ✅ |
| macOS | ✅ LibVLC | ✅ | ✅ | ✅ |
| ESP32 / nRF52 | ❌ headless | ✅ | ✅ | ✅ |
| Windows (MinGW) | ✅ LibVLC | ✅ | ✅ | ✅ |

---

## مجوز

MIT

</div>
