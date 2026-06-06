"""Tests for aethermedia.metadata.tag_reader."""

from __future__ import annotations

from unittest.mock import MagicMock, patch, PropertyMock

import pytest

from aethermedia.metadata.tag_reader import _first_str, _parse_track_number, read_tags


# ── _first_str ────────────────────────────────────────────────────────────────

def test_first_str_none_returns_none():
    assert _first_str(None) is None


def test_first_str_plain_string():
    assert _first_str("Hello") == "Hello"


def test_first_str_strips_whitespace():
    assert _first_str("  hello  ") == "hello"


def test_first_str_blank_string_returns_none():
    assert _first_str("   ") is None


def test_first_str_list_first_element():
    assert _first_str(["Alpha", "Beta"]) == "Alpha"


def test_first_str_empty_list_returns_none():
    assert _first_str([]) is None


def test_first_str_list_with_blank_first_returns_none():
    assert _first_str(["  "]) is None


def test_first_str_converts_non_string():
    assert _first_str(42) == "42"


# ── _parse_track_number ───────────────────────────────────────────────────────

def test_parse_track_number_simple():
    assert _parse_track_number("3") == 3


def test_parse_track_number_fraction():
    assert _parse_track_number("3/12") == 3


def test_parse_track_number_none():
    assert _parse_track_number(None) is None


def test_parse_track_number_invalid_string():
    assert _parse_track_number("abc") is None


def test_parse_track_number_zero():
    assert _parse_track_number("0") == 0


def test_parse_track_number_large():
    assert _parse_track_number("99/99") == 99


# ── read_tags — FileNotFoundError ─────────────────────────────────────────────

def test_read_tags_raises_when_file_missing():
    with pytest.raises(FileNotFoundError):
        read_tags("/nonexistent/path/file.mp3")


# ── read_tags — mutagen returns None ─────────────────────────────────────────

@patch("aethermedia.metadata.tag_reader.os.path.exists", return_value=True)
@patch("aethermedia.metadata.tag_reader.MutagenFile", return_value=None)
def test_read_tags_mutagen_none_returns_defaults(mock_file, mock_exists):
    result = read_tags("/fake/file.mp3")
    assert result["title"] is None
    assert result["format"] == "unknown"
    assert result["duration_ms"] is None


# ── read_tags — MP3 ───────────────────────────────────────────────────────────

def _make_mp3_mock(
    title="Test Title",
    artist="Test Artist",
    album="Test Album",
    genre="Rock",
    track="4/12",
    year="2021",
    artwork=None,
    duration=210.5,
):
    from mutagen.mp3 import MP3

    mock_info = MagicMock()
    mock_info.length = duration

    apic_mock = MagicMock()
    apic_mock.data = artwork

    tags_mock = MagicMock()

    def tags_get(key):
        mapping = {
            "TIT2": MagicMock(__str__=lambda s: title),
            "TPE1": MagicMock(__str__=lambda s: artist),
            "TALB": MagicMock(__str__=lambda s: album),
            "TCON": MagicMock(__str__=lambda s: genre),
            "TRCK": MagicMock(__str__=lambda s: track),
        }
        if key == "TDRC":
            tdrc = MagicMock()
            tdrc.text = [MagicMock(__str__=lambda s: year, __format__=lambda s, f: year)]
            # Make str(tdrc.text[0])[:4] work
            tdrc.text[0].__str__ = lambda s: year
            return tdrc
        return mapping.get(key)

    tags_mock.get = tags_get

    # simulate keys() for APIC lookup
    tags_mock.keys = lambda: (["APIC:Cover"] if artwork else [])
    if artwork:
        tags_mock.__getitem__ = lambda s, k: apic_mock

    audio_mock = MagicMock(spec=MP3)
    audio_mock.info = mock_info
    audio_mock.tags = tags_mock
    return audio_mock


@patch("aethermedia.metadata.tag_reader.os.path.exists", return_value=True)
@patch("aethermedia.metadata.tag_reader.MutagenFile")
@patch("aethermedia.metadata.tag_reader.isinstance")
def test_read_tags_mp3_basic(mock_isinstance, mock_mutagen_file, mock_exists):
    from mutagen.mp3 import MP3
    from mutagen.mp4 import MP4
    from mutagen.flac import FLAC

    audio = _make_mp3_mock()

    mock_mutagen_file.return_value = audio

    # Route isinstance checks
    def fake_isinstance(obj, cls):
        if cls is MP3:
            return True
        return False

    mock_isinstance.side_effect = fake_isinstance

    result = read_tags("/fake/song.mp3")
    assert result["format"] == "MP3"
    assert result["duration_ms"] == 210500


