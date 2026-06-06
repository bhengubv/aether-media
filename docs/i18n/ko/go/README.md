# Aether Media — Go 구현

[English](../../../../go/README.md) · [Français](../../fr/go/README.md) · [Español](../../es/go/README.md) · [العربية](../../ar/go/README.md) · [中文简体](../../zh-CN/go/README.md) · [日本語](../../ja/go/README.md) · [Deutsch](../../de/go/README.md) · [Português (BR)](../../pt-BR/go/README.md) · [Русский](../../ru/go/README.md) · [فارسی](../../fa/go/README.md) · [한국어](README.md)

백그라운드 미디어 노드 데몬, 커맨드라인 플레이어, 와이어 왕복 테스트 유틸리티를 제공하는 Aether Media의 Go 구현체입니다. 헤드리스 서버, NAS 장치, 스크립트 기반 미디어 파이프라인에 적합합니다. C# 참조 구현체와 와이어 포맷이 호환됩니다.

---

## 요구 사항

- Go 1.22+
- 선택 사항: LibVLC (cgo 바인딩; `aether-media-cli` 재생에만 필요)

---

## 설치

```bash
go get github.com/bhengubv/aether-media/go
```

또는 소스를 클론하여 빌드:

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media/go
go build ./...
```

---

## 명령어

### `aether-media-daemon` — 백그라운드 미디어 노드

로컬 라이브러리를 스캔하고, 메시 피어에 콘텐츠 알림을 전파하며, 요청 시 콘텐츠 청크를 제공하는 영구적인 Aether Media 노드를 실행합니다.

```bash
go run ./cmd/aether-media-daemon \
    --library /media/library \
    --identity ~/.aether/identity.json \
    --transport ble,wifi-direct
```

| 플래그 | 기본값 | 설명 |
|------|---------|-------------|
| `--library` | `~/Media` | 로컬 미디어 라이브러리 경로 |
| `--identity` | `~/.aether/identity.json` | AetherNetTag 신원 파일 |
| `--transport` | `auto` | 쉼표로 구분된 전송 목록 |
| `--port` | `7420` | HTTP 릴레이 수신 포트 |
| `--log-level` | `info` | 로그 상세도 (`debug`, `info`, `warn`, `error`) |

### `aether-media-cli` — 커맨드라인 플레이어

피드 탐색, 콘텐츠 재생, 로컬 라이브러리 관리를 위한 인터랙티브 CLI입니다.

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

### `wire-roundtrip` — 상호 운용성 테스트

핵심 도메인 구조체를 직렬화 및 역직렬화하여 다른 언어 구현체와의 와이어 포맷 호환성을 검증합니다.

```bash
go run ./cmd/wire-roundtrip
```

---

## 패키지

| 패키지 | 설명 |
|---------|-------------|
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `feed` | `FeedAggregator` — 최대 500개 항목, 콘텐츠 해시로 중복 제거 |
| `social` | `SocialGraph` — AetherNetTag UHID로 팔로우/언팔로우 |
| `streaming` | Aether `IStreamingService` 클라이언트 및 라이브 스트림 구독 |
| `player` | 오디오/비디오 재생을 위한 LibVLC cgo 바인딩 |

---

## 빠른 시작

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

## 테스트 실행

```bash
go test ./...
```

---

## 와이어 호환성

모든 구조체는 C# 참조 구현체와 동일한 JSON 와이어 포맷으로 직렬화됩니다. `../../tests/cross-language/`의 테스트 픽스처를 대상으로 `wire-roundtrip`을 실행하여 호환성을 검증하세요:

```bash
go run ./cmd/wire-roundtrip --fixtures ../../tests/cross-language
```

---

## 프로젝트 구조

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

## 라이선스

MIT
