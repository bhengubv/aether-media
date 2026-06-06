# Aether Media — Go 实现

[English](../../../../go/README.md) · [Français](../../fr/go/README.md) · [Español](../../es/go/README.md) · [العربية](../../ar/go/README.md) · [中文简体](README.md) · [日本語](../../ja/go/README.md) · [Deutsch](../../de/go/README.md) · [Português (BR)](../../pt-BR/go/README.md) · [Русский](../../ru/go/README.md) · [فارسی](../../fa/go/README.md) · [한국어](../../ko/go/README.md)

Aether Media 的 Go 实现，提供后台媒体节点守护进程、命令行播放器以及线路往返测试工具。适用于无头服务器、NAS 设备和脚本化媒体流水线。与 C# 参考实现在线路格式上完全兼容。

---

## 环境要求

- Go 1.22+
- 可选：LibVLC（cgo 绑定；仅 `aether-media-cli` 播放功能需要）

---

## 安装

```bash
go get github.com/bhengubv/aether-media/go
```

或从源码克隆并构建：

```bash
git clone https://github.com/bhengubv/aether-media.git
cd aether-media/go
go build ./...
```

---

## 命令

### `aether-media-daemon` — 后台媒体节点

运行持久化的 Aether Media 节点，扫描本地媒体库，向网状节点广播内容公告，并按需提供内容块服务。

```bash
go run ./cmd/aether-media-daemon \
    --library /media/library \
    --identity ~/.aether/identity.json \
    --transport ble,wifi-direct
```

| 参数 | 默认值 | 说明 |
|------|---------|-------------|
| `--library` | `~/Media` | 本地媒体库路径 |
| `--identity` | `~/.aether/identity.json` | AetherMeshTag 身份文件 |
| `--transport` | `auto` | 逗号分隔的传输方式列表 |
| `--port` | `7420` | HTTP 中继监听端口 |
| `--log-level` | `info` | 日志详细级别（`debug`、`info`、`warn`、`error`） |

### `aether-media-cli` — 命令行播放器

用于浏览 Feed、播放内容及管理本地媒体库的交互式命令行界面。

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

### `wire-roundtrip` — 互操作性测试

对核心领域结构体进行序列化和反序列化，验证与其他语言实现之间的线路格式兼容性。

```bash
go run ./cmd/wire-roundtrip
```

---

## 包

| 包 | 说明 |
|---------|-------------|
| `models` | `MediaContent`、`MediaProfile`、`MediaFeedItem`、`MediaReaction` |
| `feed` | `FeedAggregator` — 上限 500 条，按内容哈希去重 |
| `social` | `SocialGraph` — 通过 AetherMeshTag UHID 关注/取消关注 |
| `streaming` | Aether `IStreamingService` 客户端及直播流订阅 |
| `player` | 用于音视频播放的 LibVLC cgo 绑定 |

---

## 快速入门

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

## 运行测试

```bash
go test ./...
```

---

## 线路格式兼容性

所有结构体均序列化为与 C# 参考实现相同的 JSON 线路格式。针对 `../../tests/cross-language/` 中的测试固件运行 `wire-roundtrip` 以进行验证：

```bash
go run ./cmd/wire-roundtrip --fixtures ../../tests/cross-language
```

---

## 项目结构

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

## 许可证

MIT
