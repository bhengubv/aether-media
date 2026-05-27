from __future__ import annotations

from abc import ABC, abstractmethod
from uuid import UUID

from .models import VaultHealth, VaultManifest, VaultShard


class VaultService(ABC):
    """Abstract base class for erasure-coded encrypted distributed backup."""

    @abstractmethod
    async def store(
        self,
        owner_id: UUID,
        name: str,
        data: bytes,
        tags: list[str] | None = None,
    ) -> VaultManifest:
        """Encode, encrypt, and distribute *data* across mesh nodes."""
        ...

    @abstractmethod
    async def recover(self, manifest_id: UUID, requester_id: UUID) -> bytes:
        """Reconstruct and decrypt original data from available shards."""
        ...

    @abstractmethod
    async def health(self, manifest_id: UUID) -> VaultHealth:
        """Return shard availability and recoverability status."""
        ...

    @abstractmethod
    async def delete(self, manifest_id: UUID, requester_id: UUID) -> bool:
        """Instruct all nodes to drop their shards. Returns True on success."""
        ...

    @abstractmethod
    async def list_manifests(
        self, owner_id: UUID, limit: int = 50, offset: int = 0
    ) -> list[VaultManifest]:
        """Page through manifests owned by *owner_id*."""
        ...

    @abstractmethod
    async def replicate_shard(self, shard_id: UUID, target_node_id: str) -> VaultShard:
        """Copy a shard to an additional node for higher redundancy."""
        ...

    @abstractmethod
    async def verify_shard(self, shard_id: UUID) -> bool:
        """Checksum-verify a shard on its host node. Returns True if intact."""
        ...

    @abstractmethod
    async def get_manifest(self, manifest_id: UUID) -> VaultManifest | None:
        """Retrieve a manifest by ID."""
        ...

    @abstractmethod
    async def get_shard(self, shard_id: UUID) -> VaultShard | None:
        """Retrieve a shard descriptor by ID."""
        ...


class VaultServiceImpl(VaultService):
    """Stub implementation."""

    async def store(self, owner_id: UUID, name: str, data: bytes, tags: list[str] | None = None) -> VaultManifest:
        raise NotImplementedError("not implemented")

    async def recover(self, manifest_id: UUID, requester_id: UUID) -> bytes:
        raise NotImplementedError("not implemented")

    async def health(self, manifest_id: UUID) -> VaultHealth:
        raise NotImplementedError("not implemented")

    async def delete(self, manifest_id: UUID, requester_id: UUID) -> bool:
        raise NotImplementedError("not implemented")

    async def list_manifests(self, owner_id: UUID, limit: int = 50, offset: int = 0) -> list[VaultManifest]:
        raise NotImplementedError("not implemented")

    async def replicate_shard(self, shard_id: UUID, target_node_id: str) -> VaultShard:
        raise NotImplementedError("not implemented")

    async def verify_shard(self, shard_id: UUID) -> bool:
        raise NotImplementedError("not implemented")

    async def get_manifest(self, manifest_id: UUID) -> VaultManifest | None:
        raise NotImplementedError("not implemented")

    async def get_shard(self, shard_id: UUID) -> VaultShard | None:
        raise NotImplementedError("not implemented")
