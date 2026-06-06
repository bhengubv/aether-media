# Aether Media — Implémentation Rust

[English](../../../../rust/README.md) · [Français](README.md) · [Español](../../es/rust/README.md) · [العربية](../../ar/rust/README.md) · [中文简体](../../zh-CN/rust/README.md) · [日本語](../../ja/rust/README.md) · [Deutsch](../../de/rust/README.md) · [Português (BR)](../../pt-BR/rust/README.md) · [Русский](../../ru/rust/README.md) · [فارسی](../../fa/rust/README.md) · [한국어](../../ko/rust/README.md)

Une implémentation Rust complète d'Aether Media avec une interface bureau légère (Iced/Slint), des liaisons FFI LibVLC pour la lecture et un magasin de flux limité à 500 éléments. Compatible au niveau du format filaire avec l'implémentation de référence C#. Convient comme solution bureau à faible empreinte et comme nœud multimédia embarqué.

---

## Prérequis

- Rust 1.78+ (chaîne d'outils stable)
- Cargo
- Optionnel : bibliothèque partagée LibVLC (pour la lecture ; toutes les autres fonctionnalités fonctionnent sans elle)

---

## Ajouter à votre projet

```toml
# Cargo.toml
[dependencies]
aether-media = "1.0.0"
```

Ou compiler depuis les sources :

```bash
cargo build --release
```

---

## Exécuter les tests

```bash
cargo test
```

### Exécuter les benchmarks

```bash
cargo bench
```

Les benchmarks utilisent Criterion et produisent des résultats HTML dans `target/criterion/`.

---

## Démarrage rapide

```rust
use aethermesh_media::{
    models::{MediaContent, MediaFeedItem},
    feed::FeedStore,
    social::SocialGraph,
};

fn main() {
    // Construire un graphe social
    let mut graph = SocialGraph::new();
    graph.follow("peer-uhid-abc123");
    graph.follow("peer-uhid-def456");
    println!("Following: {}", graph.following_count()); // 2

    // Accumuler un flux
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

    // Formatage de la durée
    let c = MediaContent { duration_ms: 90_000, ..Default::default() };
    println!("{}", c.formatted_duration()); // "1:30"
}
```

---

## Modules

| Module | Description |
|--------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedStore` — limité à 500 éléments, déduplique par `content_hash` |
| `social` | `SocialGraph` — suivi/désabonnement, `is_following`, `following_count` |
| `streaming` | Client de flux Aether (asynchrone, tokio) |
| `player` | Liaisons FFI LibVLC (activées via fonctionnalité : `features = ["player"]`) |
| `ui` | Interface bureau Iced/Slint (activée via fonctionnalité : `features = ["ui"]`) |

---

## Fonctionnalités

```toml
[dependencies]
aether-media = { version = "1.0.0", features = ["player", "ui"] }
```

| Fonctionnalité | Défaut | Description |
|---------|---------|-------------|
| `player` | désactivée | Liaisons FFI LibVLC pour la lecture audio/vidéo |
| `ui` | désactivée | Interface bureau (Iced ou Slint, configurée à la compilation) |

Compilé sans aucune fonctionnalité, le crate fournit les modèles, le flux, le graphe social et le streaming asynchrone — adapté aux cibles sans interface et embarquées.

---

## Lecteur (LibVLC)

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

LibVLC doit être installé sur le système hôte. L'indicateur de fonctionnalité active la liaison à la compilation ; le crate ne compilera pas avec `features = ["player"]` si `libvlc` est absent.

---

## Streaming asynchrone

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

## Compatibilité filaire

La sérialisation utilise `serde_json` avec les mêmes noms de champs que l'implémentation de référence C# :

```rust
let json = serde_json::to_string(&content).unwrap();
// {"contentHash":"sha256abc","title":"Hello Mesh","durationMs":180000,...}
```

---

## Structure du projet

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

## Licence

MIT
