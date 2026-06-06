# Aether Media — Implementación en Go

[English](../../../../go/README.md) · [Français](../../fr/go/README.md) · [Español](README.md) · [العربية](../../ar/go/README.md) · [中文简体](../../zh-CN/go/README.md) · [日本語](../../ja/go/README.md) · [Deutsch](../../de/go/README.md) · [Português (BR)](../../pt-BR/go/README.md) · [Русский](../../ru/go/README.md) · [فارسی](../../fa/go/README.md) · [한국어](../../ko/go/README.md)

Una implementación en Go de Aether Media que proporciona un daemon de nodo multimedia en segundo plano, un reproductor de línea de comandos y una utilidad de prueba de ida y vuelta en formato de cable. Adecuada para servidores sin interfaz gráfica, dispositivos NAS y flujos de trabajo multimedia mediante scripts. Compatible en formato de cable con la implementación de referencia en C#.

---

## Requisitos

- Go 1.22+
- Opcional: LibVLC (bindings cgo; solo requerido para la reproducción en `aether-media-cli`)

---

## Instalación

```bash
go get github.com/bhengubv/aether-media/go
```

O clonar y compilar desde el código fuente:

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media/go
go build ./...
```

---

## Comandos

### `aether-media-daemon` — Nodo multimedia en segundo plano

Ejecuta un nodo Aether Media persistente, escaneando la biblioteca local, propagando anuncios de contenido a los pares de la malla y sirviendo fragmentos de contenido bajo demanda.

```bash
go run ./cmd/aether-media-daemon \
    --library /media/library \
    --identity ~/.aether/identity.json \
    --transport ble,wifi-direct
```

| Parámetro | Por defecto | Descripción |
|-----------|-------------|-------------|
| `--library` | `~/Media` | Ruta a la biblioteca multimedia local |
| `--identity` | `~/.aether/identity.json` | Archivo de identidad AetherMeshTag |
| `--transport` | `auto` | Lista de transportes separada por comas |
| `--port` | `7420` | Puerto de escucha del relay HTTP |
| `--log-level` | `info` | Nivel de detalle del registro (`debug`, `info`, `warn`, `error`) |

### `aether-media-cli` — Reproductor de línea de comandos

CLI interactivo para explorar el feed, reproducir contenido y gestionar la biblioteca local.

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

### `wire-roundtrip` — Prueba de interoperabilidad

Serializa y deserializa las estructuras de dominio principales, verificando la compatibilidad de formato de cable con otras implementaciones en distintos lenguajes.

```bash
go run ./cmd/wire-roundtrip
```

---

## Paquetes

| Paquete | Descripción |
|---------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedAggregator` — limitado a 500 elementos, desduplicado por hash de contenido |
| `social` | `SocialGraph` — seguir/dejar de seguir por UHID de AetherMeshTag |
| `streaming` | Cliente `IStreamingService` de Aether y suscripción a transmisiones en vivo |
| `player` | Bindings cgo de LibVLC para reproducción de audio/video |

---

## Inicio rápido

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

## Ejecutar pruebas

```bash
go test ./...
```

---

## Compatibilidad de formato de cable

Todas las estructuras se serializan al mismo formato JSON de cable utilizado por la implementación de referencia en C#. Ejecute `wire-roundtrip` con los fixtures de prueba en `../../tests/cross-language/` para verificarlo:

```bash
go run ./cmd/wire-roundtrip --fixtures ../../tests/cross-language
```

---

## Estructura del proyecto

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

## Licencia

MIT
