from __future__ import annotations

from abc import ABC, abstractmethod
from uuid import UUID

from .models import ForgeEntry, ForgeStats


class ForgeService(ABC):
    """Abstract base class for mesh-native package cache proxy operations."""

    @abstractmethod
    async def query(
        self, package_id: str, ecosystem: str, version: str | None = None
    ) -> ForgeEntry | None:
        """Look up a cached package entry."""
        ...

    @abstractmethod
    async def cache(self, entry: ForgeEntry) -> ForgeEntry:
        """Store a package entry in the mesh cache."""
        ...

    @abstractmethod
    async def fetch(self, package_id: str, ecosystem: str, version: str) -> bytes:
        """Download the raw package bytes, pulling from mesh peers if needed."""
        ...

    @abstractmethod
    async def stats(self) -> ForgeStats:
        """Return aggregate cache statistics."""
        ...

    @abstractmethod
    async def evict(self, entry_id: UUID) -> bool:
        """Remove a cached entry. Returns True if it existed."""
        ...

    @abstractmethod
    async def list_by_ecosystem(
        self, ecosystem: str, limit: int = 50, offset: int = 0
    ) -> list[ForgeEntry]:
        """Page through cached entries for a given ecosystem."""
        ...

    @abstractmethod
    async def search(
        self, query: str, ecosystem: str | None = None
    ) -> list[ForgeEntry]:
        """Full-text search across cached package names and descriptions."""
        ...

    @abstractmethod
    async def sync(self, peer_node_id: str) -> int:
        """Sync cache index with a remote peer. Returns number of entries exchanged."""
        ...


class ForgeServiceImpl(ForgeService):
    """Stub implementation."""

    async def query(self, package_id: str, ecosystem: str, version: str | None = None) -> ForgeEntry | None:
        raise NotImplementedError("not implemented")

    async def cache(self, entry: ForgeEntry) -> ForgeEntry:
        raise NotImplementedError("not implemented")

    async def fetch(self, package_id: str, ecosystem: str, version: str) -> bytes:
        raise NotImplementedError("not implemented")

    async def stats(self) -> ForgeStats:
        raise NotImplementedError("not implemented")

    async def evict(self, entry_id: UUID) -> bool:
        raise NotImplementedError("not implemented")

    async def list_by_ecosystem(self, ecosystem: str, limit: int = 50, offset: int = 0) -> list[ForgeEntry]:
        raise NotImplementedError("not implemented")

    async def search(self, query: str, ecosystem: str | None = None) -> list[ForgeEntry]:
        raise NotImplementedError("not implemented")

    async def sync(self, peer_node_id: str) -> int:
        raise NotImplementedError("not implemented")
