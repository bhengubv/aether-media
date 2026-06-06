# Aether Media — Python-Implementierung

[English](../../../../python/README.md) · [Français](../../fr/python/README.md) · [Español](../../es/python/README.md) · [العربية](../../ar/python/README.md) · [中文简体](../../zh-CN/python/README.md) · [日本語](../../ja/python/README.md) · [Deutsch](README.md) · [Português (BR)](../../pt-BR/python/README.md) · [Русский](../../ru/python/README.md) · [فارسی](../../fa/python/README.md) · [한국어](../../ko/python/README.md)

Eine Python-Plugin-Engine und Scripting-Schicht für Aether Media. Bietet Metadaten-Lesen und -Schreiben (ID3, MP4, NFO), Playlist-Parsing (M3U, XSPF), einen Plugin-Host nach dem Vorbild der VLC-Extension-API sowie eine Befehlszeilenschnittstelle. Gedacht für Power-User, Automatisierungsskripte und Drittanbieter-Plugin-Autoren.

---

## Voraussetzungen

- Python 3.11+
- pip

---

## Installation

```bash
pip install aether-media
```

Oder aus dem Quellcode installieren:

```bash
cd python
pip install -e ".[dev]"
```

---

## Tests ausführen

```bash
pytest
```

---

## Module

| Modul | Beschreibung |
|--------|-------------|
| `aethernet_media.models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `aethernet_media.metadata` | ID3- und MP4-Tag-Lesen/Schreiben (via Mutagen); NFO-XML-Scraping |
| `aethernet_media.playlist` | M3U- und XSPF-Playlist-Parsing und Serialisierung |
| `aethernet_media.plugins` | Plugin-Host — VLC-artige Extension-Skripte laden, aktivieren und aufrufen |
| `aethernet_media.cli` | Befehlszeilen-Einstiegspunkt (Befehl `aether-media`) |

---

## Schnellstart

### Metadaten lesen

```python
from aethernet_media.metadata import read_tags

tags = read_tags("/media/music/track.mp3")
print(tags.title)    # "Song Title"
print(tags.artist)   # "Artist Name"
print(tags.duration) # 213.4 (seconds)
```

### Metadaten schreiben

```python
from aethernet_media.metadata import write_tags, TagUpdate

write_tags("/media/music/track.mp3", TagUpdate(
    title="Updated Title",
    artist="Updated Artist",
))
```

### Eine Playlist parsen

```python
from aethernet_media.playlist import parse_m3u, parse_xspf

tracks = parse_m3u("/media/playlists/summer.m3u")
for track in tracks:
    print(track.path, track.duration)

tracks = parse_xspf("/media/playlists/podcast.xspf")
```

### Eine NFO-Datei auslesen

```python
from aethernet_media.metadata import read_nfo

movie = read_nfo("/media/movies/Inception/Inception.nfo")
print(movie.title)   # "Inception"
print(movie.year)    # 2010
print(movie.plot)    # "A thief who steals corporate secrets..."
```

### Ein Plugin laden

```python
from aethernet_media.plugins import PluginHost

host = PluginHost()
host.load("/path/to/my_plugin.py")
host.activate("my_plugin")
host.trigger("on_media_start", content_hash="sha256abc")
```

---

## Befehlszeilenschnittstelle

```bash
# Tags aus einer Datei lesen
aether-media metadata read /media/music/track.mp3

# Tags schreiben
aether-media metadata write /media/music/track.mp3 --title "New Title"

# Eine Playlist parsen
aether-media playlist parse /media/playlists/summer.m3u

# Installierte Plugins auflisten
aether-media plugins list

# Einen Plugin-Befehl ausführen
aether-media plugins run my_plugin on_media_start --hash sha256abc
```

---

## Ein Plugin schreiben

Ein Plugin ist eine einfache Python-Datei, die eine Reihe von Lebenszyklus-Hooks bereitstellt:

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

Legen Sie die Datei im Plugin-Verzeichnis ab (Standard: `~/.aether/plugins/`) und aktivieren Sie sie:

```bash
aether-media plugins activate my_plugin
```

---

## Modelle

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

## Projektstruktur

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

## Lizenz

MIT
