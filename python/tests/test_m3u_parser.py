"""Tests for aethermedia.playlist.m3u_parser."""

from __future__ import annotations

import pytest

from aethermedia.playlist.m3u_parser import _parse_stream_inf_attrs, parse_m3u


# ── _parse_stream_inf_attrs ───────────────────────────────────────────────────

def test_parse_stream_inf_attrs_basic():
    attrs = _parse_stream_inf_attrs('BANDWIDTH=800000,RESOLUTION=640x360')
    assert attrs["BANDWIDTH"] == "800000"
    assert attrs["RESOLUTION"] == "640x360"


def test_parse_stream_inf_attrs_quoted_value():
    attrs = _parse_stream_inf_attrs('CODECS="avc1.42e01e,mp4a.40.2"')
    assert attrs["CODECS"] == "avc1.42e01e,mp4a.40.2"


def test_parse_stream_inf_attrs_mixed():
    line = 'BANDWIDTH=2000000,RESOLUTION=1280x720,CODECS="avc1.4d401f"'
    attrs = _parse_stream_inf_attrs(line)
    assert attrs["BANDWIDTH"] == "2000000"
    assert attrs["RESOLUTION"] == "1280x720"
    assert attrs["CODECS"] == "avc1.4d401f"


def test_parse_stream_inf_attrs_empty_string():
    attrs = _parse_stream_inf_attrs("")
    assert attrs == {}


def test_parse_stream_inf_attrs_unknown_keys_included():
    attrs = _parse_stream_inf_attrs("FRAME-RATE=29.97,HDCP-LEVEL=NONE")
    assert attrs["FRAME-RATE"] == "29.97"
    assert attrs["HDCP-LEVEL"] == "NONE"


# ── parse_m3u — basic ─────────────────────────────────────────────────────────

def test_parse_m3u_empty_string():
    assert parse_m3u("") == []


def test_parse_m3u_only_whitespace():
    assert parse_m3u("   \n\n  \t  \n") == []


def test_parse_m3u_single_plain_url():
    tracks = parse_m3u("http://example.com/file.mp3\n")
    assert len(tracks) == 1
    assert tracks[0]["url"] == "http://example.com/file.mp3"
    assert tracks[0]["title"] == ""
    assert tracks[0]["duration"] == -1.0


def test_parse_m3u_header_line_skipped():
    tracks = parse_m3u("#EXTM3U\nhttp://example.com/file.mp3\n")
    assert len(tracks) == 1
    assert tracks[0]["url"] == "http://example.com/file.mp3"


def test_parse_m3u_extinf_basic():
    content = "#EXTINF:180,My Song\nhttp://example.com/song.mp3\n"
    tracks = parse_m3u(content)
    assert len(tracks) == 1
    assert tracks[0]["title"] == "My Song"
    assert tracks[0]["duration"] == 180.0
    assert tracks[0]["url"] == "http://example.com/song.mp3"


def test_parse_m3u_extinf_live_minus_one():
    content = "#EXTINF:-1,Live Stream\nhttp://stream.example.com/live\n"
    tracks = parse_m3u(content)
    assert tracks[0]["duration"] == -1.0


def test_parse_m3u_extinf_fractional_duration():
    content = "#EXTINF:3.5,Short\nhttp://example.com/s.mp3\n"
    tracks = parse_m3u(content)
    assert tracks[0]["duration"] == 3.5


def test_parse_m3u_extinf_no_title():
    content = "#EXTINF:100,\nhttp://example.com/t.mp3\n"
    tracks = parse_m3u(content)
    assert tracks[0]["title"] == ""
    assert tracks[0]["duration"] == 100.0


def test_parse_m3u_multiple_tracks():
    content = (
        "#EXTM3U\n"
        "#EXTINF:120,First\nhttp://example.com/1.mp3\n"
        "#EXTINF:240,Second\nhttp://example.com/2.mp3\n"
        "#EXTINF:60,Third\nhttp://example.com/3.mp3\n"
    )
    tracks = parse_m3u(content)
    assert len(tracks) == 3
    assert [t["title"] for t in tracks] == ["First", "Second", "Third"]
    assert [t["duration"] for t in tracks] == [120.0, 240.0, 60.0]


