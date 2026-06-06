<div dir="rtl">

# Aether Media — تنفيذ Python

[English](../../../../python/README.md) · [Français](../../fr/python/README.md) · [Español](../../es/python/README.md) · [العربية](README.md) · [中文简体](../../zh-CN/python/README.md) · [日本語](../../ja/python/README.md) · [Deutsch](../../de/python/README.md) · [Português (BR)](../../pt-BR/python/README.md) · [Русский](../../ru/python/README.md) · [فارسی](../../fa/python/README.md) · [한국어](../../ko/python/README.md)

محرك مكوّنات إضافية وطبقة نصوص برمجية بلغة Python لـ Aether Media. يوفر قراءة وكتابة البيانات الوصفية (ID3، MP4، NFO)، وتحليل قوائم التشغيل (M3U، XSPF)، ومضيف مكوّنات إضافية مُصمَّم على غرار VLC extension API، وواجهة سطر أوامر. مخصص للمستخدمين المتقدمين، والنصوص البرمجية للأتمتة، ومؤلفي المكوّنات الإضافية من أطراف ثالثة.

---

## المتطلبات

- Python 3.11+
- pip

---

## التثبيت

```bash
pip install aether-media
```

أو قم بالتثبيت من المصدر:

```bash
cd python
pip install -e ".[dev]"
```

---

## تشغيل الاختبارات

```bash
pytest
```

---

## الوحدات

| الوحدة | الوصف |
|--------|-------------|
| `aethermesh_media.models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` |
| `aethermesh_media.metadata` | قراءة وكتابة وسوم ID3 وMP4 (عبر Mutagen)؛ استخراج XML من ملفات NFO |
| `aethermesh_media.playlist` | تحليل وتسلسل قوائم التشغيل M3U وXSPF |
| `aethermesh_media.plugins` | مضيف المكوّنات الإضافية — تحميل وتفعيل واستدعاء نصوص VLC-style |
| `aethermesh_media.cli` | نقطة دخول سطر الأوامر (أمر `aether-media`) |

---

## البدء السريع

### قراءة البيانات الوصفية

```python
from aethermesh_media.metadata import read_tags

tags = read_tags("/media/music/track.mp3")
print(tags.title)    # "Song Title"
print(tags.artist)   # "Artist Name"
print(tags.duration) # 213.4 (seconds)
```

### كتابة البيانات الوصفية

```python
from aethermesh_media.metadata import write_tags, TagUpdate

write_tags("/media/music/track.mp3", TagUpdate(
    title="Updated Title",
    artist="Updated Artist",
))
```

### تحليل قائمة تشغيل

```python
from aethermesh_media.playlist import parse_m3u, parse_xspf

tracks = parse_m3u("/media/playlists/summer.m3u")
for track in tracks:
    print(track.path, track.duration)

tracks = parse_xspf("/media/playlists/podcast.xspf")
```

### استخراج ملف NFO

```python
from aethermesh_media.metadata import read_nfo

movie = read_nfo("/media/movies/Inception/Inception.nfo")
print(movie.title)   # "Inception"
print(movie.year)    # 2010
print(movie.plot)    # "A thief who steals corporate secrets..."
```

### تحميل مكوّن إضافي

```python
from aethermesh_media.plugins import PluginHost

host = PluginHost()
host.load("/path/to/my_plugin.py")
host.activate("my_plugin")
host.trigger("on_media_start", content_hash="sha256abc")
```

---

## واجهة سطر الأوامر

```bash
# قراءة الوسوم من ملف
aether-media metadata read /media/music/track.mp3

# كتابة الوسوم
aether-media metadata write /media/music/track.mp3 --title "New Title"

# تحليل قائمة تشغيل
aether-media playlist parse /media/playlists/summer.m3u

# سرد المكوّنات الإضافية المثبتة
aether-media plugins list

# تشغيل أمر مكوّن إضافي
aether-media plugins run my_plugin on_media_start --hash sha256abc
```

---

## كتابة مكوّن إضافي

المكوّن الإضافي هو ملف Python عادي يكشف مجموعة من خطّافات دورة الحياة:

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

ضع الملف في دليل المكوّنات الإضافية (الافتراضي: `~/.aether/plugins/`) وقم بتفعيله:

```bash
aether-media plugins activate my_plugin
```

---

## النماذج

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

## تخطيط المشروع

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

## الرخصة

MIT

</div>
