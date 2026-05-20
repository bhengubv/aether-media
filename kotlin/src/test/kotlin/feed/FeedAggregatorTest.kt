package feed

import aether.media.feed.FeedAggregator
import aether.media.models.MediaContent
import aether.media.models.MediaFeedItem
import org.junit.jupiter.api.Assertions.*
import org.junit.jupiter.api.Test
import org.junit.jupiter.api.assertThrows
import java.util.concurrent.CountDownLatch
import java.util.concurrent.Executors
import java.util.concurrent.TimeUnit

class FeedAggregatorTest {

    // ── helpers ────────────────────────────────────────────────────────────────

    private fun content(hash: String = "h1") = MediaContent(
        contentHash  = hash,
        title        = "Title $hash",
        durationMs   = 60_000L,
        codec        = "h264",
        contentType  = "video/mp4",
        creatorUhid  = "creator",
        sizeBytes    = 500_000L,
    )

    private fun item(hash: String = "h1", publishedAtMs: Long = System.currentTimeMillis()) =
        MediaFeedItem(
            content      = content(hash),
            likeCount    = 0,
            shareCount   = 0,
            commentCount = 0,
            watchCount   = 0,
            isLive       = false,
            streamId     = null,
            publishedAtMs = publishedAtMs,
        )

    // ── size ──────────────────────────────────────────────────────────────────

    @Test fun `size starts at zero`() {
        assertEquals(0, FeedAggregator().size)
    }

    // ── addItem ───────────────────────────────────────────────────────────────

    @Test fun `addItem increases size`() {
        val agg = FeedAggregator()
        agg.addItem(item("h1"))
        assertEquals(1, agg.size)
    }

    @Test fun `items stored newest-first`() {
        val agg = FeedAggregator()
        agg.addItem(item("first"))
        agg.addItem(item("second"))
        val feed = agg.getFeed(limit = 2, offset = 0)
        assertEquals("second", feed[0].content.contentHash)
        assertEquals("first",  feed[1].content.contentHash)
    }

    @Test fun `add multiple items increments size`() {
        val agg = FeedAggregator()
        repeat(5) { agg.addItem(item("h$it")) }
        assertEquals(5, agg.size)
    }

    // ── getFeed ───────────────────────────────────────────────────────────────

    @Test fun `getFeed on empty returns empty list`() {
        assertTrue(FeedAggregator().getFeed(limit = 10, offset = 0).isEmpty())
    }

    @Test fun `getFeed limit zero returns empty`() {
        val agg = FeedAggregator()
        agg.addItem(item("h1"))
        assertTrue(agg.getFeed(limit = 0, offset = 0).isEmpty())
    }

    @Test fun `getFeed negative limit returns empty`() {
        val agg = FeedAggregator()
        agg.addItem(item("h1"))
        assertTrue(agg.getFeed(limit = -1, offset = 0).isEmpty())
    }

    @Test fun `getFeed offset beyond size returns empty`() {
        val agg = FeedAggregator()
        agg.addItem(item("h1"))
        assertTrue(agg.getFeed(limit = 10, offset = 5).isEmpty())
    }

    @Test fun `getFeed offset exactly at size returns empty`() {
        val agg = FeedAggregator()
        agg.addItem(item("h1"))
        assertTrue(agg.getFeed(limit = 10, offset = 1).isEmpty())
    }

    @Test fun `getFeed pagination page 1`() {
        val agg = FeedAggregator()
        repeat(5) { agg.addItem(item("h$it")) }
        val page = agg.getFeed(limit = 2, offset = 0)
        assertEquals(2, page.size)
    }

    @Test fun `getFeed pagination page 2`() {
        val agg = FeedAggregator()
        repeat(5) { agg.addItem(item("h$it")) }
        val page = agg.getFeed(limit = 2, offset = 2)
        assertEquals(2, page.size)
    }

    @Test fun `getFeed last page returns partial slice`() {
        val agg = FeedAggregator()
        repeat(5) { agg.addItem(item("h$it")) }
        val page = agg.getFeed(limit = 10, offset = 4)
        assertEquals(1, page.size)
    }

    @Test fun `getFeed limit larger than size returns all items`() {
        val agg = FeedAggregator()
        repeat(3) { agg.addItem(item("h$it")) }
        assertEquals(3, agg.getFeed(limit = 100, offset = 0).size)
    }

