# Aether Media — Rust 实现

[English](../../../../rust/README.md) · [Français](../../fr/rust/README.md) · [Español](../../es/rust/README.md) · [العربية](../../ar/rust/README.md) · [中文简体](README.md) · [日本語](../../ja/rust/README.md) · [Deutsch](../../de/rust/README.md) · [Português (BR)](../../pt-BR/rust/README.md) · [Русский](../../ru/rust/README.md) · [فارسی](../../fa/rust/README.md) · [한국어](../../ko/rust/README.md)

Aether Media 的完整 Rust 实现，具备轻量级桌面 UI（Iced/Slint）、用于播放的 LibVLC FFI 绑定，以及容量上限为 500 条的信息流存储。线格式与 C# 参考实现完全兼容。适合用作低资源占用的桌面备用方案及嵌入式媒体节点。

---

## 环境要求

- Rust 1.78+（stable 工具链）
- Cargo
- 可选：LibVLC 共享库（用于播放；其余所有功能无需此库）

---

## 添加到项目

```toml
# Cargo.toml
[dependencies]
aether-media = "1.0.0"
```

或从源码构建：

```bash
cargo build --release
```

---

## 运行测试

```bash
cargo test
```

### 运行基准测试

```bash
cargo bench
```

基准测试使用 Criterion，HTML 结果输出至 `target/criterion/`。

---

## 快速入门

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

## 模块

| 模块 | 说明 |
|--------|-------------|
| `models` | `MediaContent`、`MediaProfile`、`MediaFeedItem`、`MediaReaction` |
| `feed` | `FeedStore`——上限 500 条，按 `content_hash` 去重 |
| `social` | `SocialGraph`——关注/取消关注、`is_following`、`following_count` |
| `streaming` | Aether 流客户端（异步，基于 tokio） |
| `player` | LibVLC FFI 绑定（特性门控：`features = ["player"]`） |
| `ui` | Iced/Slint 桌面 UI（特性门控：`features = ["ui"]`） |

---

## 特性

```toml
[dependencies]
aether-media = { version = "1.0.0", features = ["player", "ui"] }
```

| 特性 | 默认 | 说明 |
|---------|---------|-------------|
| `player` | 关闭 | LibVLC FFI 绑定，用于音视频播放 |
| `ui` | 关闭 | 桌面 UI（Iced 或 Slint，在构建时配置） |

不启用任何特性时，该 crate 提供模型、信息流、社交关系图谱和异步流功能——适用于无头和嵌入式目标。

---

## 播放器（LibVLC）

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

宿主系统上必须已安装 LibVLC。特性标志启用编译时链接；若 `libvlc` 缺失，带 `features = ["player"]` 的 crate 将无法构建。

---

## 异步流

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

## 线格式兼容性

序列化使用 `serde_json`，字段名与 C# 参考实现相同：

```rust
let json = serde_json::to_string(&content).unwrap();
// {"contentHash":"sha256abc","title":"Hello Mesh","durationMs":180000,...}
```

---

## 项目结构

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

## 许可证

MIT
