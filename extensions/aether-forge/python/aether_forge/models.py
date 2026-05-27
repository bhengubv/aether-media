from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime
from typing import Optional
from uuid import UUID, uuid4


@dataclass
class ForgeEntry:
    package_id: str
    ecosystem: str
    version: str
    name: str
    checksum: str
    download_url: str
    id: UUID = field(default_factory=uuid4)
    description: str = ""
    author: str = ""
    license_id: str = ""
    size_bytes: int = 0
    checksum_algorithm: str = "sha256"
    mirror_urls: list[str] = field(default_factory=list)
    dependencies: list[str] = field(default_factory=list)
    tags: list[str] = field(default_factory=list)
    is_verified: bool = False
    download_count: int = 0
    cached_at: datetime = field(default_factory=datetime.utcnow)
    expires_at: Optional[datetime] = None
    metadata: dict[str, str] = field(default_factory=dict)


@dataclass
class ForgeStats:
    total_entries: int = 0
    total_size_bytes: int = 0
    total_downloads: int = 0
    unique_ecosystems: int = 0
    unique_packages: int = 0
    verified_packages: int = 0
    hit_rate: float = 0.0
    miss_rate: float = 0.0
    average_package_size_bytes: int = 0
    peak_downloads_per_hour: int = 0
    active_peers: int = 0
    last_updated: datetime = field(default_factory=datetime.utcnow)
    ecosystem_breakdown: dict[str, int] = field(default_factory=dict)


class PackageIdParser:
    """Static helpers for parsing ecosystem-specific package identifier strings."""

    @staticmethod
    def parse_pypi(package_id: str) -> tuple[str, str]:
        """Return (name, version) from a PyPI package id like 'requests==2.31.0'."""
        if "==" in package_id:
            name, version = package_id.split("==", 1)
            return name.strip(), version.strip()
        return package_id.strip(), "latest"

    @staticmethod
    def parse_npm(package_id: str) -> tuple[str, str]:
        """Return (name, version) from an npm package id like '@scope/pkg@1.0.0'."""
        if "@" in package_id.lstrip("@"):
            at_index = package_id.rindex("@")
            return package_id[:at_index], package_id[at_index + 1:]
        return package_id, "latest"

    @staticmethod
    def parse_maven(package_id: str) -> tuple[str, str, str]:
        """Return (groupId, artifactId, version) from 'com.example:lib:1.0.0'."""
        parts = package_id.split(":")
        if len(parts) == 3:
            return parts[0], parts[1], parts[2]
        if len(parts) == 2:
            return parts[0], parts[1], "latest"
        return package_id, "", "latest"
