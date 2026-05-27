from __future__ import annotations

from abc import ABC, abstractmethod
from uuid import UUID

from .models import SpaceBreadcrumb


class SpaceService(ABC):
    """Abstract base class for geo-pinned noticeboard operations."""

    @abstractmethod
    async def drop_breadcrumb(self, breadcrumb: SpaceBreadcrumb) -> SpaceBreadcrumb:
        """Publish a breadcrumb to the mesh-local noticeboard."""
        ...

    @abstractmethod
    async def scan(self, geo_hash: str, radius_km: float = 5.0) -> list[SpaceBreadcrumb]:
        """Return all breadcrumbs within radius_km of the given geo_hash prefix."""
        ...

    @abstractmethod
    async def pin_breadcrumb(self, breadcrumb_id: UUID, space_id: UUID) -> SpaceBreadcrumb:
        """Pin a breadcrumb to the top of a space noticeboard."""
        ...

    @abstractmethod
    async def unpin_breadcrumb(self, breadcrumb_id: UUID, space_id: UUID) -> SpaceBreadcrumb:
        """Remove pin from a breadcrumb."""
        ...

    @abstractmethod
    async def delete_breadcrumb(self, breadcrumb_id: UUID, requester_id: UUID) -> bool:
        """Delete a breadcrumb. Returns True if deleted."""
        ...

    @abstractmethod
    async def get_by_id(self, breadcrumb_id: UUID) -> SpaceBreadcrumb | None:
        """Retrieve a single breadcrumb by its ID."""
        ...

    @abstractmethod
    async def list_by_space(
        self, space_id: UUID, limit: int = 50, offset: int = 0
    ) -> list[SpaceBreadcrumb]:
        """List breadcrumbs belonging to a space, newest first."""
        ...

    @abstractmethod
    async def react(self, breadcrumb_id: UUID, user_id: UUID, reaction: str) -> int:
        """Record a reaction. Returns updated reaction count."""
        ...


class SpaceServiceImpl(SpaceService):
    """Stub implementation — replace with real mesh-backed store."""

    async def drop_breadcrumb(self, breadcrumb: SpaceBreadcrumb) -> SpaceBreadcrumb:
        raise NotImplementedError("not implemented")

    async def scan(self, geo_hash: str, radius_km: float = 5.0) -> list[SpaceBreadcrumb]:
        raise NotImplementedError("not implemented")

    async def pin_breadcrumb(self, breadcrumb_id: UUID, space_id: UUID) -> SpaceBreadcrumb:
        raise NotImplementedError("not implemented")

    async def unpin_breadcrumb(self, breadcrumb_id: UUID, space_id: UUID) -> SpaceBreadcrumb:
        raise NotImplementedError("not implemented")

    async def delete_breadcrumb(self, breadcrumb_id: UUID, requester_id: UUID) -> bool:
        raise NotImplementedError("not implemented")

    async def get_by_id(self, breadcrumb_id: UUID) -> SpaceBreadcrumb | None:
        raise NotImplementedError("not implemented")

    async def list_by_space(
        self, space_id: UUID, limit: int = 50, offset: int = 0
    ) -> list[SpaceBreadcrumb]:
        raise NotImplementedError("not implemented")

    async def react(self, breadcrumb_id: UUID, user_id: UUID, reaction: str) -> int:
        raise NotImplementedError("not implemented")
