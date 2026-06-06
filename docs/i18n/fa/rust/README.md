<div dir="rtl">

# Aether Media — پیاده‌سازی Rust

[English](../../../../rust/README.md) · [Français](../../fr/rust/README.md) · [Español](../../es/rust/README.md) · [العربية](../../ar/rust/README.md) · [中文简体](../../zh-CN/rust/README.md) · [日本語](../../ja/rust/README.md) · [Deutsch](../../de/rust/README.md) · [Português (BR)](../../pt-BR/rust/README.md) · [Русский](../../ru/rust/README.md) · [فارسی](README.md) · [한국어](../../ko/rust/README.md)

یک پیاده‌سازی کامل Rust از Aether Media که شامل یک رابط کاربری دسکتاپ سبک‌وزن (Iced/Slint)، اتصالات FFI برای LibVLC جهت پخش، و یک فروشگاه فید با حداکثر ۵۰۰ آیتم است. از نظر فرمت انتقالی با پیاده‌سازی مرجع C# سازگار است. مناسب به عنوان جایگزین دسکتاپ با اثر کم و به عنوان یک گره رسانه‌ای تعبیه‌شده.

---

## پیش‌نیازها

- Rust 1.78+ (زنجیره ابزار stable)
- Cargo
- اختیاری: کتابخانه به‌اشتراک‌گذاشته‌شده LibVLC (برای پخش؛ تمام ویژگی‌های دیگر بدون آن کار می‌کنند)

---

## افزودن به پروژه

```toml
# Cargo.toml
[dependencies]
aether-media = "1.0.0"
```

یا ساخت از سورس:

```bash
cargo build --release
```

---

## اجرای تست‌ها

```bash
cargo test
```

### اجرای بنچمارک‌ها

```bash
cargo bench
```

بنچمارک‌ها از Criterion استفاده می‌کنند و نتایج HTML را در `target/criterion/` گزارش می‌دهند.

---

## شروع سریع

```rust
use aethermesh_media::{
    models::{MediaContent, MediaFeedItem},
    feed::FeedStore,
    social::SocialGraph,
};

fn main() {
    // Build a social graph
    let mut graph = SocialGraph::new();
    graph.follow("peer-uhid-abc123");
    graph.follow("peer-uhid-def456");
    println!("Following: {}", graph.following_count()); // 2

    // Accumulate a feed
    let mut feed = FeedStore::new(500);
    let content = MediaContent {
        content_hash: "sha256abc".to_string(),
        title: "Hello Mesh".to_string(),
        duration_ms: 180_000,
        codec: "h264".to_string(),
        content_type: "video/mp4".to_string(),
        creator_uhid: "uhid-xyz".to_string(),
        size_bytes: 52_428_800,
    };
    feed.push(MediaFeedItem { content, ..Default::default() });
    println!("Feed size: {}", feed.len()); // 1

    // Duration formatting
    let c = MediaContent { duration_ms: 90_000, ..Default::default() };
    println!("{}", c.formatted_duration()); // "1:30"
}
```

---

## ماژول‌ها

| ماژول | توضیحات |
|--------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedStore` — با حداکثر ۵۰۰ آیتم، تکراری‌ها را بر اساس `content_hash` حذف می‌کند |
| `social` | `SocialGraph` — فالو/آنفالو، `is_following`، `following_count` |
| `streaming` | کلاینت استریم Aether (ناهمزمان، tokio) |
| `player` | اتصالات FFI برای LibVLC (با feature-gate: `features = ["player"]`) |
| `ui` | رابط کاربری دسکتاپ Iced/Slint (با feature-gate: `features = ["ui"]`) |

---

## ویژگی‌ها

```toml
[dependencies]
aether-media = { version = "1.0.0", features = ["player", "ui"] }
```

| ویژگی | پیش‌فرض | توضیحات |
|---------|---------|-------------|
| `player` | خاموش | اتصالات FFI برای LibVLC جهت پخش صدا و تصویر |
| `ui` | خاموش | رابط کاربری دسکتاپ (Iced یا Slint، در زمان ساخت تنظیم می‌شود) |

وقتی بدون هیچ ویژگی‌ای ساخته می‌شود، crate مدل‌ها، فید، شبکه اجتماعی، و استریم ناهمزمان را فراهم می‌کند — مناسب برای اهداف headless و تعبیه‌شده.

---

## پخش‌کننده (LibVLC)

```rust
use aethermesh_media::player::Player;

#[tokio::main]
async fn main() {
    let mut player = Player::new().expect("LibVLC not found");
    player.open("aether://content/sha256abc").await.unwrap();
    player.play();
    tokio::time::sleep(std::time::Duration::from_secs(5)).await;
    player.stop();
}
```

LibVLC باید روی سیستم میزبان نصب باشد. پرچم feature اتصال در زمان کامپایل را فعال می‌کند؛ اگر `libvlc` وجود نداشته باشد، crate با `features = ["player"]` ساخته نخواهد شد.

---

## استریم ناهمزمان

```rust
use aethermesh_media::streaming::StreamClient;

#[tokio::main]
async fn main() {
    let client = StreamClient::connect("uhid-host-abc123").await.unwrap();
    let mut segments = client.subscribe().await;

    while let Some(segment) = segments.recv().await {
        println!("Segment {} ({} bytes)", segment.index, segment.data.len());
    }
}
```

---

## سازگاری انتقالی

سریال‌سازی از `serde_json` با همان نام‌های فیلد پیاده‌سازی مرجع C# استفاده می‌کند:

```rust
let json = serde_json::to_string(&content).unwrap();
// {"contentHash":"sha256abc","title":"Hello Mesh","durationMs":180000,...}
```

---

## ساختار پروژه

```
rust/
├── src/
│   ├── lib.rs          # Crate root, feature re-exports
│   ├── models.rs       # Domain structs + serde derives
│   ├── feed.rs         # FeedStore
│   ├── social.rs       # SocialGraph
│   ├── player/         # LibVLC FFI (feature = "player")
│   ├── streaming/      # Async stream client
│   └── ui/             # Desktop UI (feature = "ui")
├── benches/
│   └── bench_feed.rs   # Criterion benchmark
└── Cargo.toml
```

---

## مجوز

MIT

</div>
