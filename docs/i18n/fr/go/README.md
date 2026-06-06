# Aether Media — Implémentation Go

[English](../../../../go/README.md) · [Français](README.md) · [Español](../../es/go/README.md) · [العربية](../../ar/go/README.md) · [中文简体](../../zh-CN/go/README.md) · [日本語](../../ja/go/README.md) · [Deutsch](../../de/go/README.md) · [Português (BR)](../../pt-BR/go/README.md) · [Русский](../../ru/go/README.md) · [فارسی](../../fa/go/README.md) · [한국어](../../ko/go/README.md)

Une implémentation Go d'Aether Media fournissant un démon de nœud multimédia en arrière-plan, un lecteur en ligne de commande et un utilitaire de test de compatibilité filaire. Convient aux serveurs sans interface graphique, aux périphériques NAS et aux pipelines multimédia scriptés. Compatible au format filaire avec l'implémentation de référence C#.

---

## Prérequis

- Go 1.22+
- Optionnel : LibVLC (liaisons cgo ; requis uniquement pour la lecture `aether-media-cli`)

---

## Installation

```bash
go get github.com/bhengubv/aether-media/go
```

Ou cloner et compiler depuis les sources :

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media/go
go build ./...
```

---

## Commandes

### `aether-media-daemon` — Nœud multimédia en arrière-plan

Exécute un nœud Aether Media persistant, analysant la bibliothèque locale, propageant les annonces de contenu aux pairs du maillage et servant les morceaux de contenu à la demande.

```bash
go run ./cmd/aether-media-daemon \
    --library /media/library \
    --identity ~/.aether/identity.json \
    --transport ble,wifi-direct
```

| Option | Valeur par défaut | Description |
|--------|-------------------|-------------|
| `--library` | `~/Media` | Chemin vers la bibliothèque multimédia locale |
| `--identity` | `~/.aether/identity.json` | Fichier d'identité AetherMeshTag |
| `--transport` | `auto` | Liste de transports séparés par des virgules |
| `--port` | `7420` | Port d'écoute du relais HTTP |
| `--log-level` | `info` | Verbosité des journaux (`debug`, `info`, `warn`, `error`) |

### `aether-media-cli` — Lecteur en ligne de commande

Interface CLI interactive pour parcourir le fil, lire du contenu et gérer la bibliothèque locale.

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

### `wire-roundtrip` — Test d'interopérabilité

Sérialise et désérialise les structures de domaine principales, vérifiant la compatibilité du format filaire avec les autres implémentations dans d'autres langages.

```bash
go run ./cmd/wire-roundtrip
```

---

## Paquets

| Paquet | Description |
|--------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedAggregator` — limité à 500 éléments, déduplique par hachage de contenu |
| `social` | `SocialGraph` — suivre/ne plus suivre par UHID AetherMeshTag |
| `streaming` | Client `IStreamingService` Aether et abonnement aux flux en direct |
| `player` | Liaisons cgo LibVLC pour la lecture audio/vidéo |

---

## Démarrage rapide

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

## Exécuter les tests

```bash
go test ./...
```

---

## Compatibilité du format filaire

Toutes les structures sont sérialisées dans le même format filaire JSON utilisé par l'implémentation de référence C#. Exécutez `wire-roundtrip` sur les fixtures de test dans `../../tests/cross-language/` pour vérifier :

```bash
go run ./cmd/wire-roundtrip --fixtures ../../tests/cross-language
```

---

## Structure du projet

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

## Licence

MIT
