<div dir="rtl">

# Aether Media — پیاده‌سازی Go

[English](../../../../go/README.md) · [Français](../../fr/go/README.md) · [Español](../../es/go/README.md) · [العربية](../../ar/go/README.md) · [中文简体](../../zh-CN/go/README.md) · [日本語](../../ja/go/README.md) · [Deutsch](../../de/go/README.md) · [Português (BR)](../../pt-BR/go/README.md) · [Русский](../../ru/go/README.md) · [فارسی](README.md) · [한국어](../../ko/go/README.md)

یک پیاده‌سازی Go از Aether Media که یک دیمن گره رسانه‌ای پس‌زمینه، یک پخش‌کننده خط فرمان و یک ابزار آزمون چرخه‌ی رفت‌وبرگشت سیمی ارائه می‌دهد. مناسب برای سرورهای بدون رابط گرافیکی، دستگاه‌های NAS و خطوط لوله رسانه‌ای اسکریپت‌شده. از نظر فرمت سیمی با پیاده‌سازی مرجع C# سازگار است.

---

## پیش‌نیازها

- Go 1.22+
- اختیاری: LibVLC (اتصالات cgo؛ فقط برای پخش `aether-media-cli` مورد نیاز است)

---

## نصب

```bash
go get github.com/bhengubv/aether-media/go
```

یا کلون کرده و از سورس بسازید:

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media/go
go build ./...
```

---

## دستورات

### `aether-media-daemon` — گره رسانه‌ای پس‌زمینه

یک گره Aether Media پایدار اجرا می‌کند که کتابخانه محلی را اسکن می‌کند، اعلان‌های محتوا را به همتایان مِش ارسال می‌کند و قطعه‌های محتوا را بنا به درخواست سرویس می‌دهد.

```bash
go run ./cmd/aether-media-daemon \
    --library /media/library \
    --identity ~/.aether/identity.json \
    --transport ble,wifi-direct
```

| پرچم | پیش‌فرض | توضیح |
|------|---------|-------------|
| `--library` | `~/Media` | مسیر کتابخانه رسانه محلی |
| `--identity` | `~/.aether/identity.json` | فایل هویت AetherNetTag |
| `--transport` | `auto` | لیست انتقال جداشده با کاما |
| `--port` | `7420` | پورت شنود رله HTTP |
| `--log-level` | `info` | سطح جزئیات لاگ (`debug`، `info`، `warn`، `error`) |

### `aether-media-cli` — پخش‌کننده خط فرمان

CLI تعاملی برای مرور فید، پخش محتوا و مدیریت کتابخانه محلی.

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

### `wire-roundtrip` — آزمون قابلیت همکاری

ساختارهای دامنه اصلی را سریالایز و دی‌سریالایز می‌کند و سازگاری فرمت سیمی با سایر پیاده‌سازی‌های زبانی را تأیید می‌کند.

```bash
go run ./cmd/wire-roundtrip
```

---

## پکیج‌ها

| پکیج | توضیح |
|---------|-------------|
| `models` | `MediaContent`، `MediaProfile`، `MediaFeedItem`، `MediaReaction` |
| `feed` | `FeedAggregator` — محدود به ۵۰۰ آیتم، با حذف تکراری بر اساس هش محتوا |
| `social` | `SocialGraph` — دنبال‌کردن/لغو دنبال‌کردن با UHID AetherNetTag |
| `streaming` | کلاینت `IStreamingService` Aether و اشتراک پخش زنده |
| `player` | اتصالات cgo LibVLC برای پخش صدا/ویدیو |

---

## شروع سریع

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

## اجرای آزمون‌ها

```bash
go test ./...
```

---

## سازگاری فرمت سیمی

تمام ساختارها به همان فرمت JSON سیمی استفاده‌شده توسط پیاده‌سازی مرجع C# سریالایز می‌شوند. برای تأیید، `wire-roundtrip` را روی فیکسچرهای آزمون در `../../tests/cross-language/` اجرا کنید:

```bash
go run ./cmd/wire-roundtrip --fixtures ../../tests/cross-language
```

---

## ساختار پروژه

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

## مجوز

MIT

</div>
