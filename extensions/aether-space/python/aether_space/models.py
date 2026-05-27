from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime
from enum import Enum
from typing import Optional
from uuid import UUID, uuid4


class BreadcrumbType(str, Enum):
    POST = "POST"
    EVENT = "EVENT"
    ALERT = "ALERT"
    OFFER = "OFFER"
    NOTICE = "NOTICE"
    PINNED = "PINNED"


class GeoHash(str):
    """String subclass representing a GeoHash-encoded location."""

    _BASE32 = "0123456789bcdefghjkmnpqrstuvwxyz"

    def __new__(cls, value: str) -> "GeoHash":
        if not value or len(value) > 12:
            raise ValueError("GeoHash must be 1-12 characters")
        return super().__new__(cls, value)

    @classmethod
    def from_coordinates(cls, lat: float, lon: float, precision: int = 6) -> "GeoHash":
        """Encode lat/lon into a GeoHash string.

        Args:
            lat: Latitude in degrees (-90 to 90).
            lon: Longitude in degrees (-180 to 180).
            precision: Number of characters (1-12, default 6).

        Returns:
            A GeoHash instance containing the encoded string.
        """
        if not -90.0 <= lat <= 90.0:
            raise ValueError("Latitude must be in [-90, 90]")
        if not -180.0 <= lon <= 180.0:
            raise ValueError("Longitude must be in [-180, 180]")
        if not 1 <= precision <= 12:
            raise ValueError("Precision must be between 1 and 12")

        min_lat, max_lat = -90.0, 90.0
        min_lon, max_lon = -180.0, 180.0

        hash_chars: list[str] = []
        is_even = True
        bit = 0
        ch = 0

        while len(hash_chars) < precision:
            if is_even:
                mid = (min_lon + max_lon) / 2.0
                if lon >= mid:
                    ch |= 1 << (4 - bit)
                    min_lon = mid
                else:
                    max_lon = mid
            else:
                mid = (min_lat + max_lat) / 2.0
                if lat >= mid:
                    ch |= 1 << (4 - bit)
                    min_lat = mid
                else:
                    max_lat = mid

            is_even = not is_even
            if bit < 4:
                bit += 1
            else:
                hash_chars.append(cls._BASE32[ch])
                bit = 0
                ch = 0

        return cls("".join(hash_chars))


@dataclass
class SpaceBreadcrumb:
    space_id: UUID
    author_id: UUID
    geo_hash: str
    type: BreadcrumbType
    title: str
    body: str
    id: UUID = field(default_factory=uuid4)
    media_urls: list[str] = field(default_factory=list)
    tags: list[str] = field(default_factory=list)
    expires_at: Optional[datetime] = None
    is_pinned: bool = False
    reaction_count: int = 0
    reply_count: int = 0
    created_at: datetime = field(default_factory=datetime.utcnow)
    updated_at: datetime = field(default_factory=datetime.utcnow)
