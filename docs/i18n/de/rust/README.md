# Aether Media — Rust-Implementierung

[English](../../../../rust/README.md) · [Français](../../fr/rust/README.md) · [Español](../../es/rust/README.md) · [العربية](../../ar/rust/README.md) · [中文简体](../../zh-CN/rust/README.md) · [日本語](../../ja/rust/README.md) · [Deutsch](README.md) · [Português (BR)](../../pt-BR/rust/README.md) · [Русский](../../ru/rust/README.md) · [فارسی](../../fa/rust/README.md) · [한국어](../../ko/rust/README.md)

Eine vollständige Rust-Implementierung von Aether Media mit einer schlanken Desktop-Benutzeroberfläche (Iced/Slint), LibVLC-FFI-Bindungen für die Wiedergabe und einem auf 500 Einträge begrenzten Feed-Speicher. Wire-Format-kompatibel mit der C#-Referenzimplementierung. Geeignet als schlanke Desktop-Fallback-Lösung und als eingebetteter Medienknoten.

---

## Voraussetzungen

- Rust 1.78+ (stable Toolchain)
- Cargo
- Optional: gemeinsam genutzte LibVLC-Bibliothek (für die Wiedergabe; alle anderen Funktionen sind ohne sie nutzbar)

---

## Zum Projekt hinzufügen

```toml
# Cargo.toml
[dependencies]
aether-media = "1.0.0"
```

Oder aus dem Quellcode erstellen:

```bash
cargo build --release
```

---

## Tests ausführen

```bash
cargo test
```

### Benchmarks ausführen

```bash
cargo bench
```

Benchmarks verwenden Criterion und erzeugen HTML-Ergebnisse unter `target/criterion/`.

---

## Schnellstart

```rust
use aethermesh_media::{
    models::{MediaContent, MediaFeedItem},
    feed::FeedStore,
    social::SocialGraph,
};

fn main() {
    // Sozialen Graphen aufbauen
    let mut graph = SocialGraph::new();
    graph.follow("peer-uhid-abc123");
    graph.follow("peer-uhid-def456");
    println!("Following: {}", graph.following_count()); // 2

    // Feed befüllen
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

    // Dauerformatierung
    let c = MediaContent { duration_ms: 90_000, ..Default::default() };
    println!("{}", c.formatted_duration()); // "1:30"
}
```

---

## Module

| Modul | Beschreibung |
|--------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedStore` — auf 500 Einträge begrenzt, dedupliziert nach `content_hash` |
| `social` | `SocialGraph` — Folgen/Entfolgen, `is_following`, `following_count` |
| `streaming` | Aether-Stream-Client (asynchron, tokio) |
| `player` | LibVLC-FFI-Bindungen (feature-gesteuert: `features = ["player"]`) |
| `ui` | Iced/Slint-Desktop-UI (feature-gesteuert: `features = ["ui"]`) |

---

## Features

```toml
[dependencies]
aether-media = { version = "1.0.0", features = ["player", "ui"] }
```

| Feature | Standard | Beschreibung |
|---------|---------|-------------|
| `player` | deaktiviert | LibVLC-FFI-Bindungen für Audio-/Videowiedergabe |
| `ui` | deaktiviert | Desktop-UI (Iced oder Slint, zur Build-Zeit konfiguriert) |

Wird das Crate ohne Features gebaut, stellt es Modelle, Feed, Social und asynchrones Streaming bereit — geeignet für Headless- und Embedded-Ziele.

---

## Player (LibVLC)

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

LibVLC muss auf dem Hostsystem installiert sein. Das Feature-Flag aktiviert die Verlinkung zur Compile-Zeit; das Crate lässt sich nicht mit `features = ["player"]` bauen, wenn `libvlc` fehlt.

---

## Asynchrones Streaming

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

## Wire-Kompatibilität

Die Serialisierung verwendet `serde_json` mit denselben Feldnamen wie die C#-Referenzimplementierung:

```rust
let json = serde_json::to_string(&content).unwrap();
// {"contentHash":"sha256abc","title":"Hello Mesh","durationMs":180000,...}
```

---

## Projektstruktur

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

## Lizenz

MIT
