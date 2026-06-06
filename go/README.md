# Aether Media — Go Implementation

[English](README.md) · [Français](../docs/i18n/fr/go/README.md) · [Español](../docs/i18n/es/go/README.md) · [العربية](../docs/i18n/ar/go/README.md) · [中文简体](../docs/i18n/zh-CN/go/README.md) · [日本語](../docs/i18n/ja/go/README.md) · [Deutsch](../docs/i18n/de/go/README.md) · [Português (BR)](../docs/i18n/pt-BR/go/README.md) · [Русский](../docs/i18n/ru/go/README.md) · [فارسی](../docs/i18n/fa/go/README.md) · [한국어](../docs/i18n/ko/go/README.md)

A Go implementation of Aether Media providing a background media node daemon, a command-line player, and a wire-roundtrip test utility. Suitable for headless servers, NAS devices, and scripted media pipelines. Wire-format compatible with the C# reference implementation.

---

## Requirements

- Go 1.22+
- Optional: LibVLC (cgo bindings; required only for `aether-media-cli` playback)

---

## Install

```bash
go get github.com/bhengubv/aether-media/go
```

Or clone and build from source:

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media/go
go build ./...
```

---

## Commands

### `aether-media-daemon` — Background media node

Runs a persistent Aether Media node, scanning the local library, gossiping content announcements to mesh peers, and serving content chunks on request.

```bash
go run ./cmd/aether-media-daemon \
    --library /media/library \
    --identity ~/.aether/identity.json \
    --transport ble,wifi-direct
```

| Flag | Default | Description |
|------|---------|-------------|
| `--library` | `~/Media` | Path to local media library |
| `--identity` | `~/.aether/identity.json` | AetherMeshTag identity file |
| `--transport` | `auto` | Comma-separated transport list |
| `--port` | `7420` | HTTP relay listen port |
| `--log-level` | `info` | Logging verbosity (`debug`, `info`, `warn`, `error`) |

### `aether-media-cli` — Command-line player

Interactive CLI for browsing the feed, playing content, and managing the local library.

```bash
go run ./cmd/aether-media-cli
```

```
Commands:
  feed          Browse the content feed from followed creators
  play <hash>   Play content by SHA-256 hash
  search <q>    Search the local library
  follow <tag>  Follow a creator by AetherMeshTag
  library       List local media files
  quit
```

### `wire-roundtrip` — Interoperability test

Serialises and deserialises core domain structs, verifying wire-format compatibility with other language implementations.

```bash
go run ./cmd/wire-roundtrip
```

---

## Packages

| Package | Description |
|---------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedAggregator` — capped at 500 items, deduplicates by content hash |
| `social` | `SocialGraph` — follow/unfollow by AetherMeshTag UHID |
| `streaming` | Aether `IStreamingService` client and live-stream subscription |
| `player` | LibVLC cgo bindings for audio/video playback |

---

## Quick start

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

## Run tests

```bash
go test ./...
```

---

## Wire compatibility

All structs serialise to the same JSON wire format used by the C# reference implementation. Run `wire-roundtrip` against test fixtures in `../../tests/cross-language/` to verify:

```bash
go run ./cmd/wire-roundtrip --fixtures ../../tests/cross-language
```

---

## Project layout

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

## License

MIT
