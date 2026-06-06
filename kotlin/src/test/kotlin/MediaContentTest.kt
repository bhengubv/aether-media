import aethernet.media.models.MediaContent
import org.junit.jupiter.api.Assertions.*
import org.junit.jupiter.api.Test

class MediaContentTest {

    private fun content(durationMs: Long, contentType: String = "video/mp4") = MediaContent(
        contentHash  = "abc123",
        title        = "Test",
        durationMs   = durationMs,
        codec        = "h264",
        contentType  = contentType,
        creatorUhid  = "u1",
        sizeBytes    = 1_000_000L,
    )

    @Test fun `formattedDuration returns Live for zero`() {
        assertEquals("Live", content(0L).formattedDuration)
    }

    @Test fun `formattedDuration returns Live for negative`() {
        assertEquals("Live", content(-1L).formattedDuration)
    }

    @Test fun `formattedDuration sub-hour no padding on minutes`() {
        // 272 seconds = 4:32
        assertEquals("4:32", content(272_000L).formattedDuration)
    }

    @Test fun `formattedDuration sub-hour pads seconds`() {
        // 65 seconds = 1:05
        assertEquals("1:05", content(65_000L).formattedDuration)
    }

    @Test fun `formattedDuration over hour`() {
        // 5025 seconds = 1:23:45
        assertEquals("1:23:45", content(5_025_000L).formattedDuration)
    }

    @Test fun `formattedDuration exactly one hour`() {
        assertEquals("1:00:00", content(3_600_000L).formattedDuration)
    }

    @Test fun `isVideo true for video content type`() {
        assertTrue(content(1000L, "video/mp4").isVideo)
    }

    @Test fun `isVideo false for audio content type`() {
        assertFalse(content(1000L, "audio/aac").isVideo)
    }

    @Test fun `isAudio true for audio content type`() {
        assertTrue(content(1000L, "audio/flac").isAudio)
    }

    @Test fun `isAudio false for video content type`() {
        assertFalse(content(1000L, "video/webm").isAudio)
    }

    @Test fun `isVideo case insensitive`() {
        assertTrue(content(1000L, "VIDEO/MP4").isVideo)
    }
}
