package vault

import java.time.Instant
import java.util.UUID

data class VaultManifest(
    val id: UUID = UUID.randomUUID(),
    val ownerId: UUID,
    val name: String,
    val description: String = "",
    val originalSizeBytes: Long,
    val encodedSizeBytes: Long,
    val shardCount: Int,
    val parityShardCount: Int,
    val minShardsForRecovery: Int,
    val checksum: String,
    val checksumAlgorithm: String = "sha256",
    val encryptionAlgorithm: String = "AES-256-GCM",
    val encryptedKeyHint: String = "",
    val contentType: String = "application/octet-stream",
    val tags: List<String> = emptyList(),
    val shardIds: List<UUID> = emptyList(),
    val replicationFactor: Int = 3,
    val createdAt: Instant = Instant.now(),
    val updatedAt: Instant = Instant.now(),
    val expiresAt: Instant? = null,
    val metadata: Map<String, String> = emptyMap()
)
