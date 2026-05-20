"""Domain models for Aether Media, mirroring the C# core types."""

from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime, timezone
from enum import IntEnum
from typing import Optional


class MediaReactionType(IntEnum):
    LIKE        = 1
    SHARE       = 2
    COMMENT     = 3
    SUPER_REACT = 4


@dataclass(frozen=True)
class MediaContent:
    content_hash:   str
    title:          str
    duration_ms:    int
    codec:          str
    content_type:   str
    creator_uhid:   str
    size_bytes:     int
    created_at:     datetime
    thumbnail_hash: Optional[str]      = None
    tags:           tuple[str, ...]    = field(default_factory=tuple)

    @property
    def formatted_duration(self) -> str:
        """
        Returns:
          - "Live"     when duration_ms <= 0
          - "M:SS"     when < 1 hour  (e.g. "4:32")
          - "H:MM:SS"  when >= 1 hour (e.g. "1:23:45")
        """
        if self.duration_ms <= 0:
            return "Live"
        total_seconds = self.duration_ms // 1000
        hours   = total_seconds // 3600
        minutes = (total_seconds % 3600) // 60
        seconds = total_seconds % 60
        if hours > 0:
            return f"{hours}:{minutes:02d}:{seconds:02d}"
        return f"{minutes}:{seconds:02d}"

    @property
    def is_video(self) -> bool:
        return self.content_type.lower().startswith("video/")

    @property
    def is_audio(self) -> bool:
        return self.content_type.lower().startswith("audio/")


@dataclass
class MediaReaction:
    reaction_id:  str
    content_hash: str
    from_uhid:    str
    type:         MediaReactionType
    position_ms:  int
    message:      Optional[str]
    sent_at:      datetime

    def __post_init__(self) -> None:
        if not self.content_hash.strip():
            raise ValueError("content_hash must not be empty")
        if not self.from_uhid.strip():
            raise ValueError("from_uhid must not be empty")
        if self.position_ms < 0:
            raise ValueError("position_ms must be >= 0")
        if self.type == MediaReactionType.COMMENT:
            if not self.message or not self.message.strip():
                raise ValueError("A message is required for COMMENT reactions")
        else:
            if self.message is not None:
                raise ValueError(
                    f"message must be None for {self.type.name} reactions"
                )


@dataclass(frozen=True)
class MediaProfile:
    uhid:           str
    display_name:   str
    avatar_hash:    Optional[str]
    bio:            Optional[str]
    aether_tag:     str
    follower_count: int
    following_count: int
    content_count:  int
    is_verified:    bool
    joined_at:      datetime

    @property
    def short_bio(self) -> str:
        """
        Bio truncated to 120 characters at the last word boundary, with "…"
        appended.  Returns "" when bio is None or whitespace.
        """
        if not self.bio or not self.bio.strip():
            return ""
        trimmed = self.bio.strip()
        if len(trimmed) <= 120:
            return trimmed
        cut = trimmed[:120]
        last_space = cut.rfind(" ")
        boundary = last_space if last_space > 0 else 120
        return cut[:boundary].rstrip() + "…"


@dataclass
class LiveStream:
    stream_id:          str
    title:              str
    creator_uhid:       str
    codec:              str
    segment_duration_ms: int
    started_at:         datetime
    viewer_count:       int
    is_active:          bool
    tags:               tuple[str, ...] = field(default_factory=tuple)

    @property
    def elapsed_ms(self) -> int:
        """Wall-clock milliseconds since the broadcast started (UTC). Always >= 0."""
        now = datetime.now(tz=timezone.utc)
        started = self.started_at
        if started.tzinfo is None:
            started = started.replace(tzinfo=timezone.utc)
        elapsed = int((now - started).total_seconds() * 1000)
        return max(0, elapsed)

    @property
    def elapsed_formatted(self) -> str:
        total_seconds = self.elapsed_ms // 1000
        hours   = total_seconds // 3600
        minutes = (total_seconds % 3600) // 60
        seconds = total_seconds % 60
        if hours > 0:
            return f"{hours}:{minutes:02d}:{seconds:02d}"
        return f"{minutes}:{seconds:02d}"


@dataclass(frozen=True)
class MediaFeedItem:
    content:       MediaContent
    like_count:    int
    share_count:   int
    comment_count: int
    watch_count:   int
    is_live:       bool
    stream_id:     Optional[str]
    top_reactions: tuple[MediaReaction, ...] = field(default_factory=tuple)
    published_at:  datetime = field(default_factory=lambda: datetime.now(tz=timezone.utc))

    @property
    def is_new(self) -> bool:
        """True when published within the last 24 hours."""
        now = datetime.now(tz=timezone.utc)
        pub = self.published_at
        if pub.tzinfo is None:
            pub = pub.replace(tzinfo=timezone.utc)
        return (now - pub).total_seconds() < 86400

    @property
    def reaction_total(self) -> int:
        return self.like_count + self.share_count + self.comment_count
