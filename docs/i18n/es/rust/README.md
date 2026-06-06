# Aether Media — Implementación en Rust

[English](../../../../rust/README.md) · [Français](../../fr/rust/README.md) · [Español](README.md) · [العربية](../../ar/rust/README.md) · [中文简体](../../zh-CN/rust/README.md) · [日本語](../../ja/rust/README.md) · [Deutsch](../../de/rust/README.md) · [Português (BR)](../../pt-BR/rust/README.md) · [Русский](../../ru/rust/README.md) · [فارسی](../../fa/rust/README.md) · [한국어](../../ko/rust/README.md)

Una implementación completa en Rust de Aether Media con una interfaz de escritorio ligera (Iced/Slint), enlaces FFI a LibVLC para reproducción, y un almacén de feeds limitado a 500 elementos. Compatible en formato de cable con la implementación de referencia en C#. Adecuado como alternativa de escritorio de bajo consumo y como nodo multimedia embebido.

---

## Requisitos

- Rust 1.78+ (toolchain estable)
- Cargo
- Opcional: biblioteca compartida LibVLC (para reproducción; todas las demás funciones funcionan sin ella)

---

## Añadir a tu proyecto

```toml
# Cargo.toml
[dependencies]
aether-media = "1.0.0"
```

O compilar desde el código fuente:

```bash
cargo build --release
```

---

## Ejecutar pruebas

```bash
cargo test
```

### Ejecutar benchmarks

```bash
cargo bench
```

Los benchmarks usan Criterion y generan resultados HTML en `target/criterion/`.

---

## Inicio rápido

```rust
use aethernet_media::{
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

## Módulos

| Módulo | Descripción |
|--------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedStore` — limitado a 500 elementos, deduplica por `content_hash` |
| `social` | `SocialGraph` — seguir/dejar de seguir, `is_following`, `following_count` |
| `streaming` | Cliente de stream de Aether (asíncrono, tokio) |
| `player` | Enlaces FFI a LibVLC (habilitado por feature: `features = ["player"]`) |
| `ui` | Interfaz de escritorio Iced/Slint (habilitada por feature: `features = ["ui"]`) |

---

## Features

```toml
[dependencies]
aether-media = { version = "1.0.0", features = ["player", "ui"] }
```

| Feature | Por defecto | Descripción |
|---------|---------|-------------|
| `player` | desactivado | Enlaces FFI a LibVLC para reproducción de audio/vídeo |
| `ui` | desactivado | Interfaz de escritorio (Iced o Slint, configurado en tiempo de compilación) |

Cuando se compila sin ninguna feature, el crate proporciona modelos, feed, grafo social y streaming asíncrono — adecuado para destinos sin cabeza y embebidos.

---

## Reproductor (LibVLC)

```rust
use aethernet_media::player::Player;

#[tokio::main]
async fn main() {
    let mut player = Player::new().expect("LibVLC not found");
    player.open("aether://content/sha256abc").await.unwrap();
    player.play();
    tokio::time::sleep(std::time::Duration::from_secs(5)).await;
    player.stop();
}
```

LibVLC debe estar instalado en el sistema anfitrión. El flag de feature habilita el enlace en tiempo de compilación; el crate no compilará con `features = ["player"]` si `libvlc` está ausente.

---

## Streaming asíncrono

```rust
use aethernet_media::streaming::StreamClient;

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

## Compatibilidad de cable

La serialización utiliza `serde_json` con los mismos nombres de campo que la implementación de referencia en C#:

```rust
let json = serde_json::to_string(&content).unwrap();
// {"contentHash":"sha256abc","title":"Hello Mesh","durationMs":180000,...}
```

---

## Estructura del proyecto

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

## Licencia

MIT
