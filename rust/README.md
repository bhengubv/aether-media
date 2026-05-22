# Aether Media — Rust Implementation

[English](README.md) · [Français](../docs/i18n/fr/rust/README.md) · [Español](../docs/i18n/es/rust/README.md) · [العربية](../docs/i18n/ar/rust/README.md) · [中文简体](../docs/i18n/zh-CN/rust/README.md) · [日本語](../docs/i18n/ja/rust/README.md) · [Deutsch](../docs/i18n/de/rust/README.md) · [Português (BR)](../docs/i18n/pt-BR/rust/README.md) · [Русский](../docs/i18n/ru/rust/README.md) · [فارسی](../docs/i18n/fa/rust/README.md) · [한국어](../docs/i18n/ko/rust/README.md)

A complete Rust implementation of Aether Media featuring a lightweight desktop UI (Iced/Slint), LibVLC FFI bindings for playback, and a 500-item capped feed store. Wire-format compatible with the C# reference implementation. Suitable as a low-footprint desktop fallback and as an embedded media node.

---

## Requirements

- Rust 1.78+ (stable toolchain)
- Cargo
- Optional: LibVLC shared library (for playback; all other features work without it)

---

## Add to your project

```toml
# Cargo.toml
[dependencies]
aether-media = "1.0.0"
```

Or build from source:

```bash
cargo build --release
```

---

## Run tests

```bash
cargo test
```

### Run benchmarks

```bash
cargo bench
```

Benchmarks use Criterion and report HTML results in `target/criterion/`.

---

## Quick start

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

## Modules

| Module | Description |
|--------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedStore` — capped at 500 items, deduplicates by `content_hash` |
| `social` | `SocialGraph` — follow/unfollow, `is_following`, `following_count` |
| `streaming` | Aether stream client (async, tokio) |
| `player` | LibVLC FFI bindings (feature-gated: `features = ["player"]`) |
| `ui` | Iced/Slint desktop UI (feature-gated: `features = ["ui"]`) |

---

## Features

```toml
[dependencies]
aether-media = { version = "1.0.0", features = ["player", "ui"] }
```

| Feature | Default | Description |
|---------|---------|-------------|
| `player` | off | LibVLC FFI bindings for audio/video playback |
| `ui` | off | Desktop UI (Iced or Slint, configured at build time) |

When built without any features, the crate provides models, feed, social, and async streaming — suitable for headless and embedded targets.

---

## Player (LibVLC)

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

LibVLC must be installed on the host system. The feature flag enables compile-time linking; the crate will not build with `features = ["player"]` if `libvlc` is absent.

---

## Async streaming

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

## Wire compatibility

Serialisation uses `serde_json` with the same field names as the C# reference implementation:

```rust
let json = serde_json::to_string(&content).unwrap();
// {"contentHash":"sha256abc","title":"Hello Mesh","durationMs":180000,...}
```

---

## Project layout

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

## License

MIT
