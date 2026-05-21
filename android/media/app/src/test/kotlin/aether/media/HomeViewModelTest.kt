package aether.media

import aether.media.util.MainDispatcherRule
import aether.media.viewmodel.HomeViewModel
import kotlinx.coroutines.test.advanceUntilIdle
import kotlinx.coroutines.test.runTest
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Rule
import org.junit.Test

class HomeViewModelTest {

    @get:Rule
    val mainDispatcherRule = MainDispatcherRule()

    // ── Initial state ─────────────────────────────────────────────────────────

    @Test
    fun `isRefreshing is false on construction`() {
        val vm = HomeViewModel()
        assertFalse(vm.isRefreshing.value)
    }

    @Test
    fun `feed is populated after init coroutine completes`() = runTest {
        val vm = HomeViewModel()
        advanceUntilIdle()
        assertTrue(vm.feed.value.isNotEmpty())
    }

    @Test
    fun `feed contains both live and non-live items`() = runTest {
        val vm = HomeViewModel()
        advanceUntilIdle()
        val feed = vm.feed.value
        assertTrue("expected at least one live item",    feed.any { it.isLive })
        assertTrue("expected at least one non-live item", feed.any { !it.isLive })
    }

    @Test
    fun `all feed items have non-blank ids`() = runTest {
        val vm = HomeViewModel()
        advanceUntilIdle()
        assertTrue(vm.feed.value.all { it.id.isNotBlank() })
    }

    @Test
    fun `all feed items have non-blank titles`() = runTest {
        val vm = HomeViewModel()
        advanceUntilIdle()
        assertTrue(vm.feed.value.all { it.title.isNotBlank() })
    }

    @Test
    fun `all feed items have non-blank streamUris`() = runTest {
        val vm = HomeViewModel()
        advanceUntilIdle()
        assertTrue(vm.feed.value.all { it.streamUri.isNotBlank() })
    }

    // ── refreshFeed ───────────────────────────────────────────────────────────

    @Test
    fun `refreshFeed keeps feed non-empty after completion`() = runTest {
        val vm = HomeViewModel()
        advanceUntilIdle()
        vm.refreshFeed()
        advanceUntilIdle()
        assertTrue(vm.feed.value.isNotEmpty())
    }

    @Test
    fun `isRefreshing is false after refreshFeed completes`() = runTest {
        val vm = HomeViewModel()
        advanceUntilIdle()
        vm.refreshFeed()
        advanceUntilIdle()
        assertFalse(vm.isRefreshing.value)
    }

    @Test
    fun `refreshFeed is idempotent — second call still populates feed`() = runTest {
        val vm = HomeViewModel()
        advanceUntilIdle()
        vm.refreshFeed()
        advanceUntilIdle()
        vm.refreshFeed()
        advanceUntilIdle()
        assertTrue(vm.feed.value.isNotEmpty())
    }

    // ── Feed item invariants ──────────────────────────────────────────────────

    @Test
    fun `live items have zero durationMs`() = runTest {
        val vm = HomeViewModel()
        advanceUntilIdle()
        val liveItems = vm.feed.value.filter { it.isLive }
        assertTrue(liveItems.all { it.durationMs == 0L })
    }

    @Test
    fun `non-live items have positive durationMs`() = runTest {
        val vm = HomeViewModel()
        advanceUntilIdle()
        val nonLiveItems = vm.feed.value.filter { !it.isLive }
        assertTrue(nonLiveItems.all { it.durationMs > 0L })
    }
}
