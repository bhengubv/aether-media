<div dir="rtl">

# Aether Media — پیاده‌سازی Python

[English](../../../../python/README.md) · [Français](../../fr/python/README.md) · [Español](../../es/python/README.md) · [العربية](../../ar/python/README.md) · [中文简体](../../zh-CN/python/README.md) · [日本語](../../ja/python/README.md) · [Deutsch](../../de/python/README.md) · [Português (BR)](../../pt-BR/python/README.md) · [Русский](../../ru/python/README.md) · [فارسی](README.md) · [한국어](../../ko/python/README.md)

یک موتور افزونه Python و لایه اسکریپت‌نویسی برای Aether Media. خواندن و نوشتن متادیتا (ID3، MP4، NFO)، پارس کردن پلی‌لیست (M3U، XSPF)، یک میزبان افزونه بر اساس API افزونه VLC، و یک رابط خط فرمان فراهم می‌کند. برای کاربران پیشرفته، اسکریپت‌های اتوماسیون، و نویسندگان افزونه‌های شخص ثالث طراحی شده است.

---

## پیش‌نیازها

- Python 3.11+
- pip

---

## نصب

```bash
pip install aether-media
```

یا نصب از سورس:

```bash
cd python
pip install -e ".[dev]"
```

---

## اجرای تست‌ها

```bash
pytest
```

---

## ماژول‌ها

| ماژول | توضیحات |
|--------|-------------|
| `aether_media.models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `aether_media.metadata` | خواندن و نوشتن تگ ID3 و MP4 (از طریق Mutagen)؛ استخراج XML از NFO |
| `aether_media.playlist` | پارس کردن و سریال‌سازی پلی‌لیست M3U و XSPF |
| `aether_media.plugins` | میزبان افزونه — بارگذاری، فعال‌سازی، و فراخوانی اسکریپت‌های سبک VLC |
| `aether_media.cli` | نقطه ورودی خط فرمان (دستور `aether-media`) |

---

## شروع سریع

### خواندن متادیتا

```python
from aether_media.metadata import read_tags

tags = read_tags("/media/music/track.mp3")
print(tags.title)    # "Song Title"
print(tags.artist)   # "Artist Name"
print(tags.duration) # 213.4 (seconds)
```

### نوشتن متادیتا

```python
from aether_media.metadata import write_tags, TagUpdate

write_tags("/media/music/track.mp3", TagUpdate(
    title="Updated Title",
    artist="Updated Artist",
))
```

### پارس کردن پلی‌لیست

```python
from aether_media.playlist import parse_m3u, parse_xspf

tracks = parse_m3u("/media/playlists/summer.m3u")
for track in tracks:
    print(track.path, track.duration)

tracks = parse_xspf("/media/playlists/podcast.xspf")
```

### استخراج فایل NFO

```python
from aether_media.metadata import read_nfo

movie = read_nfo("/media/movies/Inception/Inception.nfo")
print(movie.title)   # "Inception"
print(movie.year)    # 2010
print(movie.plot)    # "A thief who steals corporate secrets..."
```

### بارگذاری افزونه

```python
from aether_media.plugins import PluginHost

host = PluginHost()
host.load("/path/to/my_plugin.py")
host.activate("my_plugin")
host.trigger("on_media_start", content_hash="sha256abc")
```

---

## رابط خط فرمان

```bash
# خواندن تگ‌ها از یک فایل
aether-media metadata read /media/music/track.mp3

# نوشتن تگ‌ها
aether-media metadata write /media/music/track.mp3 --title "New Title"

# پارس کردن پلی‌لیست
aether-media playlist parse /media/playlists/summer.m3u

# فهرست افزونه‌های نصب‌شده
aether-media plugins list

# اجرای یک دستور افزونه
aether-media plugins run my_plugin on_media_start --hash sha256abc
```

---

## نوشتن افزونه

یک افزونه یک فایل Python ساده است که مجموعه‌ای از قلاب‌های چرخه حیات را در معرض دید قرار می‌دهد:

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

فایل را در دایرکتوری افزونه‌ها قرار دهید (پیش‌فرض: `~/.aether/plugins/`) و آن را فعال کنید:

```bash
aether-media plugins activate my_plugin
```

---

## مدل‌ها

```python
from aether_media.models import MediaContent, MediaProfile, MediaFeedItem

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

## ساختار پروژه

```
python/
├── aether_media/
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

## مجوز

MIT

</div>
