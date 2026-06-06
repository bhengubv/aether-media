# Aether Media — Go Implementation

[English](../../../../go/README.md) · [Français](../../fr/go/README.md) · [Español](../../es/go/README.md) · [العربية](../../ar/go/README.md) · [中文简体](../../zh-CN/go/README.md) · [日本語](../../ja/go/README.md) · [Deutsch](README.md) · [Português (BR)](../../pt-BR/go/README.md) · [Русский](../../ru/go/README.md) · [فارسی](../../fa/go/README.md) · [한국어](../../ko/go/README.md)

Eine Go-Implementierung von Aether Media, die einen Hintergrund-Media-Node-Daemon, einen Kommandozeilen-Player und ein Wire-Roundtrip-Testwerkzeug bereitstellt. Geeignet für kopflose Server, NAS-Geräte und skriptgesteuerte Media-Pipelines. Wire-format-kompatibel mit der C#-Referenzimplementierung.

---

## Voraussetzungen

- Go 1.22+
- Optional: LibVLC (cgo-Bindungen; nur für die Wiedergabe mit `aether-media-cli` erforderlich)

---

## Installation

```bash
go get github.com/bhengubv/aether-media/go
```

Oder aus dem Quellcode klonen und bauen:

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media/go
go build ./...
```

---

## Befehle

### `aether-media-daemon` — Hintergrund-Media-Node

Betreibt einen dauerhaften Aether-Media-Node, der die lokale Bibliothek durchsucht, Inhaltsankündigungen an Mesh-Peers weitergibt und auf Anfrage Inhaltsfragmente bereitstellt.

```bash
go run ./cmd/aether-media-daemon \
    --library /media/library \
    --identity ~/.aether/identity.json \
    --transport ble,wifi-direct
```

| Flag | Standard | Beschreibung |
|------|----------|-------------|
| `--library` | `~/Media` | Pfad zur lokalen Medienbibliothek |
| `--identity` | `~/.aether/identity.json` | AetherMeshTag-Identitätsdatei |
| `--transport` | `auto` | Kommagetrennte Transportliste |
| `--port` | `7420` | HTTP-Relay-Listening-Port |
| `--log-level` | `info` | Protokollierungsdetailgrad (`debug`, `info`, `warn`, `error`) |

### `aether-media-cli` — Kommandozeilen-Player

Interaktive CLI zum Durchsuchen des Feeds, Abspielen von Inhalten und Verwalten der lokalen Bibliothek.

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

### `wire-roundtrip` — Interoperabilitätstest

Serialisiert und deserialisiert zentrale Domänenstrukturen und überprüft die Wire-format-Kompatibilität mit anderen Sprachimplementierungen.

```bash
go run ./cmd/wire-roundtrip
```

---

## Pakete

| Paket | Beschreibung |
|-------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedAggregator` — auf 500 Einträge begrenzt, dedupliziert nach Inhalts-Hash |
| `social` | `SocialGraph` — Folgen/Entfolgen per AetherMeshTag-UHID |
| `streaming` | Aether-`IStreamingService`-Client und Live-Stream-Abonnement |
| `player` | LibVLC-cgo-Bindungen für Audio-/Videowiedergabe |

---

## Schnellstart

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

## Tests ausführen

```bash
go test ./...
```

---

## Wire-Kompatibilität

Alle Strukturen werden in dasselbe JSON-Wire-Format serialisiert, das von der C#-Referenzimplementierung verwendet wird. Führen Sie `wire-roundtrip` gegen die Testfixtures in `../../tests/cross-language/` aus, um dies zu überprüfen:

```bash
go run ./cmd/wire-roundtrip --fixtures ../../tests/cross-language
```

---

## Projektstruktur

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

## Lizenz

MIT
