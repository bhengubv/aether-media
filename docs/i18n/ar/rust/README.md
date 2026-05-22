<div dir="rtl">

# Aether Media — تنفيذ Rust

[English](../../../../rust/README.md) · [Français](../../fr/rust/README.md) · [Español](../../es/rust/README.md) · [العربية](README.md) · [中文简体](../../zh-CN/rust/README.md) · [日本語](../../ja/rust/README.md) · [Deutsch](../../de/rust/README.md) · [Português (BR)](../../pt-BR/rust/README.md) · [Русский](../../ru/rust/README.md) · [فارسی](../../fa/rust/README.md) · [한국어](../../ko/rust/README.md)

تنفيذ كامل بلغة Rust لـ Aether Media يتضمن واجهة مستخدم سطح مكتبي خفيفة الوزن (Iced/Slint)، وارتباطات FFI بـ LibVLC للتشغيل، ومخزن خلاصة محدود بـ 500 عنصر. متوافق مع تنسيق السلك مع التنفيذ المرجعي بـ C#. مناسب كبديل سطح مكتبي منخفض البصمة وكعقدة وسائط مدمجة.

---

## المتطلبات

- Rust 1.78+ (سلسلة أدوات stable)
- Cargo
- اختياري: مكتبة LibVLC المشتركة (للتشغيل؛ جميع الميزات الأخرى تعمل بدونها)

---

## إضافة المكتبة إلى مشروعك

```toml
# Cargo.toml
[dependencies]
aether-media = "1.0.0"
```

أو قم بالبناء من المصدر:

```bash
cargo build --release
```

---

## تشغيل الاختبارات

```bash
cargo test
```

### تشغيل المعايير المرجعية

```bash
cargo bench
```

تستخدم المعايير المرجعية Criterion وتُصدر نتائج HTML في `target/criterion/`.

---

## البدء السريع

```rust
use aether_media::{
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

## الوحدات

| الوحدة | الوصف |
|--------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedStore` — محدود بـ 500 عنصر، يُزيل التكرارات بحسب `content_hash` |
| `social` | `SocialGraph` — متابعة/إلغاء متابعة، `is_following`، `following_count` |
| `streaming` | عميل بث Aether (غير متزامن، tokio) |
| `player` | ارتباطات FFI بـ LibVLC (مقيَّدة بالميزة: `features = ["player"]`) |
| `ui` | واجهة مستخدم سطح مكتبي Iced/Slint (مقيَّدة بالميزة: `features = ["ui"]`) |

---

## الميزات

```toml
[dependencies]
aether-media = { version = "1.0.0", features = ["player", "ui"] }
```

| الميزة | الافتراضي | الوصف |
|---------|---------|-------------|
| `player` | معطلة | ارتباطات FFI بـ LibVLC لتشغيل الصوت والفيديو |
| `ui` | معطلة | واجهة مستخدم سطح مكتبي (Iced أو Slint، يُهيَّأ عند وقت البناء) |

عند البناء بدون أي ميزات، توفر الحزمة النماذج والخلاصة والشبكة الاجتماعية والبث غير المتزامن — مناسبة للأهداف بلا رأس والمضمنة.

---

## المشغّل (LibVLC)

```rust
use aether_media::player::Player;

#[tokio::main]
async fn main() {
    let mut player = Player::new().expect("LibVLC not found");
    player.open("aether://content/sha256abc").await.unwrap();
    player.play();
    tokio::time::sleep(std::time::Duration::from_secs(5)).await;
    player.stop();
}
```

يجب تثبيت LibVLC على النظام المضيف. يُتيح علم الميزة الربط في وقت الترجمة؛ لن تُبنى الحزمة مع `features = ["player"]` إذا كان `libvlc` غائباً.

---

## البث غير المتزامن

```rust
use aether_media::streaming::StreamClient;

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

## التوافق مع السلك

تستخدم التسلسلية `serde_json` بأسماء حقول مطابقة للتنفيذ المرجعي بـ C#:

```rust
let json = serde_json::to_string(&content).unwrap();
// {"contentHash":"sha256abc","title":"Hello Mesh","durationMs":180000,...}
```

---

## تخطيط المشروع

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

## الرخصة

MIT

</div>