def test_parse_m3u_unknown_directive_skipped():
    content = (
        "#EXTM3U\n"
        "#EXT-X-VERSION:3\n"
        "#EXT-X-TARGETDURATION:10\n"
        "#EXTINF:8,Segment\n"
        "http://example.com/seg.ts\n"
    )
    tracks = parse_m3u(content)
    assert len(tracks) == 1
    assert tracks[0]["title"] == "Segment"


def test_parse_m3u_empty_lines_ignored():
    content = "\n\n#EXTINF:30,Track\n\nhttp://example.com/t.mp3\n\n"
    tracks = parse_m3u(content)
    assert len(tracks) == 1


# ── parse_m3u — HLS variant ───────────────────────────────────────────────────

def test_parse_m3u_hls_variant_single():
    content = (
        "#EXTM3U\n"
        "#EXT-X-STREAM-INF:BANDWIDTH=800000,RESOLUTION=640x360\n"
        "http://example.com/360p.m3u8\n"
    )
    entries = parse_m3u(content)
    assert len(entries) == 1
    e = entries[0]
    assert e["is_variant"] is True
    assert e["bandwidth"] == 800000
    assert e["resolution"] == "640x360"
    assert e["url"] == "http://example.com/360p.m3u8"
    assert e["duration"] == -1.0
    assert e["title"] == ""


def test_parse_m3u_hls_variant_multiple():
    content = (
        "#EXTM3U\n"
        "#EXT-X-STREAM-INF:BANDWIDTH=400000,RESOLUTION=426x240\n"
        "http://example.com/240p.m3u8\n"
        "#EXT-X-STREAM-INF:BANDWIDTH=2000000,RESOLUTION=1280x720\n"
        "http://example.com/720p.m3u8\n"
    )
    entries = parse_m3u(content)
    assert len(entries) == 2
    assert entries[0]["bandwidth"] == 400000
    assert entries[1]["bandwidth"] == 2000000
    assert entries[1]["resolution"] == "1280x720"


def test_parse_m3u_hls_variant_with_codecs():
    content = (
        '#EXT-X-STREAM-INF:BANDWIDTH=1500000,CODECS="avc1.42e01e,mp4a.40.2"\n'
        "http://example.com/hd.m3u8\n"
    )
    entries = parse_m3u(content)
    assert entries[0]["codecs"] == "avc1.42e01e,mp4a.40.2"


def test_parse_m3u_hls_variant_no_resolution():
    content = (
        "#EXT-X-STREAM-INF:BANDWIDTH=1000000\n"
        "http://example.com/stream.m3u8\n"
    )
    entries = parse_m3u(content)
    assert entries[0]["resolution"] is None
    assert entries[0]["codecs"] is None


def test_parse_m3u_hls_variant_no_bandwidth():
    content = (
        "#EXT-X-STREAM-INF:RESOLUTION=1920x1080\n"
        "http://example.com/fhd.m3u8\n"
    )
    entries = parse_m3u(content)
    assert entries[0]["bandwidth"] is None


# ── parse_m3u — mixed content ─────────────────────────────────────────────────

def test_parse_m3u_mixed_plain_and_extinf():
    content = (
        "http://example.com/plain.mp3\n"
        "#EXTINF:90,Tagged\nhttp://example.com/tagged.mp3\n"
    )
    tracks = parse_m3u(content)
    assert len(tracks) == 2
    assert tracks[0]["title"] == ""
    assert tracks[1]["title"] == "Tagged"


def test_parse_m3u_local_file_path():
    content = "#EXTINF:200,Local Song\n/home/user/music/song.mp3\n"
    tracks = parse_m3u(content)
    assert tracks[0]["url"] == "/home/user/music/song.mp3"
    assert tracks[0]["title"] == "Local Song"


def test_parse_m3u_windows_path():
    content = "C:\\Users\\user\\Music\\song.mp3\n"
    tracks = parse_m3u(content)
    assert len(tracks) == 1
    assert "song.mp3" in tracks[0]["url"]
