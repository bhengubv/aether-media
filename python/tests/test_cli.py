"""Tests for aethermesh_media.cli.__main__."""

from __future__ import annotations

import os
import sys
from io import StringIO
from unittest.mock import MagicMock, patch, call

import pytest

from aethermesh_media.cli.__main__ import (
    cmd_info,
    cmd_playlist,
    cmd_scan,
    main,
    _MEDIA_EXTENSIONS,
    _PLAYLIST_EXTENSIONS,
)


# ── cmd_scan ──────────────────────────────────────────────────────────────────

def test_cmd_scan_not_a_directory(capsys):
    with patch("aethermesh_media.cli.__main__.os.path.isdir", return_value=False):
        rc = cmd_scan("/nonexistent/path")
    assert rc == 1
    captured = capsys.readouterr()
    assert "not a directory" in captured.err


def test_cmd_scan_empty_directory(tmp_path, capsys):
    rc = cmd_scan(str(tmp_path))
    assert rc == 0
    captured = capsys.readouterr()
    assert "No media files found" in captured.out


def test_cmd_scan_finds_media_files(tmp_path, capsys):
    (tmp_path / "song.mp3").write_bytes(b"fake mp3")
    (tmp_path / "video.mp4").write_bytes(b"fake mp4")
    (tmp_path / "readme.txt").write_bytes(b"text")  # should be ignored

    rc = cmd_scan(str(tmp_path))
    assert rc == 0
    captured = capsys.readouterr()
    assert "song.mp3" in captured.out
    assert "video.mp4" in captured.out
    assert "readme.txt" not in captured.out
    assert "2 file(s) found" in captured.out


def test_cmd_scan_nested_directories(tmp_path, capsys):
    sub = tmp_path / "sub"
    sub.mkdir()
    (sub / "track.flac").write_bytes(b"fake flac")
    (tmp_path / "album.ogg").write_bytes(b"fake ogg")

    rc = cmd_scan(str(tmp_path))
    assert rc == 0
    captured = capsys.readouterr()
    assert "track.flac" in captured.out
    assert "album.ogg" in captured.out
    assert "2 file(s) found" in captured.out


def test_cmd_scan_case_insensitive_extension(tmp_path, capsys):
    (tmp_path / "SONG.MP3").write_bytes(b"fake")
    rc = cmd_scan(str(tmp_path))
    assert rc == 0
    captured = capsys.readouterr()
    assert "SONG.MP3" in captured.out


def test_cmd_scan_all_media_extensions(tmp_path, capsys):
    """Every extension in _MEDIA_EXTENSIONS should be detected."""
    for ext in _MEDIA_EXTENSIONS:
        (tmp_path / f"file{ext}").write_bytes(b"x")

    rc = cmd_scan(str(tmp_path))
    assert rc == 0
    captured = capsys.readouterr()
    assert f"{len(_MEDIA_EXTENSIONS)} file(s) found" in captured.out


def test_cmd_scan_playlist_files_ignored(tmp_path, capsys):
    (tmp_path / "list.m3u").write_bytes(b"playlist")
    rc = cmd_scan(str(tmp_path))
    assert rc == 0
    captured = capsys.readouterr()
    assert "No media files found" in captured.out


# ── cmd_info ──────────────────────────────────────────────────────────────────

def test_cmd_info_file_not_found(capsys):
    with patch("aethermesh_media.cli.__main__.os.path.isfile", return_value=False):
        rc = cmd_info("/nonexistent/song.mp3")
    assert rc == 1
    captured = capsys.readouterr()
    assert "does not exist" in captured.err


def test_cmd_info_success(tmp_path, capsys):
    fake_file = tmp_path / "song.mp3"
    fake_file.write_bytes(b"fake")

    fake_tags = {
        "format": "MP3",
        "title": "My Song",
        "artist": "My Artist",
        "album": "My Album",
        "genre": "Rock",
        "track_number": 3,
        "year": 2020,
        "duration_ms": 272_000,
        "artwork_bytes": b"fake_art",
    }

    with patch("aethermesh_media.cli.__main__.os.path.isfile", return_value=True), \
         patch("aethermesh_media.metadata.tag_reader.read_tags", return_value=fake_tags):
        rc = cmd_info(str(fake_file))

    assert rc == 0
    captured = capsys.readouterr()
    assert "My Song" in captured.out
    assert "My Artist" in captured.out
    assert "MP3" in captured.out
    assert "4:32" in captured.out  # 272000ms = 4m32s
    assert "yes" in captured.out  # has artwork


