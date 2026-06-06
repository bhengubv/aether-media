package aethermedia

import aethermedia.viewmodel.FeedItemUi
import org.junit.Assert.assertEquals
import org.junit.Test

class FeedItemUiTest {

    // ── Helper ────────────────────────────────────────────────────────────────

    private fun item(durationMs: Long, isLive: Boolean = false) = FeedItemUi(
        id          = "test-id",
        title       = "Test Title",
        creatorTag  = "@test",
        durationMs  = durationMs,
        reactionCount = 0,
        viewCount   = 0,
        isLive      = isLive,
        codecBadge  = "HLS",
        streamUri   = ""
    )

    // ── Live items ────────────────────────────────────────────────────────────

    @Test fun `formattedDuration is LIVE when isLive is true`() {
        assertEquals("LIVE", item(0L, isLive = true).formattedDuration)
    }

    @Test fun `formattedDuration is LIVE regardless of durationMs when isLive`() {
        assertEquals("LIVE", item(3_600_000L, isLive = true).formattedDuration)
    }

    // ── Sub-minute durations ──────────────────────────────────────────────────

    @Test fun `zero duration formats as 0 colon 00`() {
        assertEquals("0:00", item(0L).formattedDuration)
    }

    @Test fun `30 seconds formats as 0 colon 30`() {
        assertEquals("0:30", item(30_000L).formattedDuration)
    }

    @Test fun `59 seconds formats as 0 colon 59`() {
        assertEquals("0:59", item(59_000L).formattedDuration)
    }

    @Test fun `seconds are zero-padded below 10`() {
        assertEquals("1:05", item(65_000L).formattedDuration)
    }

    // ── Multi-minute, sub-hour durations ──────────────────────────────────────

    @Test fun `90 seconds formats as 1 colon 30`() {
        assertEquals("1:30", item(90_000L).formattedDuration)
    }

    @Test fun `59 minutes 59 seconds formats without hours`() {
        val ms = (59 * 60 + 59) * 1_000L
        assertEquals("59:59", item(ms).formattedDuration)
    }

    // ── Hour-spanning durations ───────────────────────────────────────────────

    @Test fun `exactly one hour formats as 1 colon 00 colon 00`() {
        assertEquals("1:00:00", item(3_600_000L).formattedDuration)
    }

    @Test fun `1h 2m 3s formats as 1 colon 02 colon 03`() {
        val ms = (1 * 3_600 + 2 * 60 + 3) * 1_000L
        assertEquals("1:02:03", item(ms).formattedDuration)
    }

    @Test fun `minutes are zero-padded below 10 in h-mm-ss format`() {
        val ms = (2 * 3_600 + 5 * 60 + 7) * 1_000L
        assertEquals("2:05:07", item(ms).formattedDuration)
    }

    @Test fun `hours above 9 render without padding`() {
        val ms = (10 * 3_600 + 1 * 60 + 1) * 1_000L
        assertEquals("10:01:01", item(ms).formattedDuration)
    }
}
