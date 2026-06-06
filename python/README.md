# Aether Media — Python Implementation

[English](README.md) · [Français](../docs/i18n/fr/python/README.md) · [Español](../docs/i18n/es/python/README.md) · [العربية](../docs/i18n/ar/python/README.md) · [中文简体](../docs/i18n/zh-CN/python/README.md) · [日本語](../docs/i18n/ja/python/README.md) · [Deutsch](../docs/i18n/de/python/README.md) · [Português (BR)](../docs/i18n/pt-BR/python/README.md) · [Русский](../docs/i18n/ru/python/README.md) · [فارسی](../docs/i18n/fa/python/README.md) · [한국어](../docs/i18n/ko/python/README.md)

A Python plugin engine and scripting layer for Aether Media. Provides metadata reading and writing (ID3, MP4, NFO), playlist parsing (M3U, XSPF), a plugin host modelled on the VLC extension API, and a command-line interface. Intended for power users, automation scripts, and third-party plugin authors.

---

## Requirements

- Python 3.11+
- pip

---

## Install

```bash
pip install aether-media
```

Or install from source:

```bash
cd python
pip install -e ".[dev]"
```

---

## Run tests

```bash
pytest
```

---

## Modules

| Module | Description |
|--------|-------------|
| `aethernet_media.models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `aethernet_media.metadata` | ID3 and MP4 tag reading/writing (via Mutagen); NFO XML scraping |
| `aethernet_media.playlist` | M3U and XSPF playlist parsing and serialisation |
| `aethernet_media.plugins` | Plugin host — load, activate, and call VLC-style extension scripts |
| `aethernet_media.cli` | Command-line entry point (`aether-media` command) |

---

## Quick start

### Read metadata

```python
from aethernet_media.metadata import read_tags

tags = read_tags("/media/music/track.mp3")
print(tags.title)    # "Song Title"
print(tags.artist)   # "Artist Name"
print(tags.duration) # 213.4 (seconds)
```

### Write metadata

```python
from aethernet_media.metadata import write_tags, TagUpdate

write_tags("/media/music/track.mp3", TagUpdate(
    title="Updated Title",
    artist="Updated Artist",
))
```

### Parse a playlist

```python
from aethernet_media.playlist import parse_m3u, parse_xspf

tracks = parse_m3u("/media/playlists/summer.m3u")
for track in tracks:
    print(track.path, track.duration)

tracks = parse_xspf("/media/playlists/podcast.xspf")
```

### Scrape an NFO file

```python
from aethernet_media.metadata import read_nfo

movie = read_nfo("/media/movies/Inception/Inception.nfo")
print(movie.title)   # "Inception"
print(movie.year)    # 2010
print(movie.plot)    # "A thief who steals corporate secrets..."
```

### Load a plugin

```python
from aethernet_media.plugins import PluginHost

host = PluginHost()
host.load("/path/to/my_plugin.py")
host.activate("my_plugin")
host.trigger("on_media_start", content_hash="sha256abc")
```

---

## Command-line interface

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

## Writing a plugin

A plugin is a plain Python file exposing a set of lifecycle hooks:

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

Place the file in the plugins directory (default: `~/.aether/plugins/`) and activate it:

```bash
aether-media plugins activate my_plugin
```

---

## Models

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

## Project layout

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

## License

MIT
