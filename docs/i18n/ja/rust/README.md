# Aether Media — Rust 実装

[English](../../../../rust/README.md) · [Français](../../fr/rust/README.md) · [Español](../../es/rust/README.md) · [العربية](../../ar/rust/README.md) · [中文简体](../../zh-CN/rust/README.md) · [日本語](README.md) · [Deutsch](../../de/rust/README.md) · [Português (BR)](../../pt-BR/rust/README.md) · [Русский](../../ru/rust/README.md) · [فارسی](../../fa/rust/README.md) · [한국어](../../ko/rust/README.md)

Aether Media の完全な Rust 実装です。軽量デスクトップ UI（Iced/Slint）、再生用 LibVLC FFI バインディング、500 件上限のフィードストアを備えています。C# リファレンス実装とワイヤーフォーマット互換です。フットプリントの小さいデスクトップフォールバックおよび組み込みメディアノードとして適しています。

---

## 要件

- Rust 1.78+（stable ツールチェーン）
- Cargo
- オプション: LibVLC 共有ライブラリ（再生用。他の機能はなくても動作します）

---

## プロジェクトへの追加

```toml
# Cargo.toml
[dependencies]
aether-media = "1.0.0"
```

またはソースからビルド:

```bash
cargo build --release
```

---

## テストの実行

```bash
cargo test
```

### ベンチマークの実行

```bash
cargo bench
```

ベンチマークは Criterion を使用し、HTML 結果を `target/criterion/` に出力します。

---

## クイックスタート

```rust
use aethermedia::{
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

## モジュール

| モジュール | 説明 |
|--------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedStore` — 最大 500 件、`content_hash` による重複排除 |
| `social` | `SocialGraph` — フォロー/アンフォロー、`is_following`、`following_count` |
| `streaming` | Aether ストリームクライアント（非同期、tokio） |
| `player` | LibVLC FFI バインディング（フィーチャーゲート: `features = ["player"]`） |
| `ui` | Iced/Slint デスクトップ UI（フィーチャーゲート: `features = ["ui"]`） |

---

## フィーチャー

```toml
[dependencies]
aether-media = { version = "1.0.0", features = ["player", "ui"] }
```

| フィーチャー | デフォルト | 説明 |
|---------|---------|-------------|
| `player` | off | 音声/映像再生用 LibVLC FFI バインディング |
| `ui` | off | デスクトップ UI（Iced または Slint、ビルド時に設定） |

フィーチャーなしでビルドした場合、クレートはモデル、フィード、ソーシャル、非同期ストリーミングを提供します。ヘッドレスおよび組み込みターゲットに適しています。

---

## プレイヤー（LibVLC）

```rust
use aethermedia::player::Player;

#[tokio::main]
async fn main() {
    let mut player = Player::new().expect("LibVLC not found");
    player.open("aether://content/sha256abc").await.unwrap();
    player.play();
    tokio::time::sleep(std::time::Duration::from_secs(5)).await;
    player.stop();
}
```

LibVLC はホストシステムにインストールされている必要があります。フィーチャーフラグはコンパイル時のリンクを有効にします。`libvlc` が存在しない場合、`features = ["player"]` を指定してもクレートはビルドされません。

---

## 非同期ストリーミング

```rust
use aethermedia::streaming::StreamClient;

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

## ワイヤー互換性

シリアライゼーションは `serde_json` を使用し、C# リファレンス実装と同じフィールド名を持ちます:

```rust
let json = serde_json::to_string(&content).unwrap();
// {"contentHash":"sha256abc","title":"Hello Mesh","durationMs":180000,...}
```

---

## プロジェクト構成

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

## ライセンス

MIT
