"""Tests for aethernet_media.playlist.xspf_parser."""

from __future__ import annotations

import xml.etree.ElementTree as ET

import pytest

from aethernet_media.playlist.xspf_parser import parse_xspf


_NS = "http://xspf.org/ns/0/"


def _xspf(tracklist_body: str, *, ns: bool = True) -> str:
    """Wrap track XML inside a minimal XSPF document."""
    if ns:
        return (
            f'<?xml version="1.0" encoding="UTF-8"?>'
            f'<playlist version="1" xmlns="{_NS}">'
            f"<trackList>{tracklist_body}</trackList>"
            f"</playlist>"
        )
    # Without namespace declaration
    return (
        '<?xml version="1.0" encoding="UTF-8"?>'
        "<playlist>"
        f"<trackList>{tracklist_body}</trackList>"
        "</playlist>"
    )


# ── Basic parsing ─────────────────────────────────────────────────────────────

def test_parse_xspf_empty_tracklist():
    doc = _xspf("")
    tracks = parse_xspf(doc)
    assert tracks == []


def test_parse_xspf_single_track_full():
    doc = _xspf(
        "<track>"
        "<title>My Track</title>"
        "<creator>My Artist</creator>"
        "<album>My Album</album>"
        "<duration>272000</duration>"
        "<location>http://example.com/track.mp3</location>"
        "<image>http://example.com/cover.jpg</image>"
        "<annotation>A great song</annotation>"
        "</track>"
    )
    tracks = parse_xspf(doc)
    assert len(tracks) == 1
    t = tracks[0]
    assert t["title"] == "My Track"
    assert t["creator"] == "My Artist"
    assert t["album"] == "My Album"
    assert t["duration"] == 272000
    assert t["location"] == ["http://example.com/track.mp3"]
    assert t["image"] == "http://example.com/cover.jpg"
    assert t["annotation"] == "A great song"


def test_parse_xspf_multiple_tracks():
    doc = _xspf(
        "<track><title>Alpha</title><duration>60000</duration>"
        "<location>http://a.com/a.mp3</location></track>"
        "<track><title>Beta</title><duration>120000</duration>"
        "<location>http://a.com/b.mp3</location></track>"
    )
    tracks = parse_xspf(doc)
    assert len(tracks) == 2
    assert tracks[0]["title"] == "Alpha"
    assert tracks[1]["title"] == "Beta"
    assert tracks[0]["duration"] == 60000
    assert tracks[1]["duration"] == 120000


def test_parse_xspf_missing_optional_fields_are_none():
    doc = _xspf("<track><location>http://example.com/x.mp3</location></track>")
    tracks = parse_xspf(doc)
    t = tracks[0]
    assert t["title"] is None
    assert t["creator"] is None
    assert t["album"] is None
    assert t["image"] is None
    assert t["annotation"] is None


def test_parse_xspf_missing_duration_is_none():
    doc = _xspf("<track><title>No Duration</title>"
                "<location>http://example.com/x.mp3</location></track>")
    tracks = parse_xspf(doc)
    assert tracks[0]["duration"] is None


def test_parse_xspf_invalid_duration_is_none():
    doc = _xspf("<track><duration>not_a_number</duration>"
                "<location>http://example.com/x.mp3</location></track>")
    tracks = parse_xspf(doc)
    assert tracks[0]["duration"] is None


def test_parse_xspf_multiple_locations():
    doc = _xspf(
        "<track>"
        "<title>Multi</title>"
        "<location>http://cdn1.example.com/t.mp3</location>"
        "<location>http://cdn2.example.com/t.mp3</location>"
        "</track>"
    )
    tracks = parse_xspf(doc)
    assert len(tracks[0]["location"]) == 2
    assert "http://cdn1.example.com/t.mp3" in tracks[0]["location"]
    assert "http://cdn2.example.com/t.mp3" in tracks[0]["location"]


def test_parse_xspf_no_location_gives_empty_list():
    doc = _xspf("<track><title>No URL</title></track>")
    tracks = parse_xspf(doc)
    assert tracks[0]["location"] == []


# ── Whitespace trimming ───────────────────────────────────────────────────────

def test_parse_xspf_whitespace_trimmed():
    doc = _xspf(
        "<track>"
        "<title>  Spaced Title  </title>"
        "<creator>  Artist  </creator>"
        "</track>"
    )
    tracks = parse_xspf(doc)
    assert tracks[0]["title"] == "Spaced Title"
    assert tracks[0]["creator"] == "Artist"


def test_parse_xspf_blank_text_becomes_none():
    doc = _xspf("<track><title>   </title></track>")
    tracks = parse_xspf(doc)
    assert tracks[0]["title"] is None


# ── No-namespace variant ──────────────────────────────────────────────────────

def test_parse_xspf_no_namespace():
    doc = _xspf(
        "<track><title>NS-free</title><duration>5000</duration>"
        "<location>http://example.com/t.mp3</location></track>",
        ns=False,
    )
    tracks = parse_xspf(doc)
    assert len(tracks) == 1
    assert tracks[0]["title"] == "NS-free"
    assert tracks[0]["duration"] == 5000


# ── Error handling ────────────────────────────────────────────────────────────

def test_parse_xspf_malformed_xml_raises():
    with pytest.raises(ET.ParseError):
        parse_xspf("<not valid xml<<<")


def test_parse_xspf_wrong_root_element_raises():
    with pytest.raises(ValueError, match="Root element is not"):
        parse_xspf('<media xmlns="http://xspf.org/ns/0/"><trackList></trackList></media>')


def test_parse_xspf_no_tracklist_returns_empty():
    """A valid playlist element with no trackList element returns []."""
    doc = f'<playlist version="1" xmlns="{_NS}"></playlist>'
    tracks = parse_xspf(doc)
    assert tracks == []


# ── Duration edge cases ───────────────────────────────────────────────────────

def test_parse_xspf_zero_duration():
    doc = _xspf("<track><duration>0</duration></track>")
    tracks = parse_xspf(doc)
    assert tracks[0]["duration"] == 0


def test_parse_xspf_large_duration():
    doc = _xspf("<track><duration>86400000</duration></track>")
    tracks = parse_xspf(doc)
    assert tracks[0]["duration"] == 86400000
