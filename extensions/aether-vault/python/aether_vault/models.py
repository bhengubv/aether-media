from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime
from typing import Optional
from uuid import UUID, uuid4


@dataclass
class VaultManifest:
    owner_id: UUID
    name: str
    original_size_bytes: int
    encoded_size_bytes: int
    shard_count: int
    parity_shard_count: int
    min_shards_for_recovery: int
    checksum: str
    id: UUID = field(default_factory=uuid4)
    description: str = ""
    checksum_algorithm: str = "sha256"
    encryption_algorithm: str = "AES-256-GCM"
    encrypted_key_hint: str = ""
    content_type: str = "application/octet-stream"
    tags: list[str] = field(default_factory=list)
    shard_ids: list[UUID] = field(default_factory=list)
    replication_factor: int = 3
    created_at: datetime = field(default_factory=datetime.utcnow)
    updated_at: datetime = field(default_factory=datetime.utcnow)
    expires_at: Optional[datetime] = None
    metadata: dict[str, str] = field(default_factory=dict)


@dataclass
class VaultShard:
    manifest_id: UUID
    shard_index: int
    size_bytes: int
    checksum: str
    node_id: str
    storage_key: str
    id: UUID = field(default_factory=uuid4)
    is_parity: bool = False
    checksum_algorithm: str = "sha256"
    node_address: str = ""
    is_available: bool = True
    last_verified_at: Optional[datetime] = None
    created_at: datetime = field(default_factory=datetime.utcnow)


@dataclass
class VaultHealth:
    manifest_id: UUID
    total_shards: int
    available_shards: int
    parity_shards: int
    available_parity_shards: int
    min_shards_for_recovery: int
    replication_factor: int
    degraded_nodes: list[str] = field(default_factory=list)
    last_checked_at: datetime = field(default_factory=datetime.utcnow)

    @property
    def is_recoverable(self) -> bool:
        return self.available_shards >= self.min_shards_for_recovery

    @property
    def is_healthy(self) -> bool:
        return (
            self.available_shards == self.total_shards
            and self.available_parity_shards == self.parity_shards
        )

    @property
    def is_degraded(self) -> bool:
        return self.is_recoverable and not self.is_healthy

    @property
    def health_percent(self) -> float:
        if self.total_shards == 0:
            return 0.0
        return (self.available_shards / self.total_shards) * 100.0