def test_cmd_info_unknown_fields(tmp_path, capsys):
    fake_file = tmp_path / "song.mp3"
    fake_file.write_bytes(b"fake")

    fake_tags = {
        "format": "MP3",
        "title": None,
        "artist": None,
        "album": None,
        "genre": None,
        "track_number": None,
        "year": None,
        "duration_ms": None,
        "artwork_bytes": None,
    }

    with patch("aethermesh_media.cli.__main__.os.path.isfile", return_value=True), \
         patch("aethermesh_media.metadata.tag_reader.read_tags", return_value=fake_tags):
        rc = cmd_info(str(fake_file))

    assert rc == 0
    captured = capsys.readouterr()
    assert "(unknown)" in captured.out
    assert "none" in captured.out


def test_cmd_info_duration_over_one_hour(tmp_path, capsys):
    fake_file = tmp_path / "movie.mp4"
    fake_file.write_bytes(b"fake")

    fake_tags = {
        "format": "MP4",
        "title": "Film",
        "artist": None,
        "album": None,
        "genre": None,
        "track_number": None,
        "year": None,
        "duration_ms": 5_025_000,  # 1:23:45
        "artwork_bytes": None,
    }

    with patch("aethermesh_media.cli.__main__.os.path.isfile", return_value=True), \
         patch("aethermesh_media.metadata.tag_reader.read_tags", return_value=fake_tags):
        rc = cmd_info(str(fake_file))

    assert rc == 0
    captured = capsys.readouterr()
    assert "1:23:45" in captured.out


def test_cmd_info_read_tags_exception(tmp_path, capsys):
    fake_file = tmp_path / "bad.mp3"
    fake_file.write_bytes(b"bad")

    with patch("aethermesh_media.cli.__main__.os.path.isfile", return_value=True), \
         patch("aethermesh_media.metadata.tag_reader.read_tags",
               side_effect=Exception("corrupt file")):
        rc = cmd_info(str(fake_file))

    assert rc == 1
    captured = capsys.readouterr()
    assert "error reading tags" in captured.err


def test_cmd_info_mutagen_not_installed(tmp_path, capsys):
    fake_file = tmp_path / "song.mp3"
    fake_file.write_bytes(b"fake")

    with patch("aethermesh_media.cli.__main__.os.path.isfile", return_value=True), \
         patch("aethermesh_media.cli.__main__.read_tags" if False else
               "aethermesh_media.metadata.tag_reader.read_tags",
               side_effect=ImportError("no module named mutagen")):
        # Simulate ImportError being raised on the import inside cmd_info
        with patch.dict("sys.modules", {"aethermesh_media.metadata.tag_reader": None}):
            rc = cmd_info(str(fake_file))

    # When module import fails, we get ImportError path
    assert rc == 1


# ── cmd_playlist — M3U ───────────────────────────────────────────────────────

def test_cmd_playlist_file_not_found(capsys):
    with patch("aethermesh_media.cli.__main__.os.path.isfile", return_value=False):
        rc = cmd_playlist("/nonexistent/list.m3u")
    assert rc == 1
    captured = capsys.readouterr()
    assert "does not exist" in captured.err


def test_cmd_playlist_m3u(tmp_path, capsys):
    m3u = tmp_path / "list.m3u"
    m3u.write_text(
        "#EXTM3U\n#EXTINF:180,Track One\nhttp://example.com/1.mp3\n",
        encoding="utf-8",
    )
    rc = cmd_playlist(str(m3u))
    assert rc == 0
    captured = capsys.readouterr()
    assert "M3U playlist" in captured.out
    assert "1 entry" in captured.out
    assert "Track One" in captured.out


def test_cmd_playlist_m3u8(tmp_path, capsys):
    m3u8 = tmp_path / "stream.m3u8"
    m3u8.write_text(
        "#EXTM3U\n"
        "#EXT-X-STREAM-INF:BANDWIDTH=800000,RESOLUTION=640x360\n"
        "http://example.com/360p.m3u8\n",
        encoding="utf-8",
    )
    rc = cmd_playlist(str(m3u8))
    assert rc == 0
    captured = capsys.readouterr()
    assert "M3U playlist" in captured.out
    assert "VARIANT" in captured.out
    assert "bw=800000" in captured.out


