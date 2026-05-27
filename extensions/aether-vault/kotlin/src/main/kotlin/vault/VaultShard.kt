package vault

import java.time.Instant
import java.util.UUID

data class VaultShard(
    val id: UUID = UUID.randomUUID(),
    val manifestId: UUID,
    val shardIndex: Int,
    val isParity: Boolean = false,
    val sizeBytes: Long,
    val checksum: String,
    val checksumAlgorithm: String = "sha256",
    val nodeId: String,
    val nodeAddress: String = "",
    val storageKey: String,
    val isAvailable: Boolean = true,
    val lastVerifiedAt: Instant? = null,
    val createdAt: Instant = Instant.now()
)
