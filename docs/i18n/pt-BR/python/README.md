# Aether Media — Implementação em Python

[English](../../../../python/README.md) · [Français](../../fr/python/README.md) · [Español](../../es/python/README.md) · [العربية](../../ar/python/README.md) · [中文简体](../../zh-CN/python/README.md) · [日本語](../../ja/python/README.md) · [Deutsch](../../de/python/README.md) · [Português (BR)](README.md) · [Русский](../../ru/python/README.md) · [فارسی](../../fa/python/README.md) · [한국어](../../ko/python/README.md)

Um motor de plugins e camada de scripting em Python para o Aether Media. Fornece leitura e escrita de metadados (ID3, MP4, NFO), análise de playlists (M3U, XSPF), um host de plugins modelado na API de extensões do VLC, e uma interface de linha de comando. Destinado a usuários avançados, scripts de automação e autores de plugins de terceiros.

---

## Requisitos

- Python 3.11+
- pip

---

## Instalação

```bash
pip install aether-media
```

Ou instalar a partir do código-fonte:

```bash
cd python
pip install -e ".[dev]"
```

---

## Executar testes

```bash
pytest
```

---

## Módulos

| Módulo | Descrição |
|--------|-------------|
| `aethermesh_media.models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `aethermesh_media.metadata` | Leitura/escrita de tags ID3 e MP4 (via Mutagen); extração de XML NFO |
| `aethermesh_media.playlist` | Análise e serialização de playlists M3U e XSPF |
| `aethermesh_media.plugins` | Host de plugins — carregar, ativar e chamar scripts de extensão no estilo VLC |
| `aethermesh_media.cli` | Ponto de entrada da linha de comando (comando `aether-media`) |

---

## Início rápido

### Ler metadados

```python
from aethermesh_media.metadata import read_tags

tags = read_tags("/media/music/track.mp3")
print(tags.title)    # "Song Title"
print(tags.artist)   # "Artist Name"
print(tags.duration) # 213.4 (seconds)
```

### Escrever metadados

```python
from aethermesh_media.metadata import write_tags, TagUpdate

write_tags("/media/music/track.mp3", TagUpdate(
    title="Updated Title",
    artist="Updated Artist",
))
```

### Analisar uma playlist

```python
from aethermesh_media.playlist import parse_m3u, parse_xspf

tracks = parse_m3u("/media/playlists/summer.m3u")
for track in tracks:
    print(track.path, track.duration)

tracks = parse_xspf("/media/playlists/podcast.xspf")
```

### Extrair um arquivo NFO

```python
from aethermesh_media.metadata import read_nfo

movie = read_nfo("/media/movies/Inception/Inception.nfo")
print(movie.title)   # "Inception"
print(movie.year)    # 2010
print(movie.plot)    # "A thief who steals corporate secrets..."
```

### Carregar um plugin

```python
from aethermesh_media.plugins import PluginHost

host = PluginHost()
host.load("/path/to/my_plugin.py")
host.activate("my_plugin")
host.trigger("on_media_start", content_hash="sha256abc")
```

---

## Interface de linha de comando

```bash
# Ler tags de um arquivo
aether-media metadata read /media/music/track.mp3

# Escrever tags
aether-media metadata write /media/music/track.mp3 --title "New Title"

# Analisar uma playlist
aether-media playlist parse /media/playlists/summer.m3u

# Listar plugins instalados
aether-media plugins list

# Executar um comando de plugin
aether-media plugins run my_plugin on_media_start --hash sha256abc
```

---

## Criando um plugin

Um plugin é um arquivo Python simples que expõe um conjunto de ganchos de ciclo de vida:

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

Coloque o arquivo no diretório de plugins (padrão: `~/.aether/plugins/`) e ative-o:

```bash
aether-media plugins activate my_plugin
```

---

## Modelos

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

## Estrutura do projeto

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

## Licença

MIT
