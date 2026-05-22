# Aether Media — Go 実装

[English](../../../../go/README.md) · [Français](../../fr/go/README.md) · [Español](../../es/go/README.md) · [العربية](../../ar/go/README.md) · [中文简体](../../zh-CN/go/README.md) · [日本語](README.md) · [Deutsch](../../de/go/README.md) · [Português (BR)](../../pt-BR/go/README.md) · [Русский](../../ru/go/README.md) · [فارسی](../../fa/go/README.md) · [한국어](../../ko/go/README.md)

Aether Media の Go 実装です。バックグラウンドメディアノードデーモン、コマンドラインプレイヤー、およびワイヤーラウンドトリップテストユーティリティを提供します。ヘッドレスサーバー、NAS デバイス、スクリプト化されたメディアパイプラインに適しています。C# リファレンス実装とワイヤーフォーマット互換です。

---

## 要件

- Go 1.22 以降
- オプション: LibVLC（cgo バインディング。`aether-media-cli` の再生機能にのみ必要）

---

## インストール

```bash
go get github.com/bhengubv/aether-media/go
```

またはソースからクローンしてビルドする場合:

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media/go
go build ./...
```

---

## コマンド

### `aether-media-daemon` — バックグラウンドメディアノード

ローカルライブラリをスキャンし、コンテンツアナウンスをメッシュピアにゴシップ配布し、リクエストに応じてコンテンツチャンクを配信する、永続的な Aether Media ノードを実行します。

```bash
go run ./cmd/aether-media-daemon \
    --library /media/library \
    --identity ~/.aether/identity.json \
    --transport ble,wifi-direct
```

| フラグ | デフォルト | 説明 |
|------|---------|-------------|
| `--library` | `~/Media` | ローカルメディアライブラリのパス |
| `--identity` | `~/.aether/identity.json` | AetherTag アイデンティティファイル |
| `--transport` | `auto` | カンマ区切りのトランスポートリスト |
| `--port` | `7420` | HTTP リレーのリッスンポート |
| `--log-level` | `info` | ログの詳細度（`debug`、`info`、`warn`、`error`） |

### `aether-media-cli` — コマンドラインプレイヤー

フィードの閲覧、コンテンツの再生、ローカルライブラリの管理を行うインタラクティブ CLI です。

```bash
go run ./cmd/aether-media-cli
```

```
Commands:
  feed          Browse the content feed from followed creators
  play <hash>   Play content by SHA-256 hash
  search <q>    Search the local library
  follow <tag>  Follow a creator by AetherTag
  library       List local media files
  quit
```

### `wire-roundtrip` — 相互運用性テスト

コアドメイン構造体のシリアライズとデシリアライズを行い、他の言語実装とのワイヤーフォーマット互換性を検証します。

```bash
go run ./cmd/wire-roundtrip
```

---

## パッケージ

| パッケージ | 説明 |
|---------|-------------|
| `models` | `MediaContent`、`MediaProfile`、`MediaFeedItem`、`MediaReaction` |
| `feed` | `FeedAggregator` — 最大 500 件、コンテンツハッシュで重複排除 |
| `social` | `SocialGraph` — AetherTag UHID によるフォロー / アンフォロー |
| `streaming` | Aether `IStreamingService` クライアントおよびライブストリームサブスクリプション |
| `player` | 音声 / 動画再生用 LibVLC cgo バインディング |

---

## クイックスタート

```go
package main

import (
    "fmt"
    "github.com/bhengubv/aether-media/go/feed"
    "github.com/bhengubv/aether-media/go/models"
    "github.com/bhengubv/aether-media/go/social"
)

func main() {
    // Build a social graph
    graph := social.NewSocialGraph()
    graph.Follow("peer-uhid-abc123")
    fmt.Println("Following:", graph.FollowingCount())

    // Aggregate a feed
    agg := feed.NewFeedAggregator(500)
    item := models.MediaFeedItem{
        Content: models.MediaContent{
            ContentHash: "sha256abc",
            Title:       "Hello Mesh",
            DurationMs:  180000,
        },
    }
    agg.Push(item)
    fmt.Println("Feed size:", agg.Size())
}
```

---

## テストの実行

```bash
go test ./...
```

---

## ワイヤーフォーマット互換性

すべての構造体は C# リファレンス実装が使用するものと同一の JSON ワイヤーフォーマットでシリアライズされます。`../../tests/cross-language/` のテストフィクスチャに対して `wire-roundtrip` を実行して検証してください。

```bash
go run ./cmd/wire-roundtrip --fixtures ../../tests/cross-language
```

---

## プロジェクト構成

```
go/
├── cmd/
│   ├── aether-media-daemon/   # Background node
│   ├── aether-media-cli/      # CLI player
│   └── wire-roundtrip/        # Interop test tool
├── feed/                      # FeedAggregator
├── models/                    # Domain structs
├── player/                    # LibVLC cgo bindings
├── social/                    # SocialGraph
├── streaming/                 # Stream subscription
└── go.mod
```

---

## ライセンス

MIT
