# Aether Media — Реализация на Rust

[English](../../../../rust/README.md) · [Français](../../fr/rust/README.md) · [Español](../../es/rust/README.md) · [العربية](../../ar/rust/README.md) · [中文简体](../../zh-CN/rust/README.md) · [日本語](../../ja/rust/README.md) · [Deutsch](../../de/rust/README.md) · [Português (BR)](../../pt-BR/rust/README.md) · [Русский](README.md) · [فارسی](../../fa/rust/README.md) · [한국어](../../ko/rust/README.md)

Полная реализация Aether Media на Rust с лёгким десктопным интерфейсом (Iced/Slint), FFI-привязками LibVLC для воспроизведения и хранилищем ленты с ограничением в 500 элементов. Совместима по формату проводного протокола с эталонной реализацией на C#. Подходит в качестве экономичного десктопного запасного варианта и как встроенный медиаузел.

---

## Требования

- Rust 1.78+ (стабильный тулчейн)
- Cargo
- Необязательно: разделяемая библиотека LibVLC (для воспроизведения; все прочие функции работают без неё)

---

## Добавление в проект

```toml
# Cargo.toml
[dependencies]
aether-media = "1.0.0"
```

Или сборка из исходников:

```bash
cargo build --release
```

---

## Запуск тестов

```bash
cargo test
```

### Запуск бенчмарков

```bash
cargo bench
```

Бенчмарки используют Criterion и выводят HTML-результаты в `target/criterion/`.

---

## Быстрый старт

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

## Модули

| Модуль | Описание |
|--------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedStore` — ограничен 500 элементами, дедублирует по `content_hash` |
| `social` | `SocialGraph` — подписки/отписки, `is_following`, `following_count` |
| `streaming` | Клиент потока Aether (асинхронный, tokio) |
| `player` | FFI-привязки LibVLC (управляемые флагом: `features = ["player"]`) |
| `ui` | Десктопный интерфейс Iced/Slint (управляемый флагом: `features = ["ui"]`) |

---

## Возможности (features)

```toml
[dependencies]
aether-media = { version = "1.0.0", features = ["player", "ui"] }
```

| Флаг | По умолчанию | Описание |
|---------|---------|-------------|
| `player` | выкл. | FFI-привязки LibVLC для воспроизведения аудио/видео |
| `ui` | выкл. | Десктопный интерфейс (Iced или Slint, настраивается во время сборки) |

При сборке без флагов крейт предоставляет модели, ленту, граф социальных связей и асинхронный стриминг — подходит для серверных и встроенных целей без пользовательского интерфейса.

---

## Проигрыватель (LibVLC)

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

LibVLC должен быть установлен в хост-системе. Флаг функции включает компиляционное связывание; крейт не соберётся с `features = ["player"]`, если `libvlc` отсутствует.

---

## Асинхронный стриминг

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

## Совместимость проводного протокола

Сериализация использует `serde_json` с теми же именами полей, что и эталонная реализация на C#:

```rust
let json = serde_json::to_string(&content).unwrap();
// {"contentHash":"sha256abc","title":"Hello Mesh","durationMs":180000,...}
```

---

## Структура проекта

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

## Лицензия

MIT
