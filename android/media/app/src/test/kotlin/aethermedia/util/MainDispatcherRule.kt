package aethermedia.util

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.test.StandardTestDispatcher
import kotlinx.coroutines.test.TestDispatcher
import kotlinx.coroutines.test.resetMain
import kotlinx.coroutines.test.setMain
import org.junit.rules.TestWatcher
import org.junit.runner.Description

/**
 * A JUnit rule that replaces [Dispatchers.Main] with a [TestDispatcher] for the duration
 * of each test, so that [androidx.lifecycle.viewModelScope] coroutines are dispatched
 * synchronously and can be advanced via [kotlinx.coroutines.test.runTest] /
 * [kotlinx.coroutines.test.advanceUntilIdle].
 */
class MainDispatcherRule(
    val testDispatcher: TestDispatcher = StandardTestDispatcher(),
) : TestWatcher() {
    override fun starting(description: Description) {
        Dispatchers.setMain(testDispatcher)
    }

    override fun finished(description: Description) {
        Dispatchers.resetMain()
    }
}
