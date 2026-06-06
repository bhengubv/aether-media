"""pytest tests for Aether Media Python models and parsers."""

from __future__ import annotations

import pytest
import time


# ── MediaContent.formatted_duration ───────────────────────────────────────────

from aethermesh_media.models import MediaContent, MediaReaction, MediaReactionType


def _make_content(duration_ms: int) -> MediaContent:
    return MediaContent(
        content_hash="abc123",
        title="Test",
        duration_ms=duration_ms,
        codec="h264",
        content_type="video/mp4",
        creator_uhid="uhid-1",
        size_bytes=1_000_000,
        created_at_ms=int(time.time() * 1000),
    )


def test_formatted_duration_live():
    assert _make_content(0).formatted_duration == "Live"


def test_formatted_duration_negative_is_live():
    assert _make_content(-1).formatted_duration == "Live"


def test_formatted_duration_sub_hour():
    # 4 minutes 32 seconds = 272 000 ms
    c = _make_content(272_000)
    assert c.formatted_duration == "4:32"


def test_formatted_duration_exactly_one_hour():
    # 3 600 000 ms = 1:00:00
    c = _make_content(3_600_000)
    assert c.formatted_duration == "1:00:00"


def test_formatted_duration_over_hour():
    # 1h 23m 45s = 5025000 ms
    c = _make_content(5_025_000)
    assert c.formatted_duration == "1:23:45"


def test_formatted_duration_seconds_padded():
    # 1 minute 5 seconds = 65 000 ms
    c = _make_content(65_000)
    assert c.formatted_duration == "1:05"


def test_is_video_true():
    c = _make_content(1000)
    assert c.is_video is True


def test_is_video_false_for_audio():
    c = MediaContent(
        content_hash="x", title="t", duration_ms=1000, codec="aac",
        content_type="audio/mp4", creator_uhid="u", size_bytes=1,
        created_at_ms=0,
    )
    assert c.is_video is False
    assert c.is_audio is True


# ── MediaReaction validation ───────────────────────────────────────────────────

def _make_reaction(**kwargs):
    defaults = dict(
        reaction_id="r1",
        content_hash="abc123",
        from_uhid="uhid-1",
        type=MediaReactionType.LIKE,
        position_ms=0,
        message=None,
        sent_at_ms=int(time.time() * 1000),
    )
    defaults.update(kwargs)
    return MediaReaction(**defaults)


def test_reaction_like_valid():
    r = _make_reaction()
    assert r.type == MediaReactionType.LIKE
    assert r.message is None


def test_reaction_comment_requires_message():
    with pytest.raises(ValueError, match="message is required"):
        _make_reaction(type=MediaReactionType.COMMENT, message=None)


def test_reaction_comment_empty_string_rejected():
    with pytest.raises(ValueError, match="message is required"):
        _make_reaction(type=MediaReactionType.COMMENT, message="   ")


def test_reaction_comment_with_message_valid():
    r = _make_reaction(type=MediaReactionType.COMMENT, message="Great stream!")
    assert r.message == "Great stream!"


def test_reaction_non_comment_with_message_rejected():
    with pytest.raises(ValueError, match="must be None"):
        _make_reaction(type=MediaReactionType.LIKE, message="oops")


def test_reaction_negative_position_rejected():
    with pytest.raises(ValueError, match="position_ms must be"):
        _make_reaction(position_ms=-1)


def test_reaction_empty_content_hash_rejected():
    with pytest.raises(ValueError, match="content_hash"):
        _make_reaction(content_hash="  ")


# ── M3U parser ─────────────────────────────────────────────────────────────────

from aethermesh_media.playlist.m3u_parser import parse_m3u


def test_m3u_basic():
    content = "#EXTM3U\n#EXTINF:272,Track One\nhttp://example.com/one.mp3\n"
    tracks = parse_m3u(content)
    assert len(tracks) == 1
    assert tracks[0]["title"] == "Track One"
    assert tracks[0]["url"] == "http://example.com/one.mp3"
    assert tracks[0]["duration"] == 272.0


def test_m3u_live_duration():
    content = "#EXTINF:-1,Live Radio\nhttp://stream.example.com/live\n"
    tracks = parse_m3u(content)
    assert tracks[0]["duration"] == -1.0
    assert tracks[0]["title"] == "Live Radio"


def test_m3u_plain_url_no_extinf():
    content = "http://example.com/video.mp4\n"
    tracks = parse_m3u(content)
    assert len(tracks) == 1
    assert tracks[0]["url"] == "http://example.com/video.mp4"
    assert tracks[0]["title"] == ""


def test_m3u_hls_variant():
    content = (
        "#EXTM3U\n"
        '#EXT-X-STREAM-INF:BANDWIDTH=800000,RESOLUTION=640x360,CODECS="avc1.42e01e,mp4a.40.2"\n'
        "http://example.com/360p.m3u8\n"
        '#EXT-X-STREAM-INF:BANDWIDTH=2000000,RESOLUTION=1280x720\n'
        "http://example.com/720p.m3u8\n"
    )
    entries = parse_m3u(content)
    assert len(entries) == 2
    assert entries[0]["is_variant"] is True
    assert entries[0]["bandwidth"] == 800000
    assert entries[0]["resolution"] == "640x360"
    assert entries[1]["bandwidth"] == 2000000
    assert entries[1]["url"] == "http://example.com/720p.m3u8"


def test_m3u_multiple_tracks():
    content = (
        "#EXTM3U\n"
        "#EXTINF:180,Alpha\nhttp://example.com/a.mp3\n"
        "#EXTINF:240,Beta\nhttp://example.com/b.mp3\n"
    )
    tracks = parse_m3u(content)
    assert len(tracks) == 2
    assert tracks[0]["title"] == "Alpha"
    assert tracks[1]["title"] == "Beta"


# ── tag_reader (offline — no real file required for import test) ───────────────

def test_tag_reader_raises_file_not_found():
    from aethermesh_media.metadata.tag_reader import read_tags
    with pytest.raises(FileNotFoundError):
        read_tags("/nonexistent/path/file.mp3")
