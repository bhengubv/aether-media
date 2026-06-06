# Aether Media — Implementação em Rust

[English](../../../../rust/README.md) · [Français](../../fr/rust/README.md) · [Español](../../es/rust/README.md) · [العربية](../../ar/rust/README.md) · [中文简体](../../zh-CN/rust/README.md) · [日本語](../../ja/rust/README.md) · [Deutsch](../../de/rust/README.md) · [Português (BR)](README.md) · [Русский](../../ru/rust/README.md) · [فارسی](../../fa/rust/README.md) · [한국어](../../ko/rust/README.md)

Uma implementação completa do Aether Media em Rust, com uma interface desktop leve (Iced/Slint), bindings FFI para o LibVLC para reprodução, e um armazenamento de feed com limite de 500 itens. Compatível em formato de serialização com a implementação de referência em C#. Adequada como fallback desktop de baixo consumo e como nó de mídia embarcado.

---

## Requisitos

- Rust 1.78+ (toolchain estável)
- Cargo
- Opcional: biblioteca compartilhada LibVLC (para reprodução; todos os demais recursos funcionam sem ela)

---

## Adicionar ao seu projeto

```toml
# Cargo.toml
[dependencies]
aether-media = "1.0.0"
```

Ou compilar a partir do código-fonte:

```bash
cargo build --release
```

---

## Executar testes

```bash
cargo test
```

### Executar benchmarks

```bash
cargo bench
```

Os benchmarks usam Criterion e geram resultados em HTML em `target/criterion/`.

---

## Início rápido

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

## Módulos

| Módulo | Descrição |
|--------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedStore` — limitado a 500 itens, deduplica por `content_hash` |
| `social` | `SocialGraph` — seguir/deixar de seguir, `is_following`, `following_count` |
| `streaming` | Cliente de stream Aether (assíncrono, tokio) |
| `player` | Bindings FFI do LibVLC (controlado por feature: `features = ["player"]`) |
| `ui` | Interface desktop Iced/Slint (controlado por feature: `features = ["ui"]`) |

---

## Features

```toml
[dependencies]
aether-media = { version = "1.0.0", features = ["player", "ui"] }
```

| Feature | Padrão | Descrição |
|---------|---------|-------------|
| `player` | desativado | Bindings FFI do LibVLC para reprodução de áudio/vídeo |
| `ui` | desativado | Interface desktop (Iced ou Slint, configurado em tempo de compilação) |

Quando compilado sem nenhuma feature, o crate fornece modelos, feed, social e streaming assíncrono — adequado para alvos headless e embarcados.

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

O LibVLC deve estar instalado no sistema host. A feature flag habilita a vinculação em tempo de compilação; o crate não será compilado com `features = ["player"]` se `libvlc` estiver ausente.

---

## Streaming assíncrono

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

## Compatibilidade de formato

A serialização usa `serde_json` com os mesmos nomes de campo da implementação de referência em C#:

```rust
let json = serde_json::to_string(&content).unwrap();
// {"contentHash":"sha256abc","title":"Hello Mesh","durationMs":180000,...}
```

---

## Estrutura do projeto

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

## Licença

MIT
