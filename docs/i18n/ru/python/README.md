# Aether Media — Реализация на Python

[English](../../../../python/README.md) · [Français](../../fr/python/README.md) · [Español](../../es/python/README.md) · [العربية](../../ar/python/README.md) · [中文简体](../../zh-CN/python/README.md) · [日本語](../../ja/python/README.md) · [Deutsch](../../de/python/README.md) · [Português (BR)](../../pt-BR/python/README.md) · [Русский](README.md) · [فارسی](../../fa/python/README.md) · [한국어](../../ko/python/README.md)

Движок плагинов и слой сценариев на Python для Aether Media. Обеспечивает чтение и запись метаданных (ID3, MP4, NFO), разбор плейлистов (M3U, XSPF), хост плагинов по образцу API расширений VLC, а также интерфейс командной строки. Предназначен для опытных пользователей, скриптов автоматизации и авторов сторонних плагинов.

---

## Требования

- Python 3.11+
- pip

---

## Установка

```bash
pip install aether-media
```

Или установка из исходников:

```bash
cd python
pip install -e ".[dev]"
```

---

## Запуск тестов

```bash
pytest
```

---

## Модули

| Модуль | Описание |
|--------|-------------|
| `aethermesh_media.models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `aethermesh_media.metadata` | Чтение и запись тегов ID3 и MP4 (через Mutagen); разбор NFO XML |
| `aethermesh_media.playlist` | Разбор и сериализация плейлистов M3U и XSPF |
| `aethermesh_media.plugins` | Хост плагинов — загрузка, активация и вызов скриптов расширений в стиле VLC |
| `aethermesh_media.cli` | Точка входа командной строки (команда `aether-media`) |

---

## Быстрый старт

### Чтение метаданных

```python
from aethermesh_media.metadata import read_tags

tags = read_tags("/media/music/track.mp3")
print(tags.title)    # "Song Title"
print(tags.artist)   # "Artist Name"
print(tags.duration) # 213.4 (seconds)
```

### Запись метаданных

```python
from aethermesh_media.metadata import write_tags, TagUpdate

write_tags("/media/music/track.mp3", TagUpdate(
    title="Updated Title",
    artist="Updated Artist",
))
```

### Разбор плейлиста

```python
from aethermesh_media.playlist import parse_m3u, parse_xspf

tracks = parse_m3u("/media/playlists/summer.m3u")
for track in tracks:
    print(track.path, track.duration)

tracks = parse_xspf("/media/playlists/podcast.xspf")
```

### Разбор NFO-файла

```python
from aethermesh_media.metadata import read_nfo

movie = read_nfo("/media/movies/Inception/Inception.nfo")
print(movie.title)   # "Inception"
print(movie.year)    # 2010
print(movie.plot)    # "A thief who steals corporate secrets..."
```

### Загрузка плагина

```python
from aethermesh_media.plugins import PluginHost

host = PluginHost()
host.load("/path/to/my_plugin.py")
host.activate("my_plugin")
host.trigger("on_media_start", content_hash="sha256abc")
```

---

## Интерфейс командной строки

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

## Написание плагина

Плагин представляет собой обычный файл Python, предоставляющий набор хуков жизненного цикла:

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

Поместите файл в директорию плагинов (по умолчанию: `~/.aether/plugins/`) и активируйте его:

```bash
aether-media plugins activate my_plugin
```

---

## Модели

```python
from aethermesh_media.models import MediaContent, MediaProfile, MediaFeedItem

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

## Структура проекта

```
python/
├── aethermesh_media/
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

## Лицензия

MIT
