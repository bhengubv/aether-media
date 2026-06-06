"""
Aether Media CLI.

Commands:
  scan <directory>   — Walk a directory and print all media files found.
  info <file>        — Print ID3/MP4/FLAC tag metadata for a single file.
  playlist <file>    — Parse and print an M3U/M3U8 or XSPF playlist.

Usage:
  python -m aethermesh_media.cli scan /path/to/music
  python -m aethermesh_media.cli info /path/to/song.mp3
  python -m aethermesh_media.cli playlist /path/to/playlist.m3u
"""

from __future__ import annotations

import argparse
import os
import sys

# Media file extensions recognised by the scan command
_MEDIA_EXTENSIONS = {
    ".mp3", ".mp4", ".m4a", ".m4v", ".mkv", ".webm", ".ogg", ".opus",
    ".flac", ".wav", ".aiff", ".aac", ".ts", ".mov", ".avi",
}

_PLAYLIST_EXTENSIONS = {".m3u", ".m3u8", ".xspf"}


def cmd_scan(directory: str) -> int:
    """Walk `directory` and print every recognised media file."""
    if not os.path.isdir(directory):
        print(f"error: '{directory}' is not a directory", file=sys.stderr)
        return 1

    found = 0
    for root, _dirs, files in os.walk(directory):
        for fname in sorted(files):
            ext = os.path.splitext(fname)[1].lower()
            if ext in _MEDIA_EXTENSIONS:
                full = os.path.join(root, fname)
                size = os.path.getsize(full)
                print(f"{full}  ({size:,} bytes)")
                found += 1

    if found == 0:
        print(f"No media files found in '{directory}'")
    else:
        print(f"\n{found} file(s) found.")
    return 0


def cmd_info(file_path: str) -> int:
    """Print tag metadata for a single media file."""
    if not os.path.isfile(file_path):
        print(f"error: '{file_path}' does not exist or is not a file", file=sys.stderr)
        return 1

    try:
        from aethermesh_media.metadata.tag_reader import read_tags
        tags = read_tags(file_path)
    except ImportError as exc:
        print(f"error: mutagen is not installed — {exc}", file=sys.stderr)
        return 1
    except Exception as exc:
        print(f"error reading tags: {exc}", file=sys.stderr)
        return 1

    print(f"File:         {file_path}")
    print(f"Format:       {tags['format']}")
    print(f"Title:        {tags['title'] or '(unknown)'}")
    print(f"Artist:       {tags['artist'] or '(unknown)'}")
    print(f"Album:        {tags['album'] or '(unknown)'}")
    print(f"Genre:        {tags['genre'] or '(unknown)'}")
    print(f"Track:        {tags['track_number'] or '(unknown)'}")
    print(f"Year:         {tags['year'] or '(unknown)'}")
    if tags["duration_ms"] is not None:
        ms = tags["duration_ms"]
        total_s = ms // 1000
        m, s = divmod(total_s, 60)
        h, m = divmod(m, 60)
        if h:
            print(f"Duration:     {h}:{m:02d}:{s:02d}")
        else:
            print(f"Duration:     {m}:{s:02d}")
    else:
        print("Duration:     (unknown)")
    has_art = tags["artwork_bytes"] is not None
    print(f"Artwork:      {'yes (%d bytes)' % len(tags['artwork_bytes']) if has_art else 'none'}")
    return 0


def cmd_playlist(file_path: str) -> int:
    """Parse and print a playlist file."""
    if not os.path.isfile(file_path):
        print(f"error: '{file_path}' does not exist or is not a file", file=sys.stderr)
        return 1

    ext = os.path.splitext(file_path)[1].lower()
    with open(file_path, encoding="utf-8", errors="replace") as fh:
        content = fh.read()

    if ext == ".xspf":
        from aethermesh_media.playlist.xspf_parser import parse_xspf
        tracks = parse_xspf(content)
        print(f"XSPF playlist: {len(tracks)} track(s)\n")
        for i, t in enumerate(tracks, 1):
            locs = ", ".join(t.get("location") or []) or "(no location)"
            dur  = t.get("duration")
            dur_str = f"{dur} ms" if dur is not None else "unknown"
            print(f"  {i:3}. {t.get('title') or '(untitled)'}  [{dur_str}]")
            print(f"       {locs}")
    else:
        # Treat as M3U/M3U8
        from aethermesh_media.playlist.m3u_parser import parse_m3u
        entries = parse_m3u(content)
        print(f"M3U playlist: {len(entries)} entry/entries\n")
        for i, e in enumerate(entries, 1):
            dur  = e.get("duration", -1)
            dur_str = f"{dur:.1f}s" if dur >= 0 else "live/unknown"
            variant = "  [VARIANT]" if e.get("is_variant") else ""
            bw   = e.get("bandwidth")
            bw_str = f"  bw={bw}" if bw else ""
            print(f"  {i:3}. {e.get('title') or e['url']}  [{dur_str}]{variant}{bw_str}")
            if e.get("title"):
                print(f"       {e['url']}")

    return 0


def main() -> None:
    parser = argparse.ArgumentParser(
        prog="aether-media",
        description="Aether Media CLI — scan, inspect, and parse media files",
    )
    subparsers = parser.add_subparsers(dest="command", required=True)

    # scan
    p_scan = subparsers.add_parser("scan", help="Walk a directory and list media files")
    p_scan.add_argument("directory", help="Directory to scan")

    # info
    p_info = subparsers.add_parser("info", help="Print metadata tags for a media file")
    p_info.add_argument("file", help="Path to the media file")

    # playlist
    p_pl = subparsers.add_parser("playlist", help="Parse and display a playlist file")
    p_pl.add_argument("file", help="Path to an M3U, M3U8, or XSPF playlist")

    args = parser.parse_args()

    if args.command == "scan":
        sys.exit(cmd_scan(args.directory))
    elif args.command == "info":
        sys.exit(cmd_info(args.file))
    elif args.command == "playlist":
        sys.exit(cmd_playlist(args.file))
    else:
        parser.print_help()
        sys.exit(1)


if __name__ == "__main__":
    main()
