"""Domain models for Aether Media, mirroring the C# core types."""

from __future__ import annotations

from dataclasses import asdict, dataclass, field
from enum import IntEnum
from typing import Optional


class MediaReactionType(IntEnum):
    LIKE        = 1
    SHARE       = 2
    COMMENT     = 3
    SUPER_REACT = 4

    def to_wire(self) -> str:
        return {
            MediaReactionType.LIKE:        "like",
            MediaReactionType.SHARE:       "share",
            MediaReactionType.COMMENT:     "comment",
            MediaReactionType.SUPER_REACT: "super_react",
        }[self]

    @classmethod
    def from_wire(cls, value: str) -> "MediaReactionType":
        mapping = {
            "like":        cls.LIKE,
            "share":       cls.SHARE,
            "comment":     cls.COMMENT,
            "super_react": cls.SUPER_REACT,
        }
        if value not in mapping:
            raise ValueError(f"Unknown MediaReactionType wire value: {value!r}")
        return mapping[value]


@dataclass(frozen=True)
class MediaContent:
    content_hash:   str
    title:          str
    duration_ms:    int
    codec:          str
    content_type:   str
    creator_uhid:   str
    size_bytes:     int
    created_at_ms:  int
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

    @classmethod
    def from_dict(cls, d: dict) -> "MediaContent":
        """Constructs a MediaContent from a wire-format dict (snake_case)."""
        return cls(
            content_hash=d["content_hash"],
            title=d["title"],
            duration_ms=d["duration_ms"],
            codec=d["codec"],
            content_type=d["content_type"],
            creator_uhid=d["creator_uhid"],
            size_bytes=d["size_bytes"],
            created_at_ms=d["created_at_ms"],
            thumbnail_hash=d.get("thumbnail_hash"),
            tags=tuple(d.get("tags") or []),
        )

    def to_dict(self) -> dict:
        """Returns the canonical wire-format dict (snake_case, no datetime objects)."""
        d = asdict(self)
        d["tags"] = list(self.tags)
        return d


@dataclass
class MediaReaction:
    reaction_id:  str
    content_hash: str
    from_uhid:    str
    type:         MediaReactionType
    position_ms:  int
    message:      Optional[str]
    sent_at_ms:   int              # unix milliseconds

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

    @classmethod
    def from_dict(cls, d: dict) -> "MediaReaction":
        """Constructs a MediaReaction from a wire-format dict (snake_case)."""
        return cls(
            reaction_id=d["reaction_id"],
            content_hash=d["content_hash"],
            from_uhid=d["from_uhid"],
            type=MediaReactionType.from_wire(d["type"]),
            position_ms=d["position_ms"],
            message=d.get("message"),
            sent_at_ms=d["sent_at_ms"],
        )

    def to_dict(self) -> dict:
        """Returns the canonical wire-format dict (snake_case, lowercase type string)."""
        return {
            "reaction_id":  self.reaction_id,
            "content_hash": self.content_hash,
            "from_uhid":    self.from_uhid,
            "type":         self.type.to_wire(),
            "position_ms":  self.position_ms,
            "message":      self.message,
            "sent_at_ms":   self.sent_at_ms,
        }


@dataclass(frozen=True)
class MediaProfile:
    uhid:            str
    display_name:    str
    avatar_hash:     Optional[str]
    bio:             Optional[str]
    aethermesh_tag:      str
    follower_count:  int
    following_count: int
    content_count:   int
    is_verified:     bool
    joined_at_ms:    int           # unix milliseconds

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

    @classmethod
    def from_dict(cls, d: dict) -> "MediaProfile":
        """Constructs a MediaProfile from a wire-format dict (snake_case)."""
        return cls(
            uhid=d["uhid"],
            display_name=d["display_name"],
            avatar_hash=d.get("avatar_hash"),
            bio=d.get("bio"),
            aethermesh_tag=d["aethermesh_tag"],
            follower_count=d["follower_count"],
            following_count=d["following_count"],
            content_count=d["content_count"],
            is_verified=d["is_verified"],
            joined_at_ms=d["joined_at_ms"],
        )

    def to_dict(self) -> dict:
        """Returns the canonical wire-format dict (snake_case, no datetime objects)."""
        return {
            "uhid":            self.uhid,
            "display_name":    self.display_name,
            "avatar_hash":     self.avatar_hash,
            "bio":             self.bio,
            "aethermesh_tag":      self.aethermesh_tag,
            "follower_count":  self.follower_count,
            "following_count": self.following_count,
            "content_count":   self.content_count,
            "is_verified":     self.is_verified,
            "joined_at_ms":    self.joined_at_ms,
        }


@dataclass
class LiveStream:
    stream_id:           str
    title:               str
    creator_uhid:        str
    codec:               str
    segment_duration_ms: int
    started_at_ms:       int       # unix milliseconds
    viewer_count:        int
    is_active:           bool
    tags:                tuple[str, ...] = field(default_factory=tuple)

    @property
    def elapsed_ms(self) -> int:
        """Wall-clock milliseconds since the broadcast started (UTC). Always >= 0."""
        import time
        now_ms = int(time.time() * 1000)
        return max(0, now_ms - self.started_at_ms)

    @property
    def elapsed_formatted(self) -> str:
        total_seconds = self.elapsed_ms // 1000
        hours   = total_seconds // 3600
        minutes = (total_seconds % 3600) // 60
        seconds = total_seconds % 60
        if hours > 0:
            return f"{hours}:{minutes:02d}:{seconds:02d}"
        return f"{minutes}:{seconds:02d}"

    def to_dict(self) -> dict:
        """Returns the canonical wire-format dict."""
        return {
            "stream_id":            self.stream_id,
            "title":                self.title,
            "creator_uhid":         self.creator_uhid,
            "codec":                self.codec,
            "segment_duration_ms":  self.segment_duration_ms,
            "started_at_ms":        self.started_at_ms,
            "viewer_count":         self.viewer_count,
            "is_active":            self.is_active,
            "tags":                 list(self.tags),
        }


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
    published_at_ms: int = 0       # unix milliseconds

    @property
    def is_new(self) -> bool:
        """True when published within the last 24 hours."""
        import time
        now_ms = int(time.time() * 1000)
        return (now_ms - self.published_at_ms) < 86_400_000

    @property
    def reaction_total(self) -> int:
        return self.like_count + self.share_count + self.comment_count

    def to_dict(self) -> dict:
        """Returns the canonical wire-format dict."""
        return {
            "content":          self.content.to_dict(),
            "like_count":       self.like_count,
            "share_count":      self.share_count,
            "comment_count":    self.comment_count,
            "watch_count":      self.watch_count,
            "is_live":          self.is_live,
            "stream_id":        self.stream_id,
            "top_reactions":    [r.to_dict() for r in self.top_reactions],
            "published_at_ms":  self.published_at_ms,
        }
