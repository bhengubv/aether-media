package aether.media

import aether.media.util.MainDispatcherRule
import aether.media.viewmodel.PlayerState
import aether.media.viewmodel.PlayerViewModel
import kotlinx.coroutines.test.advanceUntilIdle
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertNull
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test

class PlayerViewModelTest {

    @get:Rule
    val mainDispatcherRule = MainDispatcherRule()

    // ── Initial state ─────────────────────────────────────────────────────────

    @Test
    fun `initial playerState is Idle`() {
        val vm = PlayerViewModel()
        assertEquals(PlayerState.Idle, vm.playerState.value)
    }

    @Test
    fun `initial currentPositionMs is 0`() {
        val vm = PlayerViewModel()
        assertEquals(0L, vm.currentPositionMs.value)
    }

    @Test
    fun `initial durationMs is 0`() {
        val vm = PlayerViewModel()
        assertEquals(0L, vm.durationMs.value)
    }

    @Test
    fun `initial isWatchPartyActive is false`() {
        val vm = PlayerViewModel()
        assertFalse(vm.isWatchPartyActive.value)
    }

    @Test
    fun `initial reactionOverlay is null`() {
        val vm = PlayerViewModel()
        assertNull(vm.reactionOverlay.value)
    }

    @Test
    fun `initial exoPlayer is null`() {
        val vm = PlayerViewModel()
        assertNull(vm.exoPlayer)
    }

    // ── seekTo ────────────────────────────────────────────────────────────────

    @Test
    fun `seekTo updates currentPositionMs when no player is attached`() {
        val vm = PlayerViewModel()
        vm.seekTo(30_000L)
        assertEquals(30_000L, vm.currentPositionMs.value)
    }

    @Test
    fun `seekTo accepts zero`() {
        val vm = PlayerViewModel()
        vm.seekTo(0L)
        assertEquals(0L, vm.currentPositionMs.value)
    }

    @Test
    fun `seekTo overwrites previous position`() {
        val vm = PlayerViewModel()
        vm.seekTo(90_000L)
        vm.seekTo(15_000L)
        assertEquals(15_000L, vm.currentPositionMs.value)
    }

    @Test
    fun `seekTo to 0 after non-zero resets position`() {
        val vm = PlayerViewModel()
        vm.seekTo(120_000L)
        vm.seekTo(0L)
        assertEquals(0L, vm.currentPositionMs.value)
    }

    // ── sendReaction ──────────────────────────────────────────────────────────

    @Test
    fun `sendReaction sets reactionOverlay`() = runTest {
        val vm = PlayerViewModel()
        vm.sendReaction("❤️")
        advanceUntilIdle()
        assertEquals("❤️", vm.reactionOverlay.value)
    }

    @Test
    fun `sendReaction with fire emoji sets reactionOverlay`() = runTest {
        val vm = PlayerViewModel()
        vm.sendReaction("🔥")
        advanceUntilIdle()
        assertEquals("🔥", vm.reactionOverlay.value)
    }

    @Test
    fun `sendReaction overwrites previous overlay`() = runTest {
        val vm = PlayerViewModel()
        vm.sendReaction("❤️")
        advanceUntilIdle()
        vm.sendReaction("👏")
        advanceUntilIdle()
        assertEquals("👏", vm.reactionOverlay.value)
    }

    // ── clearReactionOverlay ──────────────────────────────────────────────────

    @Test
    fun `clearReactionOverlay sets reactionOverlay to null`() = runTest {
        val vm = PlayerViewModel()
        vm.sendReaction("😂")
        advanceUntilIdle()
        vm.clearReactionOverlay()
        assertNull(vm.reactionOverlay.value)
    }

    @Test
    fun `clearReactionOverlay is a no-op when already null`() {
        val vm = PlayerViewModel()
        vm.clearReactionOverlay() // should not throw
        assertNull(vm.reactionOverlay.value)
    }

    // ── setWatchPartyActive ───────────────────────────────────────────────────

    @Test
    fun `setWatchPartyActive true sets isWatchPartyActive to true`() {
        val vm = PlayerViewModel()
        vm.setWatchPartyActive(true)
        assertTrue(vm.isWatchPartyActive.value)
    }

    @Test
    fun `setWatchPartyActive false after true clears isWatchPartyActive`() {
        val vm = PlayerViewModel()
        vm.setWatchPartyActive(true)
        vm.setWatchPartyActive(false)
        assertFalse(vm.isWatchPartyActive.value)
    }

    @Test
    fun `setWatchPartyActive false is idempotent when already false`() {
        val vm = PlayerViewModel()
        vm.setWatchPartyActive(false)
        assertFalse(vm.isWatchPartyActive.value)
    }

    // ── cleanUp ───────────────────────────────────────────────────────────────

    @Test
    fun `cleanUp resets playerState to Idle`() {
        val vm = PlayerViewModel()
        vm.cleanUp()
        assertEquals(PlayerState.Idle, vm.playerState.value)
    }

    @Test
    fun `cleanUp nullifies exoPlayer`() {
        val vm = PlayerViewModel()
        vm.cleanUp()
        assertNull(vm.exoPlayer)
    }

    @Test
    fun `cleanUp is idempotent — second call does not throw`() {
        val vm = PlayerViewModel()
        vm.cleanUp()
        vm.cleanUp() // should not throw
        assertEquals(PlayerState.Idle, vm.playerState.value)
    }
}