# A cleaner approach: test without mocking isinstance (use a real MP3 subclass mock)

def _build_mp3_audio(duration=180.0, title="My Song", artist="Artist",
                     album="Album", genre="Pop", track="1/10", year="2020",
                     artwork_bytes=None):
    """Build a mock that passes isinstance(audio, MP3) via spec."""
    from mutagen.mp3 import MP3

    audio = MagicMock(spec=MP3)
    info = MagicMock()
    info.length = duration
    audio.info = info

    tags = {}

    def make_tag(value):
        t = MagicMock()
        t.__str__ = lambda s: value
        return t

    if title:
        tags["TIT2"] = make_tag(title)
    if artist:
        tags["TPE1"] = make_tag(artist)
    if album:
        tags["TALB"] = make_tag(album)
    if genre:
        tags["TCON"] = make_tag(genre)
    if track:
        tags["TRCK"] = make_tag(track)
    if year:
        tdrc = MagicMock()
        year_val = MagicMock()
        year_val.__str__ = lambda s: year
        tdrc.text = [year_val]
        tags["TDRC"] = tdrc

    if artwork_bytes:
        apic = MagicMock()
        apic.data = artwork_bytes
        tags["APIC:Cover"] = apic
        audio.tags = MagicMock()
        audio.tags.get = lambda k: tags.get(k)
        audio.tags.keys = lambda: list(tags.keys())
        audio.tags.__getitem__ = lambda s, k: tags[k]
    else:
        audio.tags = MagicMock()
        audio.tags.get = lambda k: tags.get(k)
        audio.tags.keys = lambda: list(tags.keys())

    return audio


@patch("aethermedia.metadata.tag_reader.os.path.exists", return_value=True)
@patch("aethermedia.metadata.tag_reader.MutagenFile")
def test_read_tags_mp3_full(mock_mutagen_file, mock_exists):
    from mutagen.mp3 import MP3

    audio = _build_mp3_audio(duration=180.0, title="My Song", artist="Test Artist",
                              album="My Album", genre="Jazz", track="2/8", year="2019")
    mock_mutagen_file.return_value = audio

    result = read_tags("/fake/song.mp3")

    assert result["format"] == "MP3"
    assert result["duration_ms"] == 180_000
    # Title comes through _first_str which calls str() on the mock
    assert result["title"] == "My Song"
    assert result["artist"] == "Test Artist"
    assert result["album"] == "My Album"
    assert result["genre"] == "Jazz"
    assert result["track_number"] == 2
    assert result["year"] == 2019
    assert result["artwork_bytes"] is None


@patch("aethermedia.metadata.tag_reader.os.path.exists", return_value=True)
@patch("aethermedia.metadata.tag_reader.MutagenFile")
def test_read_tags_mp3_with_artwork(mock_mutagen_file, mock_exists):
    from mutagen.mp3 import MP3

    raw_art = b"\xff\xd8\xff\xe0fake_jpeg"
    audio = _build_mp3_audio(artwork_bytes=raw_art)
    mock_mutagen_file.return_value = audio

    result = read_tags("/fake/cover.mp3")
    assert result["artwork_bytes"] == raw_art


@patch("aethermedia.metadata.tag_reader.os.path.exists", return_value=True)
@patch("aethermedia.metadata.tag_reader.MutagenFile")
def test_read_tags_mp3_no_tags(mock_mutagen_file, mock_exists):
    from mutagen.mp3 import MP3

    audio = MagicMock(spec=MP3)
    audio.info = MagicMock()
    audio.info.length = 60.0
    audio.tags = None
    mock_mutagen_file.return_value = audio

    result = read_tags("/fake/notags.mp3")
    assert result["format"] == "MP3"
    assert result["title"] is None
    assert result["duration_ms"] == 60_000


# ── read_tags — MP4 ───────────────────────────────────────────────────────────

