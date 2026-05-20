package aether.media.models

import kotlinx.serialization.Serializable

/**
 * Immutable description of a single piece of media stored on the Aether network.
 * Primary key is [contentHash] — SHA-256 hex of the raw encoded bytes.
 */
@Serializable
data class MediaContent(
    val contentHash: String,
    val title: String,
    val durationMs: Long,
    val codec: String,
    val contentType: String,
    val creatorUhid: String,
    val sizeBytes: Long,
    val tags: List<String> = emptyList(),
    val thumbnailHash: String? = null,
) {
    /**
     * Human-readable duration:
     * - 0 ms → "Live"
     * - < 1 hour → "M:SS"
     * - >= 1 hour → "H:MM:SS"
     */
    val formattedDuration: String
        get() = when {
            durationMs <= 0L -> "Live"
            durationMs < 3_600_000L -> {
                val totalSec = durationMs / 1000L
                val minutes = totalSec / 60L
                val seconds = totalSec % 60L
                "$minutes:${seconds.toString().padStart(2, '0')}"
            }
            else -> {
                val totalSec = durationMs / 1000L
                val hours   = totalSec / 3600L
                val minutes = (totalSec % 3600L) / 60L
                val seconds = totalSec % 60L
                "$hours:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}"
            }
        }

    val isVideo: Boolean get() = contentType.lowercase().startsWith("video/")
    val isAudio: Boolean get() = contentType.lowercase().startsWith("audio/")
}

// ── MediaReactionType ──────────────────────────────────────────────────────────

enum class MediaReactionType(val value: Int) {
    LIKE(1),
    SHARE(2),
    COMMENT(3),
    SUPER_REACT(4);

    companion object {
        fun fromValue(v: Int): MediaReactionType =
            entries.firstOrNull { it.value == v }
                ?: throw IllegalArgumentException("Unknown MediaReactionType value: $v")
    }
}

// ── MediaReaction ──────────────────────────────────────────────────────────────

/**
 * A timestamped reaction sent by a viewer.
 *
 * Validation rules:
 * - [message] is required (non-blank) for [MediaReactionType.COMMENT].
 * - [message] must be null for all other types.
 */
@Serializable
data class MediaReaction(
    val reactionId: String,
    val contentHash: String,
    val fromUhid: String,
    val type: MediaReactionType,
    val positionMs: Long,
    val message: String?,
    val sentAtMs: Long,
) {
    init {
        require(contentHash.isNotBlank()) { "contentHash must not be empty" }
        require(fromUhid.isNotBlank())    { "fromUhid must not be empty" }
        require(positionMs >= 0L)         { "positionMs must be >= 0" }
        if (type == MediaReactionType.COMMENT) {
            require(!message.isNullOrBlank()) {
                "A message is required for COMMENT reactions"
            }
        } else {
            require(message == null) {
                "message must be null for ${type.name} reactions"
            }
        }
    }
}

// ── MediaProfile ───────────────────────────────────────────────────────────────

@Serializable
data class MediaProfile(
    val uhid: String,
    val displayName: String,
    val avatarHash: String?,
    val bio: String?,
    val aetherTagValue: String,
    val followerCount: Int,
    val followingCount: Int,
    val contentCount: Int,
    val isVerified: Boolean,
    val joinedAtMs: Long,
) {
    private companion object { const val SHORT_BIO_MAX = 120 }

    /**
     * Bio trimmed to 120 characters at the last word boundary, with "…" appended.
     * Returns "" when [bio] is null or blank.
     */
    val shortBio: String
        get() {
            val trimmed = bio?.trim() ?: return ""
            if (trimmed.isEmpty()) return ""
            if (trimmed.length <= SHORT_BIO_MAX) return trimmed
            val cut = trimmed.substring(0, SHORT_BIO_MAX)
            val lastSpace = cut.lastIndexOf(' ')
            val boundary = if (lastSpace > 0) lastSpace else SHORT_BIO_MAX
            return cut.substring(0, boundary).trimEnd() + "…"
        }
}

// ── LiveStream ─────────────────────────────────────────────────────────────────

@Serializable
data class LiveStream(
    val streamId: String,
    val title: String,
    val creatorUhid: String,
    val codec: String,
    val segmentDurationMs: Int,
    val startedAtMs: Long,
    val viewerCount: Int,
    val isActive: Boolean,
    val tags: List<String> = emptyList(),
) {
    /** Wall-clock milliseconds since the broadcast started.  Always >= 0. */
    val elapsedMs: Long
        get() = maxOf(0L, System.currentTimeMillis() - startedAtMs)

    val elapsedFormatted: String
        get() {
            val totalSec = elapsedMs / 1000L
            val hours   = totalSec / 3600L
            val minutes = (totalSec % 3600L) / 60L
            val seconds = totalSec % 60L
            return if (hours > 0L) {
                "$hours:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}"
            } else {
                "$minutes:${seconds.toString().padStart(2, '0')}"
            }
        }
}

// ── MediaFeedItem ──────────────────────────────────────────────────────────────

@Serializable
data class MediaFeedItem(
    val content: MediaContent,
    val likeCount: Int,
    val shareCount: Int,
    val commentCount: Int,
    val watchCount: Int,
    val isLive: Boolean,
    val streamId: String?,
    val topReactions: List<MediaReaction> = emptyList(),
    val publishedAtMs: Long,
    val watchedMs: Long = 0L,
) {
    val isNew: Boolean
        get() = (System.currentTimeMillis() - publishedAtMs) < 86_400_000L

    val reactionTotal: Int
        get() = likeCount + shareCount + commentCount
}
