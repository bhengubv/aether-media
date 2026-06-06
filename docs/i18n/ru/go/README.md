# Aether Media — Go Implementation

[English](../../../../go/README.md) · [Français](../../fr/go/README.md) · [Español](../../es/go/README.md) · [العربية](../../ar/go/README.md) · [中文简体](../../zh-CN/go/README.md) · [日本語](../../ja/go/README.md) · [Deutsch](../../de/go/README.md) · [Português (BR)](../../pt-BR/go/README.md) · [Русский](README.md) · [فارسی](../../fa/go/README.md) · [한국어](../../ko/go/README.md)

Реализация Aether Media на Go, включающая фоновый демон медиаузла, консольный плеер и утилиту для тестирования круговой сериализации данных. Подходит для безголовых серверов, NAS-устройств и скриптовых медиаконвейеров. Совместима на уровне формата данных с эталонной реализацией на C#.

---

## Требования

- Go 1.22+
- Опционально: LibVLC (привязки cgo; требуется только для воспроизведения в `aether-media-cli`)

---

## Установка

```bash
go get github.com/bhengubv/aether-media/go
```

Или клонируйте и соберите из исходного кода:

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media/go
go build ./...
```

---

## Команды

### `aether-media-daemon` — Фоновый медиаузел

Запускает постоянный узел Aether Media, сканирует локальную библиотеку, передаёт объявления о контенте меш-пирам и обслуживает блоки контента по запросу.

```bash
go run ./cmd/aether-media-daemon \
    --library /media/library \
    --identity ~/.aether/identity.json \
    --transport ble,wifi-direct
```

| Флаг | По умолчанию | Описание |
|------|-------------|----------|
| `--library` | `~/Media` | Путь к локальной медиабиблиотеке |
| `--identity` | `~/.aether/identity.json` | Файл идентификатора AetherNetTag |
| `--transport` | `auto` | Список транспортов через запятую |
| `--port` | `7420` | Порт прослушивания HTTP-ретрансляции |
| `--log-level` | `info` | Уровень детализации логов (`debug`, `info`, `warn`, `error`) |

### `aether-media-cli` — Консольный плеер

Интерактивный CLI для просмотра ленты, воспроизведения контента и управления локальной библиотекой.

```bash
go run ./cmd/aether-media-cli
```

```
Commands:
  feed          Browse the content feed from followed creators
  play <hash>   Play content by SHA-256 hash
  search <q>    Search the local library
  follow <tag>  Follow a creator by AetherNetTag
  library       List local media files
  quit
```

### `wire-roundtrip` — Тест совместимости

Сериализует и десериализует основные доменные структуры, проверяя совместимость формата данных с реализациями на других языках.

```bash
go run ./cmd/wire-roundtrip
```

---

## Пакеты

| Пакет | Описание |
|-------|----------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedAggregator` — ограничен 500 элементами, дедуплицирует по хэшу контента |
| `social` | `SocialGraph` — подписка/отписка по UHID AetherNetTag |
| `streaming` | Клиент `IStreamingService` Aether и подписка на прямые трансляции |
| `player` | Привязки cgo LibVLC для воспроизведения аудио/видео |

---

## Быстрый старт

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

## Запуск тестов

```bash
go test ./...
```

---

## Совместимость на уровне протокола

Все структуры сериализуются в тот же JSON-формат, что используется в эталонной реализации на C#. Запустите `wire-roundtrip` с тестовыми данными из `../../tests/cross-language/` для проверки:

```bash
go run ./cmd/wire-roundtrip --fixtures ../../tests/cross-language
```

---

## Структура проекта

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

## Лицензия

MIT
