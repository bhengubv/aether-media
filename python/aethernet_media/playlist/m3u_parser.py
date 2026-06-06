"""
M3U / M3U8 playlist parser.

Handles:
 - Plain M3U (#EXTM3U header optional)
 - #EXTINF:-1,Title followed by a URL line
 - #EXT-X-STREAM-INF for HLS variant playlists (returns bandwidth + uri)
 - Comments (lines starting with # that are not recognised directives are skipped)
"""

from __future__ import annotations
import re
from typing import Optional


_EXTINF_RE = re.compile(
    r"#EXTINF:\s*(-?\d+(?:\.\d+)?)\s*(?:,(.*))?$"
)
_STREAM_INF_RE = re.compile(r"#EXT-X-STREAM-INF:([^\n]*)")
_KV_RE = re.compile(r'([A-Z0-9-]+)=(?:"([^"]*)"|([\w./@-]+))')


def _parse_stream_inf_attrs(attr_string: str) -> dict[str, str]:
    """Parse a comma-separated key=value (or key="value") attribute string."""
    result: dict[str, str] = {}
    for m in _KV_RE.finditer(attr_string):
        key = m.group(1)
        value = m.group(2) if m.group(2) is not None else m.group(3)
        result[key] = value
    return result


def parse_m3u(content: str) -> list[dict]:
    """
    Parse an M3U or M3U8 playlist string.

    Returns a list of track dicts.  Each dict always contains:
      - "url"      (str)  — the media URL or segment URI
      - "title"    (str)  — track title, "" if absent
      - "duration" (float) — declared duration in seconds, -1 for live/unknown

    HLS variant entries additionally contain:
      - "bandwidth"  (int or None)
      - "resolution" (str or None)
      - "codecs"     (str or None)
      - "is_variant" (True)
    """
    lines = content.splitlines()
    results: list[dict] = []

    pending_extinf: Optional[dict] = None          # from #EXTINF
    pending_stream_inf: Optional[dict] = None      # from #EXT-X-STREAM-INF

    for line in lines:
        line = line.strip()
        if not line:
            continue

        # ── #EXTINF ─────────────────────────────────────────────────────────
        m = _EXTINF_RE.match(line)
        if m:
            duration_s = float(m.group(1))
            title = (m.group(2) or "").strip()
            pending_extinf = {"duration": duration_s, "title": title}
            continue

        # ── #EXT-X-STREAM-INF ───────────────────────────────────────────────
        m2 = _STREAM_INF_RE.match(line)
        if m2:
            attrs = _parse_stream_inf_attrs(m2.group(1))
            bandwidth  = int(attrs["BANDWIDTH"]) if "BANDWIDTH" in attrs else None
            resolution = attrs.get("RESOLUTION")
            codecs     = attrs.get("CODECS")
            pending_stream_inf = {
                "bandwidth":  bandwidth,
                "resolution": resolution,
                "codecs":     codecs,
            }
            continue

        # ── Skip other directives ────────────────────────────────────────────
        if line.startswith("#"):
            continue

        # ── URL / path line ──────────────────────────────────────────────────
        url = line

        if pending_stream_inf is not None:
            entry: dict = {
                "url":        url,
                "title":      "",
                "duration":   -1.0,
                "is_variant": True,
                **pending_stream_inf,
            }
            results.append(entry)
            pending_stream_inf = None
        elif pending_extinf is not None:
            entry = {
                "url":      url,
                "title":    pending_extinf["title"],
                "duration": pending_extinf["duration"],
            }
            results.append(entry)
            pending_extinf = None
        else:
            # Plain URL with no preceding directive
            results.append({"url": url, "title": "", "duration": -1.0})

    return results
