package aethermesh.media.feed

import aethermesh.media.models.MediaFeedItem
import java.util.concurrent.locks.ReentrantReadWriteLock
import kotlin.concurrent.read
import kotlin.concurrent.write

private const val FEED_CAP = 500

/**
 * Thread-safe in-memory feed aggregator capped at [FEED_CAP] items.
 *
 * Items are stored newest-first.  When capacity is reached the oldest
 * item (last in the list) is evicted.
 */
class FeedAggregator {
    private val items = ArrayDeque<MediaFeedItem>(FEED_CAP)
    private val lock  = ReentrantReadWriteLock()

    /**
     * Prepend [item] to the feed.  Evicts the oldest item when at capacity.
     */
    fun addItem(item: MediaFeedItem) {
        lock.write {
            if (items.size >= FEED_CAP) {
                items.removeLast()
            }
            items.addFirst(item)
        }
    }

    /**
     * Returns an immutable page of at most [limit] items starting at [offset].
     * Returns an empty list when [offset] >= total items or [limit] <= 0.
     */
    fun getFeed(limit: Int, offset: Int): List<MediaFeedItem> {
        if (limit <= 0) return emptyList()
        return lock.read {
            if (offset >= items.size) return@read emptyList()
            val end = minOf(offset + limit, items.size)
            items.toList().subList(offset, end)
        }
    }

    /**
     * Record that the local user watched [ms] milliseconds of the content
     * identified by [contentHash].  Accumulates into the existing watchedMs.
     */
    fun markWatched(contentHash: String, ms: Long) {
        require(ms >= 0L) { "ms must be >= 0" }
        lock.write {
            val idx = items.indexOfFirst { it.content.contentHash == contentHash }
            if (idx >= 0) {
                val old = items[idx]
                items[idx] = old.copy(
                    watchedMs  = old.watchedMs + ms,
                    watchCount = old.watchCount + 1,
                )
            }
        }
    }

    /** Number of items currently in the feed. */
    val size: Int get() = lock.read { items.size }
}
