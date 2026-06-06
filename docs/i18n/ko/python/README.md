# Aether Media — Python 구현

[English](../../../../python/README.md) · [Français](../../fr/python/README.md) · [Español](../../es/python/README.md) · [العربية](../../ar/python/README.md) · [中文简体](../../zh-CN/python/README.md) · [日本語](../../ja/python/README.md) · [Deutsch](../../de/python/README.md) · [Português (BR)](../../pt-BR/python/README.md) · [Русский](../../ru/python/README.md) · [فارسی](../../fa/python/README.md) · [한국어](README.md)

Aether Media를 위한 Python 플러그인 엔진 및 스크립팅 레이어입니다. 메타데이터 읽기/쓰기(ID3, MP4, NFO), 플레이리스트 파싱(M3U, XSPF), VLC 확장 API를 모델로 한 플러그인 호스트, 그리고 커맨드라인 인터페이스를 제공합니다. 파워 유저, 자동화 스크립트, 서드파티 플러그인 작성자를 위해 설계되었습니다.

---

## 요구 사항

- Python 3.11+
- pip

---

## 설치

```bash
pip install aether-media
```

또는 소스에서 설치:

```bash
cd python
pip install -e ".[dev]"
```

---

## 테스트 실행

```bash
pytest
```

---

## 모듈

| 모듈 | 설명 |
|--------|-------------|
| `aethernet_media.models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `aethernet_media.metadata` | ID3 및 MP4 태그 읽기/쓰기 (Mutagen 사용); NFO XML 스크래핑 |
| `aethernet_media.playlist` | M3U 및 XSPF 플레이리스트 파싱 및 직렬화 |
| `aethernet_media.plugins` | 플러그인 호스트 — VLC 스타일 확장 스크립트 로드, 활성화 및 호출 |
| `aethernet_media.cli` | 커맨드라인 진입점 (`aether-media` 명령) |

---

## 빠른 시작

### 메타데이터 읽기

```python
from aethernet_media.metadata import read_tags

tags = read_tags("/media/music/track.mp3")
print(tags.title)    # "Song Title"
print(tags.artist)   # "Artist Name"
print(tags.duration) # 213.4 (seconds)
```

### 메타데이터 쓰기

```python
from aethernet_media.metadata import write_tags, TagUpdate

write_tags("/media/music/track.mp3", TagUpdate(
    title="Updated Title",
    artist="Updated Artist",
))
```

### 플레이리스트 파싱

```python
from aethernet_media.playlist import parse_m3u, parse_xspf

tracks = parse_m3u("/media/playlists/summer.m3u")
for track in tracks:
    print(track.path, track.duration)

tracks = parse_xspf("/media/playlists/podcast.xspf")
```

### NFO 파일 스크래핑

```python
from aethernet_media.metadata import read_nfo

movie = read_nfo("/media/movies/Inception/Inception.nfo")
print(movie.title)   # "Inception"
print(movie.year)    # 2010
print(movie.plot)    # "A thief who steals corporate secrets..."
```

### 플러그인 로드

```python
from aethernet_media.plugins import PluginHost

host = PluginHost()
host.load("/path/to/my_plugin.py")
host.activate("my_plugin")
host.trigger("on_media_start", content_hash="sha256abc")
```

---

## 커맨드라인 인터페이스

```bash
# Read tags from a file
aether-media metadata read /media/music/track.mp3

# Write tags
aether-media metadata write /media/music/track.mp3 --title "New Title"

# Parse a playlist
aether-media playlist parse /media/playlists/summer.m3u

# List installed plugins
aether-media plugins list

# Run a plugin command
aether-media plugins run my_plugin on_media_start --hash sha256abc
```

---

## 플러그인 작성

플러그인은 일련의 라이프사이클 훅을 노출하는 일반 Python 파일입니다:

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

파일을 플러그인 디렉터리(기본값: `~/.aether/plugins/`)에 저장하고 활성화합니다:

```bash
aether-media plugins activate my_plugin
```

---

## 모델

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

## 프로젝트 구조

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

## 라이선스

MIT
