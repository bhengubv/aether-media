# Aether Media — Implementação em Go

[English](../../../../go/README.md) · [Français](../../fr/go/README.md) · [Español](../../es/go/README.md) · [العربية](../../ar/go/README.md) · [中文简体](../../zh-CN/go/README.md) · [日本語](../../ja/go/README.md) · [Deutsch](../../de/go/README.md) · [Português (BR)](README.md) · [Русский](../../ru/go/README.md) · [فارسی](../../fa/go/README.md) · [한국어](../../ko/go/README.md)

Uma implementação Go do Aether Media que fornece um daemon de nó de mídia em segundo plano, um player de linha de comando e um utilitário de teste de roundtrip de wire. Adequado para servidores headless, dispositivos NAS e pipelines de mídia automatizados. Compatível no formato de wire com a implementação de referência em C#.

---

## Requisitos

- Go 1.22+
- Opcional: LibVLC (bindings cgo; necessário apenas para reprodução no `aether-media-cli`)

---

## Instalação

```bash
go get github.com/bhengubv/aether-media/go
```

Ou clone e compile a partir do código-fonte:

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media/go
go build ./...
```

---

## Comandos

### `aether-media-daemon` — Nó de mídia em segundo plano

Executa um nó Aether Media persistente, varrendo a biblioteca local, propagando anúncios de conteúdo para peers na mesh e servindo fragmentos de conteúdo sob demanda.

```bash
go run ./cmd/aether-media-daemon \
    --library /media/library \
    --identity ~/.aether/identity.json \
    --transport ble,wifi-direct
```

| Flag | Padrão | Descrição |
|------|---------|-------------|
| `--library` | `~/Media` | Caminho para a biblioteca de mídia local |
| `--identity` | `~/.aether/identity.json` | Arquivo de identidade AetherMeshTag |
| `--transport` | `auto` | Lista de transportes separada por vírgulas |
| `--port` | `7420` | Porta de escuta do relay HTTP |
| `--log-level` | `info` | Verbosidade do log (`debug`, `info`, `warn`, `error`) |

### `aether-media-cli` — Player de linha de comando

CLI interativa para navegar no feed, reproduzir conteúdo e gerenciar a biblioteca local.

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

### `wire-roundtrip` — Teste de interoperabilidade

Serializa e desserializa as structs de domínio principais, verificando a compatibilidade de formato de wire com outras implementações de linguagem.

```bash
go run ./cmd/wire-roundtrip
```

---

## Pacotes

| Pacote | Descrição |
|---------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedAggregator` — limitado a 500 itens, elimina duplicatas por hash de conteúdo |
| `social` | `SocialGraph` — follow/unfollow por UHID AetherMeshTag |
| `streaming` | Cliente `IStreamingService` Aether e subscrição de stream ao vivo |
| `player` | Bindings cgo do LibVLC para reprodução de áudio/vídeo |

---

## Início rápido

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

## Executar testes

```bash
go test ./...
```

---

## Compatibilidade de formato de wire

Todas as structs são serializadas no mesmo formato JSON de wire utilizado pela implementação de referência em C#. Execute `wire-roundtrip` com os fixtures de teste em `../../tests/cross-language/` para verificar:

```bash
go run ./cmd/wire-roundtrip --fixtures ../../tests/cross-language
```

---

## Estrutura do projeto

```
go/
├── cmd/
│   ├── aether-media-daemon/   # Nó em segundo plano
│   ├── aether-media-cli/      # Player CLI
│   └── wire-roundtrip/        # Ferramenta de teste de interoperabilidade
├── feed/                      # FeedAggregator
├── models/                    # Structs de domínio
├── player/                    # Bindings cgo do LibVLC
├── social/                    # SocialGraph
├── streaming/                 # Subscrição de stream
└── go.mod
```

---

## Licença

MIT