def test_cmd_playlist_m3u_live_stream(tmp_path, capsys):
    m3u = tmp_path / "live.m3u"
    m3u.write_text(
        "#EXTINF:-1,Live Radio\nhttp://stream.example.com/live\n",
        encoding="utf-8",
    )
    rc = cmd_playlist(str(m3u))
    assert rc == 0
    captured = capsys.readouterr()
    assert "live/unknown" in captured.out


# ── cmd_playlist — XSPF ──────────────────────────────────────────────────────

def test_cmd_playlist_xspf(tmp_path, capsys):
    ns = "http://xspf.org/ns/0/"
    xspf = tmp_path / "playlist.xspf"
    xspf.write_text(
        f'<?xml version="1.0" encoding="UTF-8"?>'
        f'<playlist version="1" xmlns="{ns}">'
        f'<trackList>'
        f'<track><title>XSPF Track</title><duration>60000</duration>'
        f'<location>http://example.com/t.mp3</location></track>'
        f'</trackList></playlist>',
        encoding="utf-8",
    )
    rc = cmd_playlist(str(xspf))
    assert rc == 0
    captured = capsys.readouterr()
    assert "XSPF playlist" in captured.out
    assert "1 track" in captured.out
    assert "XSPF Track" in captured.out
    assert "60000 ms" in captured.out


def test_cmd_playlist_xspf_no_duration(tmp_path, capsys):
    ns = "http://xspf.org/ns/0/"
    xspf = tmp_path / "nodur.xspf"
    xspf.write_text(
        f'<?xml version="1.0" encoding="UTF-8"?>'
        f'<playlist version="1" xmlns="{ns}">'
        f'<trackList>'
        f'<track><location>http://example.com/t.mp3</location></track>'
        f'</trackList></playlist>',
        encoding="utf-8",
    )
    rc = cmd_playlist(str(xspf))
    assert rc == 0
    captured = capsys.readouterr()
    assert "unknown" in captured.out


def test_cmd_playlist_xspf_untitled_track(tmp_path, capsys):
    ns = "http://xspf.org/ns/0/"
    xspf = tmp_path / "notitle.xspf"
    xspf.write_text(
        f'<?xml version="1.0" encoding="UTF-8"?>'
        f'<playlist version="1" xmlns="{ns}">'
        f'<trackList><track>'
        f'<location>http://example.com/x.mp3</location>'
        f'<duration>1000</duration>'
        f'</track></trackList></playlist>',
        encoding="utf-8",
    )
    rc = cmd_playlist(str(xspf))
    assert rc == 0
    captured = capsys.readouterr()
    assert "(untitled)" in captured.out


# ── main() — argparse dispatch ────────────────────────────────────────────────

def test_main_scan_dispatches(tmp_path):
    with patch("aethermesh_media.cli.__main__.cmd_scan", return_value=0) as mock_scan, \
         patch("sys.argv", ["aether-media", "scan", str(tmp_path)]), \
         pytest.raises(SystemExit) as exc_info:
        main()
    mock_scan.assert_called_once_with(str(tmp_path))
    assert exc_info.value.code == 0


def test_main_info_dispatches(tmp_path):
    fake = tmp_path / "song.mp3"
    fake.write_bytes(b"x")
    with patch("aethermesh_media.cli.__main__.cmd_info", return_value=0) as mock_info, \
         patch("sys.argv", ["aether-media", "info", str(fake)]), \
         pytest.raises(SystemExit) as exc_info:
        main()
    mock_info.assert_called_once_with(str(fake))
    assert exc_info.value.code == 0


def test_main_playlist_dispatches(tmp_path):
    pl = tmp_path / "list.m3u"
    pl.write_bytes(b"x")
    with patch("aethermesh_media.cli.__main__.cmd_playlist", return_value=0) as mock_pl, \
         patch("sys.argv", ["aether-media", "playlist", str(pl)]), \
         pytest.raises(SystemExit) as exc_info:
        main()
    mock_pl.assert_called_once_with(str(pl))
    assert exc_info.value.code == 0


def test_main_no_command_exits_nonzero():
    with patch("sys.argv", ["aether-media"]), \
         pytest.raises(SystemExit) as exc_info:
        main()
    assert exc_info.value.code != 0


def test_main_scan_returns_exit_code_1(tmp_path):
    with patch("aethermesh_media.cli.__main__.cmd_scan", return_value=1) as mock_scan, \
         patch("sys.argv", ["aether-media", "scan", "/bad/path"]), \
         pytest.raises(SystemExit) as exc_info:
        main()
    assert exc_info.value.code == 1
