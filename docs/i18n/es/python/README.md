# Aether Media — Implementación en Python

[English](../../../../python/README.md) · [Français](../../fr/python/README.md) · [Español](README.md) · [العربية](../../ar/python/README.md) · [中文简体](../../zh-CN/python/README.md) · [日本語](../../ja/python/README.md) · [Deutsch](../../de/python/README.md) · [Português (BR)](../../pt-BR/python/README.md) · [Русский](../../ru/python/README.md) · [فارسی](../../fa/python/README.md) · [한국어](../../ko/python/README.md)

Un motor de plugins Python y capa de scripting para Aether Media. Proporciona lectura y escritura de metadatos (ID3, MP4, NFO), análisis de listas de reproducción (M3U, XSPF), un host de plugins modelado a partir de la API de extensiones de VLC, y una interfaz de línea de comandos. Destinado a usuarios avanzados, scripts de automatización y autores de plugins de terceros.

---

## Requisitos

- Python 3.11+
- pip

---

## Instalación

```bash
pip install aether-media
```

O instalar desde el código fuente:

```bash
cd python
pip install -e ".[dev]"
```

---

## Ejecutar pruebas

```bash
pytest
```

---

## Módulos

| Módulo | Descripción |
|--------|-------------|
| `aethernet_media.models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `aethernet_media.metadata` | Lectura/escritura de etiquetas ID3 y MP4 (vía Mutagen); extracción de XML NFO |
| `aethernet_media.playlist` | Análisis y serialización de listas de reproducción M3U y XSPF |
| `aethernet_media.plugins` | Host de plugins — cargar, activar y llamar scripts de extensión estilo VLC |
| `aethernet_media.cli` | Punto de entrada de la línea de comandos (comando `aether-media`) |

---

## Inicio rápido

### Leer metadatos

```python
from aethernet_media.metadata import read_tags

tags = read_tags("/media/music/track.mp3")
print(tags.title)    # "Song Title"
print(tags.artist)   # "Artist Name"
print(tags.duration) # 213.4 (seconds)
```

### Escribir metadatos

```python
from aethernet_media.metadata import write_tags, TagUpdate

write_tags("/media/music/track.mp3", TagUpdate(
    title="Updated Title",
    artist="Updated Artist",
))
```

### Analizar una lista de reproducción

```python
from aethernet_media.playlist import parse_m3u, parse_xspf

tracks = parse_m3u("/media/playlists/summer.m3u")
for track in tracks:
    print(track.path, track.duration)

tracks = parse_xspf("/media/playlists/podcast.xspf")
```

### Extraer un archivo NFO

```python
from aethernet_media.metadata import read_nfo

movie = read_nfo("/media/movies/Inception/Inception.nfo")
print(movie.title)   # "Inception"
print(movie.year)    # 2010
print(movie.plot)    # "A thief who steals corporate secrets..."
```

### Cargar un plugin

```python
from aethernet_media.plugins import PluginHost

host = PluginHost()
host.load("/path/to/my_plugin.py")
host.activate("my_plugin")
host.trigger("on_media_start", content_hash="sha256abc")
```

---

## Interfaz de línea de comandos

```bash
# Leer etiquetas de un archivo
aether-media metadata read /media/music/track.mp3

# Escribir etiquetas
aether-media metadata write /media/music/track.mp3 --title "New Title"

# Analizar una lista de reproducción
aether-media playlist parse /media/playlists/summer.m3u

# Listar plugins instalados
aether-media plugins list

# Ejecutar un comando de plugin
aether-media plugins run my_plugin on_media_start --hash sha256abc
```

---

## Escribir un plugin

Un plugin es un archivo Python simple que expone un conjunto de hooks de ciclo de vida:

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

Coloca el archivo en el directorio de plugins (por defecto: `~/.aether/plugins/`) y actívalo:

```bash
aether-media plugins activate my_plugin
```

---

## Modelos

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

## Estructura del proyecto

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

## Licencia

MIT
