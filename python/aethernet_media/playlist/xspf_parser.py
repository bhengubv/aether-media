"""
XSPF (XML Shareable Playlist Format) parser.

Spec: https://www.xspf.org/xspf-v1.html

Returns a list of track dicts with keys:
  - "title"    (str or None)
  - "creator"  (str or None) — artist / channel name
  - "album"    (str or None)
  - "duration" (int or None) — milliseconds as declared in <duration>
  - "location" (list[str])   — one or more <location> URIs
  - "image"    (str or None) — track artwork URI
  - "annotation" (str or None) — free-text note
"""

from __future__ import annotations

import xml.etree.ElementTree as ET
from typing import Optional


_NS = "http://xspf.org/ns/0/"


def _tag(local: str) -> str:
    return f"{{{_NS}}}{local}"


def _text(element: ET.Element, local: str) -> Optional[str]:
    child = element.find(_tag(local))
    if child is None or child.text is None:
        return None
    return child.text.strip() or None


def parse_xspf(content: str) -> list[dict]:
    """
    Parse an XSPF playlist XML string.

    Returns a list of track dicts as described in the module docstring.
    Raises xml.etree.ElementTree.ParseError on malformed XML.
    """
    root = ET.fromstring(content)

    # Support both with and without namespace declaration
    # Detect namespace from root tag
    if root.tag == _tag("playlist"):
        ns = _NS
    elif root.tag == "playlist":
        ns = ""
    else:
        raise ValueError(f"Root element is not <playlist>, got: {root.tag}")

    def ns_tag(local: str) -> str:
        return f"{{{ns}}}{local}" if ns else local

    def ns_text(element: ET.Element, local: str) -> Optional[str]:
        child = element.find(ns_tag(local))
        if child is None or child.text is None:
            return None
        return child.text.strip() or None

    tracklist_el = root.find(ns_tag("trackList"))
    if tracklist_el is None:
        return []

    tracks: list[dict] = []
    for track_el in tracklist_el.findall(ns_tag("track")):
        title      = ns_text(track_el, "title")
        creator    = ns_text(track_el, "creator")
        album      = ns_text(track_el, "album")
        annotation = ns_text(track_el, "annotation")
        image      = ns_text(track_el, "image")

        # <duration> is in milliseconds per spec
        duration_text = ns_text(track_el, "duration")
        duration_ms: Optional[int] = None
        if duration_text is not None:
            try:
                duration_ms = int(duration_text)
            except ValueError:
                pass

        # Collect all <location> elements (a track may have multiple)
        locations = [
            loc_el.text.strip()
            for loc_el in track_el.findall(ns_tag("location"))
            if loc_el.text and loc_el.text.strip()
        ]

        tracks.append({
            "title":      title,
            "creator":    creator,
            "album":      album,
            "duration":   duration_ms,
            "location":   locations,
            "image":      image,
            "annotation": annotation,
        })

    return tracks