@patch("aethermedia.metadata.tag_reader.os.path.exists", return_value=True)
@patch("aethermedia.metadata.tag_reader.MutagenFile")
def test_read_tags_mp4_basic(mock_mutagen_file, mock_exists):
    from mutagen.mp4 import MP4, MP4Cover

    audio = MagicMock(spec=MP4)
    audio.info = MagicMock()
    audio.info.length = 300.0

    tag_data = {
        "\xa9nam": ["MP4 Title"],
        "\xa9ART": ["MP4 Artist"],
        "\xa9alb": ["MP4 Album"],
        "\xa9gen": ["Electronic"],
        "trkn": [(5, 12)],
        "\xa9day": ["2022-06-15"],
    }
    audio.tags = MagicMock()
    audio.tags.get = lambda k: tag_data.get(k)

    mock_mutagen_file.return_value = audio

    result = read_tags("/fake/song.mp4")
    assert result["format"] == "MP4"
    assert result["duration_ms"] == 300_000
    assert result["title"] == "MP4 Title"
    assert result["artist"] == "MP4 Artist"
    assert result["album"] == "MP4 Album"
    assert result["genre"] == "Electronic"
    assert result["track_number"] == 5
    assert result["year"] == 2022


@patch("aethermedia.metadata.tag_reader.os.path.exists", return_value=True)
@patch("aethermedia.metadata.tag_reader.MutagenFile")
def test_read_tags_mp4_with_cover(mock_mutagen_file, mock_exists):
    from mutagen.mp4 import MP4, MP4Cover

    raw_art = b"\x89PNG\r\nfake_png"

    # bytes(cover) calls cover.__bytes__(); use a real bytearray-backed object
    # so that bytes() works without spec interference.
    cover = bytearray(raw_art)
    # Attach the MP4Cover class identity so isinstance(cover, MP4Cover) passes.
    # Instead, wrap in a simple class that subclasses bytes — same as mutagen does.
    class _FakeCover(bytes):
        pass

    cover_obj = _FakeCover(raw_art)

    audio = MagicMock(spec=MP4)
    audio.info = MagicMock()
    audio.info.length = 60.0

    tag_data = {"covr": [cover_obj]}
    audio.tags = MagicMock()
    audio.tags.get = lambda k: tag_data.get(k)

    # Make isinstance(cover_obj, MP4Cover) return True
    with patch("aethermedia.metadata.tag_reader.MP4Cover", _FakeCover):
        mock_mutagen_file.return_value = audio
        result = read_tags("/fake/cover.m4a")

    assert result["format"] == "MP4"
    assert result["artwork_bytes"] == raw_art


@patch("aethermedia.metadata.tag_reader.os.path.exists", return_value=True)
@patch("aethermedia.metadata.tag_reader.MutagenFile")
def test_read_tags_mp4_no_tags(mock_mutagen_file, mock_exists):
    from mutagen.mp4 import MP4

    audio = MagicMock(spec=MP4)
    audio.info = MagicMock()
    audio.info.length = 30.0
    audio.tags = None
    mock_mutagen_file.return_value = audio

    result = read_tags("/fake/notags.m4a")
    assert result["title"] is None
    assert result["format"] == "MP4"


# ── read_tags — FLAC ──────────────────────────────────────────────────────────

@patch("aethermedia.metadata.tag_reader.os.path.exists", return_value=True)
@patch("aethermedia.metadata.tag_reader.MutagenFile")
def test_read_tags_flac_basic(mock_mutagen_file, mock_exists):
    from mutagen.flac import FLAC

    audio = MagicMock(spec=FLAC)
    audio.info = MagicMock()
    audio.info.length = 240.0
    audio.pictures = []

    tag_data = {
        "title":       ["FLAC Title"],
        "artist":      ["FLAC Artist"],
        "album":       ["FLAC Album"],
        "genre":       ["Classical"],
        "tracknumber": ["7"],
        "date":        ["2018"],
    }
    audio.tags = MagicMock()
    audio.tags.get = lambda k: tag_data.get(k)

    mock_mutagen_file.return_value = audio

    result = read_tags("/fake/song.flac")
    assert result["format"] == "FLAC"
    assert result["duration_ms"] == 240_000
    assert result["title"] == "FLAC Title"
    assert result["artist"] == "FLAC Artist"
    assert result["track_number"] == 7
    assert result["year"] == 2018
    assert result["artwork_bytes"] is None


