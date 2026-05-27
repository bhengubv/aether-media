package space

import java.time.Instant
import java.util.UUID

enum class BreadcrumbType {
    POST,
    EVENT,
    ALERT,
    OFFER,
    NOTICE,
    PINNED
}

data class SpaceBreadcrumb(
    val id: UUID = UUID.randomUUID(),
    val spaceId: UUID,
    val authorId: UUID,
    val geoHash: String,
    val type: BreadcrumbType,
    val title: String,
    val body: String,
    val mediaUrls: List<String> = emptyList(),
    val tags: List<String> = emptyList(),
    val expiresAt: Instant?,
    val isPinned: Boolean = false,
    val reactionCount: Int = 0,
    val replyCount: Int = 0,
    val createdAt: Instant = Instant.now(),
    val updatedAt: Instant = Instant.now()
)
