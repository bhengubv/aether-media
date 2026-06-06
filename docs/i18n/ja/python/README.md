# Aether Media — Python 実装

[English](../../../../python/README.md) · [Français](../../fr/python/README.md) · [Español](../../es/python/README.md) · [العربية](../../ar/python/README.md) · [中文简体](../../zh-CN/python/README.md) · [日本語](README.md) · [Deutsch](../../de/python/README.md) · [Português (BR)](../../pt-BR/python/README.md) · [Русский](../../ru/python/README.md) · [فارسی](../../fa/python/README.md) · [한국어](../../ko/python/README.md)

Aether Media 向けの Python プラグインエンジンおよびスクリプティングレイヤーです。メタデータの読み取り・書き込み（ID3、MP4、NFO）、プレイリスト解析（M3U、XSPF）、VLC 拡張 API をモデルにしたプラグインホスト、およびコマンドラインインターフェースを提供します。パワーユーザー、自動化スクリプト、サードパーティプラグイン作者を対象としています。

---

## 要件

- Python 3.11+
- pip

---

## インストール

```bash
pip install aether-media
```

またはソースからインストール:

```bash
cd python
pip install -e ".[dev]"
```

---

## テストの実行

```bash
pytest
```

---

## モジュール

| モジュール | 説明 |
|--------|-------------|
| `aethernet_media.models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `aethernet_media.metadata` | ID3 および MP4 タグの読み書き（Mutagen 経由）; NFO XML スクレイピング |
| `aethernet_media.playlist` | M3U および XSPF プレイリストの解析とシリアライゼーション |
| `aethernet_media.plugins` | プラグインホスト — VLC スタイルの拡張スクリプトの読み込み・アクティベート・呼び出し |
| `aethernet_media.cli` | コマンドラインエントリポイント（`aether-media` コマンド） |

---

## クイックスタート

### メタデータの読み取り

```python
from aethernet_media.metadata import read_tags

tags = read_tags("/media/music/track.mp3")
print(tags.title)    # "Song Title"
print(tags.artist)   # "Artist Name"
print(tags.duration) # 213.4 (seconds)
```

### メタデータの書き込み

```python
from aethernet_media.metadata import write_tags, TagUpdate

write_tags("/media/music/track.mp3", TagUpdate(
    title="Updated Title",
    artist="Updated Artist",
))
```

### プレイリストの解析

```python
from aethernet_media.playlist import parse_m3u, parse_xspf

tracks = parse_m3u("/media/playlists/summer.m3u")
for track in tracks:
    print(track.path, track.duration)

tracks = parse_xspf("/media/playlists/podcast.xspf")
```

### NFO ファイルのスクレイピング

```python
from aethernet_media.metadata import read_nfo

movie = read_nfo("/media/movies/Inception/Inception.nfo")
print(movie.title)   # "Inception"
print(movie.year)    # 2010
print(movie.plot)    # "A thief who steals corporate secrets..."
```

### プラグインの読み込み

```python
from aethernet_media.plugins import PluginHost

host = PluginHost()
host.load("/path/to/my_plugin.py")
host.activate("my_plugin")
host.trigger("on_media_start", content_hash="sha256abc")
```

---

## コマンドラインインターフェース

```bash
# ファイルからタグを読み取る
aether-media metadata read /media/music/track.mp3

# タグを書き込む
aether-media metadata write /media/music/track.mp3 --title "New Title"

# プレイリストを解析する
aether-media playlist parse /media/playlists/summer.m3u

# インストール済みプラグインの一覧
aether-media plugins list

# プラグインコマンドを実行する
aether-media plugins run my_plugin on_media_start --hash sha256abc
```

---

## プラグインの作成

プラグインは一連のライフサイクルフックを公開するプレーンな Python ファイルです:

```python
# my_plugin.py

PLUGIN_NAME = "my_plugin"
PLUGIN_VERSION = "1.0.0"

def on_media_start(content_hash: str, **kwargs):
    """Called when playback begins."""
    print(f"Playing: {content_hash}")

def on_media_stop(content_hash: str, position_ms: int, **kwargs):
    """Called when playback stops."""
    print(f"Stopped at {position_ms} ms")

def on_feed_item(item, **kwargs):
    """Called for each new feed item received from the mesh."""
    print(f"New content: {item.content.title}")
```

ファイルをプラグインディレクトリ（デフォルト: `~/.aether/plugins/`）に配置し、アクティベートします:

```bash
aether-media plugins activate my_plugin
```

---

## モデル

```python
from aethernet_media.models import MediaContent, MediaProfile, MediaFeedItem

content = MediaContent(
    content_hash="sha256abc",
    title="Sample Video",
    duration_ms=180_000,
    codec="h264",
    content_type="video/mp4",
    creator_uhid="uhid-xyz",
    size_bytes=52_428_800,
)

print(content.formatted_duration)  # "3:00"
print(content.is_video)             # True
```

---

## プロジェクト構成

```
python/
├── aethernet_media/
│   ├── __init__.py
│   ├── models.py            # Domain models
│   ├── metadata/            # Tag reading/writing, NFO scraping
│   ├── playlist/            # M3U and XSPF parsers
│   ├── plugins/             # Plugin host
│   └── cli/                 # CLI entry point
├── tests/
└── pyproject.toml
```

---

## ライセンス

MIT
