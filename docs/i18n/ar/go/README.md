<div dir="rtl">

# Aether Media — تنفيذ Go

[English](../../../../go/README.md) · [Français](../../fr/go/README.md) · [Español](../../es/go/README.md) · [العربية](README.md) · [中文简体](../../zh-CN/go/README.md) · [日本語](../../ja/go/README.md) · [Deutsch](../../de/go/README.md) · [Português (BR)](../../pt-BR/go/README.md) · [Русский](../../ru/go/README.md) · [فارسی](../../fa/go/README.md) · [한국어](../../ko/go/README.md)

تنفيذ Go لـ Aether Media يوفّر عفريت عقدة وسائط في الخلفية، ومشغّلاً من سطر الأوامر، وأداة اختبار التوافق السلكي. مناسب للخوادم عديمة الواجهة الرسومية، وأجهزة NAS، وخطوط أنابيب الوسائط البرمجية. متوافق من حيث تنسيق البروتوكول السلكي مع التنفيذ المرجعي بلغة C#.

---

## المتطلبات

- Go 1.22+
- اختياري: LibVLC (ارتباطات cgo؛ مطلوبة فقط لتشغيل `aether-media-cli`)

---

## التثبيت

```bash
go get github.com/bhengubv/aether-media/go
```

أو استنسخ وابنِ من المصدر:

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media/go
go build ./...
```

---

## الأوامر

### `aether-media-daemon` — عقدة الوسائط في الخلفية

يشغّل عقدة Aether Media دائمة تفحص المكتبة المحلية، وتنشر إعلانات المحتوى إلى الأجهزة المتاحة عبر الشبكة، وتخدم قطع المحتوى عند الطلب.

```bash
go run ./cmd/aether-media-daemon \
    --library /media/library \
    --identity ~/.aether/identity.json \
    --transport ble,wifi-direct
```

| الخيار | القيمة الافتراضية | الوصف |
|------|---------|-------------|
| `--library` | `~/Media` | المسار إلى مكتبة الوسائط المحلية |
| `--identity` | `~/.aether/identity.json` | ملف هوية AetherNetTag |
| `--transport` | `auto` | قائمة وسائل النقل مفصولة بفواصل |
| `--port` | `7420` | منفذ الاستماع لترحيل HTTP |
| `--log-level` | `info` | مستوى تفصيل السجل (`debug`، `info`، `warn`، `error`) |

### `aether-media-cli` — مشغّل سطر الأوامر

واجهة سطر أوامر تفاعلية لتصفح التغذية، وتشغيل المحتوى، وإدارة المكتبة المحلية.

```bash
go run ./cmd/aether-media-cli
```

```
Commands:
  feed          Browse the content feed from followed creators
  play <hash>   Play content by SHA-256 hash
  search <q>    Search the local library
  follow <tag>  Follow a creator by AetherNetTag
  library       List local media files
  quit
```

### `wire-roundtrip` — اختبار التشغيل البيني

يُسلسل ويُفك تسلسل هياكل النطاق الأساسية، للتحقق من توافق تنسيق البروتوكول السلكي مع تنفيذات اللغات الأخرى.

```bash
go run ./cmd/wire-roundtrip
```

---

## الحزم

| الحزمة | الوصف |
|---------|-------------|
| `models` | `MediaContent`، `MediaProfile`، `MediaFeedItem`، `MediaReaction` |
| `feed` | `FeedAggregator` — محدود بـ 500 عنصر، يُزيل التكرار حسب تجزئة المحتوى |
| `social` | `SocialGraph` — متابعة/إلغاء متابعة بـ AetherNetTag UHID |
| `streaming` | عميل Aether `IStreamingService` واشتراك البث المباشر |
| `player` | ارتباطات cgo لـ LibVLC لتشغيل الصوت والفيديو |

---

## البداية السريعة

```go
package main

import (
    "fmt"
    "github.com/bhengubv/aether-media/go/feed"
    "github.com/bhengubv/aether-media/go/models"
    "github.com/bhengubv/aether-media/go/social"
)

func main() {
    // Build a social graph
    graph := social.NewSocialGraph()
    graph.Follow("peer-uhid-abc123")
    fmt.Println("Following:", graph.FollowingCount())

    // Aggregate a feed
    agg := feed.NewFeedAggregator(500)
    item := models.MediaFeedItem{
        Content: models.MediaContent{
            ContentHash: "sha256abc",
            Title:       "Hello Mesh",
            DurationMs:  180000,
        },
    }
    agg.Push(item)
    fmt.Println("Feed size:", agg.Size())
}
```

---

## تشغيل الاختبارات

```bash
go test ./...
```

---

## توافق البروتوكول السلكي

تُسلسل جميع الهياكل إلى نفس تنسيق JSON السلكي المستخدم في التنفيذ المرجعي بلغة C#. شغّل `wire-roundtrip` على بيانات الاختبار في `../../tests/cross-language/` للتحقق:

```bash
go run ./cmd/wire-roundtrip --fixtures ../../tests/cross-language
```

---

## هيكل المشروع

```
go/
├── cmd/
│   ├── aether-media-daemon/   # Background node
│   ├── aether-media-cli/      # CLI player
│   └── wire-roundtrip/        # Interop test tool
├── feed/                      # FeedAggregator
├── models/                    # Domain structs
├── player/                    # LibVLC cgo bindings
├── social/                    # SocialGraph
├── streaming/                 # Stream subscription
└── go.mod
```

---

## الرخصة

MIT

</div>
