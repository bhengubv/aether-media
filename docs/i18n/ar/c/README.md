<div dir="rtl">

# Aether Media — تنفيذ C

[English](../../../../c/README.md) · [Français](../../fr/c/README.md) · [Español](../../es/c/README.md) · [العربية](README.md) · [中文简体](../../zh-CN/c/README.md) · [日本語](../../ja/c/README.md) · [Deutsch](../../de/c/README.md) · [Português (BR)](../../pt-BR/c/README.md) · [Русский](../../ru/c/README.md) · [فارسی](../../fa/c/README.md) · [한국어](../../ko/c/README.md)

تنفيذ C99 مجرّد من الواجهة الرسومية ومناسب للأنظمة المدمجة. يعمل على أجهزة Linux ذات اللوحة الواحدة، وESP32، وnRF52، وأي نظام POSIX. يوفّر اكتشاف المحتوى، وإدارة الرسم الاجتماعي، وتشغيل الوسائط (حيثما توفرت LibVLC) — كل ذلك عبر شبكة Aether اللاسلكية دون الحاجة إلى اتصال بالإنترنت.

---

## المتطلبات

- مُترجم متوافق مع C99 (GCC 10+، Clang 12+)
- CMake 3.20+
- اختياري: LibVLC (للتشغيل؛ تعمل وظائف الشبكة الاجتماعية والتغذية بدونها)
- اختياري: مكتبة aether-protocol لـ C (لنقل الشبكة اللاسلكية)

---

## البناء

```bash
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --parallel
```

### البناء بدون LibVLC (التغذية والشبكة الاجتماعية فقط)

```bash
cmake .. -DAETHER_MEDIA_ENABLE_PLAYER=OFF
cmake --build .
```

### التحويل المتقاطع لـ ARM (مثلاً Raspberry Pi)

```bash
cmake .. -DCMAKE_TOOLCHAIN_FILE=../cmake/arm-linux-gnueabihf.cmake
cmake --build .
```

---

## تشغيل الاختبارات

```bash
cd build
ctest --output-on-failure
```

---

## نظرة عامة على واجهة برمجة التطبيقات

أدرج ملف الرأس الشامل الوحيد:

```c
#include "aethermedia/aethermedia.h"
```

### نموذج المحتوى

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

### الرسم الاجتماعي

```c
AetherNetSocialGraph *graph = aethernet_social_graph_create();

aethernet_social_graph_follow(graph, "peer-uhid-abc123");
aethernet_social_graph_follow(graph, "peer-uhid-def456");

printf("following: %zu\n", aethernet_social_graph_following_count(graph));

bool is_following = aethernet_social_graph_is_following(graph, "peer-uhid-abc123");

aethernet_social_graph_unfollow(graph, "peer-uhid-abc123");
aethernet_social_graph_destroy(graph);
```

### مُجمّع التغذية

```c
AetherNetFeedAggregator *feed = aethernet_feed_aggregator_create(500);

AetherNetMediaFeedItem item = {0};
strncpy(item.content_hash, "sha256...", sizeof(item.content_hash) - 1);
aethernet_feed_aggregator_push(feed, &item);

printf("feed size: %zu\n", aethernet_feed_aggregator_size(feed));
aethernet_feed_aggregator_destroy(feed);
```

### المشغّل (يتطلب LibVLC)

```c
AetherNetPlayer *player = aethernet_player_create();
aethernet_player_open(player, "aether://content/sha256abc");
aethernet_player_play(player);
/* ... */
aethernet_player_stop(player);
aethernet_player_destroy(player);
```

---

## هيكل المشروع

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

## توافق البروتوكول السلكي

تنفيذ C متوافق من حيث تنسيق البروتوكول السلكي مع التنفيذ المرجعي بلغة C# وجميع ارتباطات لغات Aether Media الأخرى. تجزئات المحتوى، ومعرّفات UHID للملفات الشخصية، وهياكل عناصر التغذية متطابقة عبر جميع المنصات.

---

## ملاحظات المنصات

| المنصة | المشغّل | الشبكة الاجتماعية | التغذية | البث |
|----------|--------|--------|------|-----------|
| Linux (x86-64, ARM) | ✅ LibVLC | ✅ | ✅ | ✅ |
| macOS | ✅ LibVLC | ✅ | ✅ | ✅ |
| ESP32 / nRF52 | ❌ headless | ✅ | ✅ | ✅ |
| Windows (MinGW) | ✅ LibVLC | ✅ | ✅ | ✅ |

---

## الرخصة

MIT

</div>
