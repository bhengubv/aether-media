package aether.media.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class FeedItemUi(
    val id: String,
    val title: String,
    val creatorTag: String,
    val durationMs: Long,
    val reactionCount: Int,
    val viewCount: Int,
    val isLive: Boolean,
    val codecBadge: String,
    val streamUri: String
) {
    val formattedDuration: String
        get() {
            if (isLive) return "LIVE"
            val totalSeconds = durationMs / 1000
            val hours = totalSeconds / 3600
            val minutes = (totalSeconds % 3600) / 60
            val seconds = totalSeconds % 60
            return if (hours > 0) {
                String.format("%d:%02d:%02d", hours, minutes, seconds)
            } else {
                String.format("%d:%02d", minutes, seconds)
            }
        }
}

class HomeViewModel : ViewModel() {

    private val _feed = MutableStateFlow<List<FeedItemUi>>(emptyList())
    val feed: StateFlow<List<FeedItemUi>> = _feed.asStateFlow()

    private val _isRefreshing = MutableStateFlow(false)
    val isRefreshing: StateFlow<Boolean> = _isRefreshing.asStateFlow()

    init {
        loadMockFeed()
    }

    fun refreshFeed() {
        viewModelScope.launch {
            _isRefreshing.value = true
            // In production this would call the Aether mesh feed service.
            // For now reload the same mock data to demonstrate the flow.
            _feed.value = buildMockFeed()
            _isRefreshing.value = false
        }
    }

    private fun loadMockFeed() {
        viewModelScope.launch {
            _feed.value = buildMockFeed()
        }
    }

    private fun buildMockFeed(): List<FeedItemUi> = listOf(
        FeedItemUi(
            id = "feed-001",
            title = "African Rhythms & the Future of Sound",
            creatorTag = "@djkhumalo",
            durationMs = 3_660_000L,
            reactionCount = 1_240,
            viewCount = 8_302,
            isLive = false,
            codecBadge = "HLS",
            streamUri = "https://demo.unified-streaming.com/k8s/features/stable/video/tears-of-steel/tears-of-steel.ism/.m3u8"
        ),
        FeedItemUi(
            id = "feed-002",
            title = "Live: Cape Town City Session",
            creatorTag = "@sunsetgroove",
            durationMs = 0L,
            reactionCount = 432,
            viewCount = 1_109,
            isLive = true,
            codecBadge = "HLS",
            streamUri = "https://demo.unified-streaming.com/k8s/features/stable/video/tears-of-steel/tears-of-steel.ism/.m3u8"
        ),
        FeedItemUi(
            id = "feed-003",
            title = "Mesh Network Explained — No Internet Required",
            creatorTag = "@aethertech",
            durationMs = 1_920_000L,
            reactionCount = 3_871,
            viewCount = 24_500,
            isLive = false,
            codecBadge = "MP4",
            streamUri = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"
        ),
        FeedItemUi(
            id = "feed-004",
            title = "Street Food Tour: Johannesburg Markets",
            creatorTag = "@tastejozi",
            durationMs = 2_580_000L,
            reactionCount = 987,
            viewCount = 5_640,
            isLive = false,
            codecBadge = "MP4",
            streamUri = "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4"
        ),
        FeedItemUi(
            id = "feed-005",
            title = "Live: Underground Durban Jazz — Set 3",
            creatorTag = "@durbanflow",
            durationMs = 0L,
            reactionCount = 211,
            viewCount = 560,
            isLive = true,
            codecBadge = "HLS",
            streamUri = "https://demo.unified-streaming.com/k8s/features/stable/video/tears-of-steel/tears-of-steel.ism/.m3u8"
        )
    )
}
