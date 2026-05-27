package forge

import java.time.Instant
import java.util.UUID

data class ForgeEntry(
    val id: UUID = UUID.randomUUID(),
    val packageId: String,
    val ecosystem: String,
    val version: String,
    val name: String,
    val description: String = "",
    val author: String = "",
    val licenseId: String = "",
    val sizeBytes: Long = 0L,
    val checksum: String,
    val checksumAlgorithm: String = "sha256",
    val downloadUrl: String,
    val mirrorUrls: List<String> = emptyList(),
    val dependencies: List<String> = emptyList(),
    val tags: List<String> = emptyList(),
    val isVerified: Boolean = false,
    val downloadCount: Long = 0L,
    val cachedAt: Instant = Instant.now(),
    val expiresAt: Instant? = null,
    val metadata: Map<String, String> = emptyMap()
)
