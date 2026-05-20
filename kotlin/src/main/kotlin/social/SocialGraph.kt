package aether.media.social

import java.util.concurrent.ConcurrentHashMap

/**
 * Thread-safe social graph tracking which UHIDs the local user is following.
 *
 * Uses [ConcurrentHashMap] as a set — all operations are O(1) and lock-free
 * on the read path.
 */
class SocialGraph {
    private val following: MutableSet<String> = ConcurrentHashMap.newKeySet()

    /** Add [uhid] to the following set.  No-op if already following. */
    fun follow(uhid: String) {
        require(uhid.isNotBlank()) { "uhid must not be blank" }
        following.add(uhid)
    }

    /** Remove [uhid] from the following set.  No-op if not following. */
    fun unfollow(uhid: String) {
        following.remove(uhid)
    }

    /** Returns true when [uhid] is in the following set. */
    fun isFollowing(uhid: String): Boolean = following.contains(uhid)

    /** Returns a sorted, immutable snapshot of followed UHIDs. */
    fun getFollowing(): List<String> = following.toSortedSet().toList()

    /** Number of followed accounts. */
    val count: Int get() = following.size
}