@patch("aethermedia.metadata.tag_reader.os.path.exists", return_value=True)
@patch("aethermedia.metadata.tag_reader.MutagenFile")
def test_read_tags_flac_with_picture(mock_mutagen_file, mock_exists):
    from mutagen.flac import FLAC, Picture

    raw_art = b"fake_picture_data"
    pic = MagicMock(spec=Picture)
    pic.data = raw_art

    audio = MagicMock(spec=FLAC)
    audio.info = MagicMock()
    audio.info.length = 120.0
    audio.pictures = [pic]
    audio.tags = MagicMock()
    audio.tags.get = lambda k: None

    mock_mutagen_file.return_value = audio

    result = read_tags("/fake/art.flac")
    assert result["artwork_bytes"] == raw_art


@patch("aethermedia.metadata.tag_reader.os.path.exists", return_value=True)
@patch("aethermedia.metadata.tag_reader.MutagenFile")
def test_read_tags_flac_year_from_year_tag(mock_mutagen_file, mock_exists):
    """FLAC can store year in 'year' key instead of 'date'."""
    from mutagen.flac import FLAC

    audio = MagicMock(spec=FLAC)
    audio.info = MagicMock()
    audio.info.length = 60.0
    audio.pictures = []

    tag_data = {"year": ["1999"]}
    audio.tags = MagicMock()
    audio.tags.get = lambda k: tag_data.get(k)

    mock_mutagen_file.return_value = audio

    result = read_tags("/fake/old.flac")
    assert result["year"] == 1999


@patch("aethermedia.metadata.tag_reader.os.path.exists", return_value=True)
@patch("aethermedia.metadata.tag_reader.MutagenFile")
def test_read_tags_flac_no_tags(mock_mutagen_file, mock_exists):
    from mutagen.flac import FLAC

    audio = MagicMock(spec=FLAC)
    audio.info = MagicMock()
    audio.info.length = 90.0
    audio.pictures = []
    audio.tags = None

    mock_mutagen_file.return_value = audio

    result = read_tags("/fake/novc.flac")
    assert result["title"] is None
    assert result["format"] == "FLAC"


# ── read_tags — Generic / Vorbis (OGG etc.) ───────────────────────────────────

@patch("aethermedia.metadata.tag_reader.os.path.exists", return_value=True)
@patch("aethermedia.metadata.tag_reader.MutagenFile")
def test_read_tags_generic_vorbis(mock_mutagen_file, mock_exists):
    """A generic mutagen object (not MP3/MP4/FLAC) goes through the generic path."""
    # Use a plain MagicMock (not spec=MP3/MP4/FLAC) so isinstance returns False for all
    audio = MagicMock()
    audio.__class__.__name__ = "OggVorbis"
    audio.info = MagicMock()
    audio.info.length = 150.0

    tag_data = {
        "title":       ["OGG Song"],
        "artist":      ["OGG Artist"],
        "album":       ["OGG Album"],
        "genre":       ["Indie"],
        "tracknumber": ["3/10"],
        "date":        ["2023"],
    }
    tags_mock = MagicMock()
    tags_mock.get = lambda k: tag_data.get(k)
    audio.tags = tags_mock

    mock_mutagen_file.return_value = audio

    result = read_tags("/fake/song.ogg")
    assert result["duration_ms"] == 150_000
    assert result["title"] == "OGG Song"
    assert result["artist"] == "OGG Artist"
    assert result["track_number"] == 3
    assert result["year"] == 2023


@patch("aethermedia.metadata.tag_reader.os.path.exists", return_value=True)
@patch("aethermedia.metadata.tag_reader.MutagenFile")
def test_read_tags_generic_no_info(mock_mutagen_file, mock_exists):
    """Audio object with no info attribute returns None duration."""
    audio = MagicMock(spec=[])  # no attributes at all
    audio.tags = None
    mock_mutagen_file.return_value = audio

    result = read_tags("/fake/weird.ogg")
    assert result["duration_ms"] is None


@patch("aethermedia.metadata.tag_reader.os.path.exists", return_value=True)
@patch("aethermedia.metadata.tag_reader.MutagenFile")
def test_read_tags_generic_year_from_year_key(mock_mutagen_file, mock_exists):
    """Generic path: falls back to 'year' tag when 'date' is absent."""
    audio = MagicMock()
    audio.__class__.__name__ = "OggOpus"
    audio.info = MagicMock()
    audio.info.length = 60.0

    tag_data = {"year": ["2000"]}
    tags_mock = MagicMock()
    tags_mock.get = lambda k: tag_data.get(k)
    audio.tags = tags_mock

    mock_mutagen_file.return_value = audio

    result = read_tags("/fake/opus.ogg")
    assert result["year"] == 2000
