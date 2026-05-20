"""
Read ID3 (MP3), MP4/M4A, FLAC, OGG, and other audio/video file tags
using the mutagen library.

Returns a dict with keys:
  - "title"          (str or None)
  - "artist"         (str or None)
  - "album"          (str or None)
  - "genre"          (str or None)
  - "duration_ms"    (int or None)  — duration in milliseconds
  - "track_number"   (int or None)
  - "year"           (int or None)
  - "artwork_bytes"  (bytes or None) — raw bytes of the embedded cover art
  - "format"         (str)          — e.g. "MP3", "MP4", "FLAC", "OGG"
"""

from __future__ import annotations

from typing import Optional
import os

from mutagen import File as MutagenFile
from mutagen.mp3 import MP3
from mutagen.mp4 import MP4, MP4Cover
from mutagen.flac import FLAC, Picture
from mutagen.id3 import ID3NoHeaderError


def _first_str(tag_value) -> Optional[str]:
    """Extract the first string value from a mutagen tag list-or-scalar."""
    if tag_value is None:
        return None
    if isinstance(tag_value, list):
        if not tag_value:
            return None
        tag_value = tag_value[0]
    return str(tag_value).strip() or None


def _parse_track_number(raw: Optional[str]) -> Optional[int]:
    """Parse "3/12" or "3" track number strings."""
    if raw is None:
        return None
    try:
        return int(str(raw).split("/")[0])
    except (ValueError, IndexError):
        return None


def read_tags(file_path: str) -> dict:
    """
    Read audio/video file tags.

    Args:
        file_path: Absolute or relative path to the media file.

    Returns:
        Dict with title, artist, album, genre, duration_ms, track_number,
        year, artwork_bytes, format.

    Raises:
        FileNotFoundError: If the file does not exist.
        mutagen.MutagenError: If the file cannot be parsed.
    """
    if not os.path.exists(file_path):
        raise FileNotFoundError(f"File not found: {file_path}")

    result: dict = {
        "title":         None,
        "artist":        None,
        "album":         None,
        "genre":         None,
        "duration_ms":   None,
        "track_number":  None,
        "year":          None,
        "artwork_bytes": None,
        "format":        "unknown",
    }

    audio = MutagenFile(file_path, easy=False)
    if audio is None:
        return result

    # ── Duration (available on all mutagen file objects) ───────────────────
    if hasattr(audio, "info") and audio.info is not None:
        info = audio.info
        if hasattr(info, "length") and info.length:
            result["duration_ms"] = int(info.length * 1000)

    # ── MP3 / ID3 ──────────────────────────────────────────────────────────
    if isinstance(audio, MP3):
        result["format"] = "MP3"
        tags = audio.tags
        if tags is not None:
            result["title"]  = _first_str(tags.get("TIT2"))
            result["artist"] = _first_str(tags.get("TPE1"))
            result["album"]  = _first_str(tags.get("TALB"))
            result["genre"]  = _first_str(tags.get("TCON"))
            trck = tags.get("TRCK")
            result["track_number"] = _parse_track_number(_first_str(trck))
            tdrc = tags.get("TDRC")
            if tdrc:
                try:
                    result["year"] = int(str(tdrc.text[0])[:4])
                except (IndexError, ValueError):
                    pass
            # Embedded cover art
            for key in tags.keys():
                if key.startswith("APIC"):
                    apic = tags[key]
                    if hasattr(apic, "data") and apic.data:
                        result["artwork_bytes"] = bytes(apic.data)
                    break
        return result

    # ── MP4 / M4A / M4V ───────────────────────────────────────────────────
    if isinstance(audio, MP4):
        result["format"] = "MP4"
        tags = audio.tags
        if tags is not None:
            result["title"]  = _first_str(tags.get("\xa9nam"))
            result["artist"] = _first_str(tags.get("\xa9ART"))
            result["album"]  = _first_str(tags.get("\xa9alb"))
            result["genre"]  = _first_str(tags.get("\xa9gen"))
            trkn = tags.get("trkn")
            if trkn and isinstance(trkn, list) and trkn:
                try:
                    result["track_number"] = int(trkn[0][0])
                except (IndexError, TypeError, ValueError):
                    pass
            yrr = tags.get("\xa9day")
            if yrr:
                try:
                    result["year"] = int(str(yrr[0])[:4])
                except (IndexError, ValueError):
                    pass
            covr = tags.get("covr")
            if covr and isinstance(covr, list) and isinstance(covr[0], MP4Cover):
                result["artwork_bytes"] = bytes(covr[0])
        return result

    # ── FLAC ───────────────────────────────────────────────────────────────
    if isinstance(audio, FLAC):
        result["format"] = "FLAC"
        vc = audio.tags  # VorbisComment
        if vc is not None:
            result["title"]  = _first_str(vc.get("title"))
            result["artist"] = _first_str(vc.get("artist"))
            result["album"]  = _first_str(vc.get("album"))
            result["genre"]  = _first_str(vc.get("genre"))
            trck = vc.get("tracknumber")
            result["track_number"] = _parse_track_number(_first_str(trck))
            yr = vc.get("date") or vc.get("year")
            if yr:
                try:
                    result["year"] = int(str(yr[0])[:4])
                except (IndexError, ValueError):
                    pass
        # Embedded pictures
        if audio.pictures:
            result["artwork_bytes"] = bytes(audio.pictures[0].data)
        return result

    # ── Generic VorbisComment (OGG Vorbis, Opus, Speex, etc.) ─────────────
    tags = getattr(audio, "tags", None)
    if tags is not None:
        fmt_name = type(audio).__name__
        result["format"] = fmt_name
        # VorbisComment exposes a dict-like interface
        if hasattr(tags, "get"):
            result["title"]  = _first_str(tags.get("title"))
            result["artist"] = _first_str(tags.get("artist"))
            result["album"]  = _first_str(tags.get("album"))
            result["genre"]  = _first_str(tags.get("genre"))
            trck = tags.get("tracknumber")
            result["track_number"] = _parse_track_number(_first_str(trck))
            yr = tags.get("date") or tags.get("year")
            if yr:
                try:
                    result["year"] = int(str(yr[0])[:4])
                except (IndexError, ValueError):
                    pass

    return result