    @Test fun `getFeed returns immutable snapshot`() {
        val agg = FeedAggregator()
        agg.addItem(item("h1"))
        val snap = agg.getFeed(limit = 10, offset = 0)
        // Adding another item after snapshotting must not change snap
        agg.addItem(item("h2"))
        assertEquals(1, snap.size)
    }

    // ── markWatched ───────────────────────────────────────────────────────────

    @Test fun `markWatched accumulates ms`() {
        val agg = FeedAggregator()
        agg.addItem(item("vid1"))
        agg.markWatched("vid1", 1_000L)
        agg.markWatched("vid1", 2_000L)
        assertEquals(3_000L, agg.getFeed(1, 0)[0].watchedMs)
    }

    @Test fun `markWatched increments watchCount each call`() {
        val agg = FeedAggregator()
        agg.addItem(item("vid1"))
        agg.markWatched("vid1", 500L)
        agg.markWatched("vid1", 500L)
        assertEquals(2, agg.getFeed(1, 0)[0].watchCount)
    }

    @Test fun `markWatched unknown hash is noop`() {
        val agg = FeedAggregator()
        agg.addItem(item("vid1"))
        agg.markWatched("nope", 1_000L)
        assertEquals(0L, agg.getFeed(1, 0)[0].watchedMs)
        assertEquals(0,  agg.getFeed(1, 0)[0].watchCount)
    }

    @Test fun `markWatched zero ms still increments watchCount`() {
        val agg = FeedAggregator()
        agg.addItem(item("vid1"))
        agg.markWatched("vid1", 0L)
        val it = agg.getFeed(1, 0)[0]
        assertEquals(0L, it.watchedMs)
        assertEquals(1,  it.watchCount)
    }

    @Test fun `markWatched negative ms throws`() {
        val agg = FeedAggregator()
        agg.addItem(item("vid1"))
        assertThrows<IllegalArgumentException> {
            agg.markWatched("vid1", -1L)
        }
    }

    @Test fun `markWatched on empty feed is noop`() {
        val agg = FeedAggregator()
        // Must not throw
        agg.markWatched("anything", 1_000L)
        assertEquals(0, agg.size)
    }

    // ── capacity eviction ─────────────────────────────────────────────────────

    @Test fun `capacity stays at 500 after overflow`() {
        val agg = FeedAggregator()
        repeat(505) { agg.addItem(item("h$it")) }
        assertEquals(500, agg.size)
    }

    @Test fun `eviction removes oldest item`() {
        val agg = FeedAggregator()
        // Add 500 items
        repeat(500) { agg.addItem(item("h$it")) }
        // One more: oldest ("h0", added first) should be evicted
        agg.addItem(item("new"))
        assertEquals(500, agg.size)
        val all = agg.getFeed(limit = 500, offset = 0)
        // newest is at front
        assertEquals("new", all[0].content.contentHash)
        // "h0" was the oldest and must be gone
        assertFalse(all.any { it.content.contentHash == "h0" })
    }

    @Test fun `at-capacity add keeps newest item at front`() {
        val agg = FeedAggregator()
        repeat(500) { agg.addItem(item("h$it")) }
        agg.addItem(item("newest"))
        assertEquals("newest", agg.getFeed(1, 0)[0].content.contentHash)
    }

    // ── thread safety ─────────────────────────────────────────────────────────

    @Test fun `concurrent addItem is thread-safe`() {
        val agg      = FeedAggregator()
        val threads  = 20
        val perThread = 10
        val latch    = CountDownLatch(threads)
        val pool     = Executors.newFixedThreadPool(threads)

        repeat(threads) { t ->
            pool.submit {
                repeat(perThread) { i ->
                    agg.addItem(item("t${t}_$i"))
                }
                latch.countDown()
            }
        }
        assertTrue(latch.await(10, TimeUnit.SECONDS))
        pool.shutdown()
        assertEquals(threads * perThread, agg.size)
    }

    @Test fun `concurrent markWatched is thread-safe`() {
        val agg = FeedAggregator()
        agg.addItem(item("shared"))
        val threads = 20
        val latch   = CountDownLatch(threads)
        val pool    = Executors.newFixedThreadPool(threads)

        repeat(threads) {
            pool.submit {
                agg.markWatched("shared", 100L)
                latch.countDown()
            }
        }
        assertTrue(latch.await(10, TimeUnit.SECONDS))
        pool.shutdown()
        val it = agg.getFeed(1, 0)[0]
        assertEquals(threads,         it.watchCount)
        assertEquals(threads * 100L,  it.watchedMs)
    }
}
