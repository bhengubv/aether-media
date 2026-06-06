# Aether Media — Rust 구현

[English](../../../../rust/README.md) · [Français](../../fr/rust/README.md) · [Español](../../es/rust/README.md) · [العربية](../../ar/rust/README.md) · [中文简体](../../zh-CN/rust/README.md) · [日本語](../../ja/rust/README.md) · [Deutsch](../../de/rust/README.md) · [Português (BR)](../../pt-BR/rust/README.md) · [Русский](../../ru/rust/README.md) · [فارسی](../../fa/rust/README.md) · [한국어](README.md)

경량 데스크톱 UI(Iced/Slint), 재생을 위한 LibVLC FFI 바인딩, 500개 항목 제한 피드 저장소를 갖춘 완전한 Rust 구현입니다. C# 참조 구현과 와이어 포맷 호환성을 유지합니다. 저사양 데스크톱 폴백 및 임베디드 미디어 노드로 적합합니다.

---

## 요구 사항

- Rust 1.78+ (stable 툴체인)
- Cargo
- 선택 사항: LibVLC 공유 라이브러리 (재생용; 다른 모든 기능은 없어도 작동)

---

## 프로젝트에 추가하기

```toml
# Cargo.toml
[dependencies]
aether-media = "1.0.0"
```

또는 소스에서 빌드:

```bash
cargo build --release
```

---

## 테스트 실행

```bash
cargo test
```

### 벤치마크 실행

```bash
cargo bench
```

벤치마크는 Criterion을 사용하며 HTML 결과를 `target/criterion/`에 저장합니다.

---

## 빠른 시작

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

## 모듈

| 모듈 | 설명 |
|--------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedStore` — 최대 500개 항목, `content_hash`로 중복 제거 |
| `social` | `SocialGraph` — 팔로우/언팔로우, `is_following`, `following_count` |
| `streaming` | Aether 스트림 클라이언트 (비동기, tokio) |
| `player` | LibVLC FFI 바인딩 (피처 게이트: `features = ["player"]`) |
| `ui` | Iced/Slint 데스크톱 UI (피처 게이트: `features = ["ui"]`) |

---

## 피처

```toml
[dependencies]
aether-media = { version = "1.0.0", features = ["player", "ui"] }
```

| 피처 | 기본값 | 설명 |
|---------|---------|-------------|
| `player` | off | 오디오/비디오 재생을 위한 LibVLC FFI 바인딩 |
| `ui` | off | 데스크톱 UI (Iced 또는 Slint, 빌드 시 설정) |

피처 없이 빌드하면 크레이트는 모델, 피드, 소셜, 비동기 스트리밍을 제공하며 헤드리스 및 임베디드 타깃에 적합합니다.

---

## 플레이어 (LibVLC)

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

LibVLC는 호스트 시스템에 설치되어 있어야 합니다. 피처 플래그를 사용하면 컴파일 시 링킹이 활성화되며, `libvlc`가 없으면 `features = ["player"]`로 빌드할 수 없습니다.

---

## 비동기 스트리밍

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

## 와이어 호환성

직렬화는 C# 참조 구현과 동일한 필드 이름을 가진 `serde_json`을 사용합니다:

```rust
let json = serde_json::to_string(&content).unwrap();
// {"contentHash":"sha256abc","title":"Hello Mesh","durationMs":180000,...}
```

---

## 프로젝트 구조

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

## 라이선스

MIT
