import aethernet.media.social.SocialGraph
import org.junit.jupiter.api.Assertions.*
import org.junit.jupiter.api.Test

class SocialGraphTest {

    @Test fun `follow adds uhid to the set`() {
        val g = SocialGraph()
        g.follow("alice")
        assertTrue(g.isFollowing("alice"))
    }

    @Test fun `follow multiple accounts`() {
        val g = SocialGraph()
        g.follow("alice")
        g.follow("bob")
        assertEquals(2, g.count)
        assertTrue(g.isFollowing("bob"))
    }

    @Test fun `double follow is idempotent`() {
        val g = SocialGraph()
        g.follow("alice")
        g.follow("alice")
        assertEquals(1, g.count)
    }

    @Test fun `unfollow removes uhid`() {
        val g = SocialGraph()
        g.follow("alice")
        g.unfollow("alice")
        assertFalse(g.isFollowing("alice"))
        assertEquals(0, g.count)
    }

    @Test fun `unfollow non-following is noop`() {
        val g = SocialGraph()
        assertDoesNotThrow { g.unfollow("ghost") }
        assertEquals(0, g.count)
    }

    @Test fun `getFollowing returns sorted list`() {
        val g = SocialGraph()
        g.follow("charlie")
        g.follow("alice")
        g.follow("bob")
        assertEquals(listOf("alice", "bob", "charlie"), g.getFollowing())
    }

    @Test fun `isFollowing returns false for unknown uhid`() {
        val g = SocialGraph()
        assertFalse(g.isFollowing("unknown"))
    }

    @Test fun `follow blank uhid throws`() {
        val g = SocialGraph()
        assertThrows(IllegalArgumentException::class.java) {
            g.follow("   ")
        }
    }
}
