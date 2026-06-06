package aethermesh.media.ui

import android.view.ViewGroup
import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.aspectRatio
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Pause
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.VolumeUp
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Slider
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableFloatStateOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.viewinterop.AndroidView
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.media3.ui.PlayerView
import aethermesh.media.viewmodel.PlayerState
import aethermesh.media.viewmodel.PlayerViewModel
import kotlinx.coroutines.delay

// Maps known feed/nearby IDs to stream URIs.
// In production this would come from the Aether content catalogue service.
private val STREAM_CATALOGUE = mapOf(
    "feed-001" to "https://demo.unified-streaming.com/k8s/features/stable/video/tears-of-steel/tears-of-steel.ism/.m3u8",
    "feed-002" to "https://demo.unified-streaming.com/k8s/features/stable/video/tears-of-steel/tears-of-steel.ism/.m3u8",
    "feed-003" to "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
    "feed-004" to "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4",
    "feed-005" to "https://demo.unified-streaming.com/k8s/features/stable/video/tears-of-steel/tears-of-steel.ism/.m3u8",
    "nearby-001" to "https://demo.unified-streaming.com/k8s/features/stable/video/tears-of-steel/tears-of-steel.ism/.m3u8",
    "nearby-002" to "https://demo.unified-streaming.com/k8s/features/stable/video/tears-of-steel/tears-of-steel.ism/.m3u8",
    "nearby-003" to "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
    "library-001" to "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4",
    "library-002" to "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4",
    "library-003" to "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ForBiggerBlazes.mp4"
)

private val TITLE_CATALOGUE = mapOf(
    "feed-001" to "African Rhythms & the Future of Sound",
    "feed-002" to "Live: Cape Town City Session",
    "feed-003" to "Mesh Network Explained",
    "feed-004" to "Street Food Tour: Johannesburg Markets",
    "feed-005" to "Live: Underground Durban Jazz — Set 3",
    "nearby-001" to "DJ Khumalo — Live",
    "nearby-002" to "Sunset Groove — Live",
    "nearby-003" to "Aether Tech — Live",
    "library-001" to "Saved: African Rhythms",
    "library-002" to "Saved: Johannesburg Markets",
    "library-003" to "Saved: Tech Session"
)

@Composable
fun PlayerScreen(
    mediaId: String,
    onBack: () -> Unit,
    playerViewModel: PlayerViewModel = viewModel()
) {
    val context = LocalContext.current
    val playerState by playerViewModel.playerState.collectAsState()
    val currentPositionMs by playerViewModel.currentPositionMs.collectAsState()
    val durationMs by playerViewModel.durationMs.collectAsState()
    val isWatchParty by playerViewModel.isWatchPartyActive.collectAsState()
    val reactionOverlay by playerViewModel.reactionOverlay.collectAsState()

    val streamUri = STREAM_CATALOGUE[mediaId]
        ?: "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"
    val title = TITLE_CATALOGUE[mediaId] ?: "Aether Stream"

    var sliderPosition by remember { mutableFloatStateOf(0f) }
    var isDragging by remember { mutableStateOf(false) }
    var volumeLevel by remember { mutableFloatStateOf(1.0f) }
    var showReaction by remember { mutableStateOf(false) }
    var reactionEmoji by remember { mutableStateOf("") }

    // Setup player when screen appears
    DisposableEffect(mediaId) {
        playerViewModel.setupPlayer(context, streamUri)
        playerViewModel.play()
        onDispose {
            playerViewModel.cleanUp()
        }
    }

    // Poll position every 500ms while playing
    LaunchedEffect(playerState) {
        while (playerState == PlayerState.Playing) {
            playerViewModel.updatePosition()
            delay(500)
        }
    }

    // Sync slider with position when not dragging
    LaunchedEffect(currentPositionMs, durationMs) {
        if (!isDragging && durationMs > 0) {
            sliderPosition = currentPositionMs.toFloat() / durationMs.toFloat()
        }
    }

    // Reaction overlay auto-dismiss
    LaunchedEffect(reactionOverlay) {
        val emoji = reactionOverlay
        if (emoji != null) {
            reactionEmoji = emoji
            showReaction = true
            delay(2_000)
            showReaction = false
            playerViewModel.clearReactionOverlay()
        }
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.Black)
    ) {
        // Back button row
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(8.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(onClick = onBack) {
                Icon(
                    imageVector = Icons.Filled.ArrowBack,
                    contentDescription = "Back",
                    tint = Color.White
                )
            }
            Text(
                text = title,
                style = MaterialTheme.typography.bodyLarge,
                color = Color.White,
                fontWeight = FontWeight.SemiBold,
                maxLines = 1,
                modifier = Modifier.weight(1f)
            )
            if (isWatchParty) {
                Surface(
                    shape = RoundedCornerShape(4.dp),
                    color = MaterialTheme.colorScheme.primary
                ) {
                    Text(
                        text = "Watch Party",
                        color = Color.White,
                        modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp),
                        style = MaterialTheme.typography.labelSmall
                    )
                }
                Spacer(modifier = Modifier.width(8.dp))
            }
        }

        // Video surface
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .aspectRatio(16f / 9f)
        ) {
            AndroidView(
                factory = { ctx ->
                    PlayerView(ctx).apply {
                        layoutParams = ViewGroup.LayoutParams(
                            ViewGroup.LayoutParams.MATCH_PARENT,
                            ViewGroup.LayoutParams.MATCH_PARENT
                        )
                        useController = false
                    }
                },
                update = { playerView ->
                    playerView.player = playerViewModel.exoPlayer
                },
                modifier = Modifier.fillMaxSize()
            )

            // Buffering indicator
            if (playerState == PlayerState.Buffering) {
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .background(Color.Black.copy(alpha = 0.4f)),
                    contentAlignment = Alignment.Center
                ) {
                    androidx.compose.material3.CircularProgressIndicator(
                        color = MaterialTheme.colorScheme.primary
                    )
                }
            }

            // Floating reaction overlay — extracted to break implicit ColumnScope receiver chain
            ReactionOverlay(
                visible = showReaction,
                emoji   = reactionEmoji,
                modifier = Modifier
                    .align(Alignment.BottomEnd)
                    .padding(16.dp)
            )
        }

        // Controls
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .background(Color(0xFF1A1A2E))
                .padding(horizontal = 16.dp, vertical = 8.dp)
        ) {
            // Seek slider
            Slider(
                value = sliderPosition,
                onValueChange = { value ->
                    isDragging = true
                    sliderPosition = value
                },
                onValueChangeFinished = {
                    val seekMs = (sliderPosition * durationMs).toLong()
                    playerViewModel.seekTo(seekMs)
                    isDragging = false
                },
                modifier = Modifier.fillMaxWidth(),
                enabled = durationMs > 0
            )

            // Time labels
            Row(modifier = Modifier.fillMaxWidth()) {
                Text(
                    text = formatMs(currentPositionMs),
                    style = MaterialTheme.typography.labelSmall,
                    color = Color.White.copy(alpha = 0.7f)
                )
                Spacer(modifier = Modifier.weight(1f))
                Text(
                    text = if (durationMs > 0) formatMs(durationMs) else "LIVE",
                    style = MaterialTheme.typography.labelSmall,
                    color = Color.White.copy(alpha = 0.7f)
                )
            }

            Spacer(modifier = Modifier.height(4.dp))

            // Play/pause + volume row
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically
            ) {
                // Play / Pause
                val isPlaying = playerState == PlayerState.Playing
                IconButton(
                    onClick = { if (isPlaying) playerViewModel.pause() else playerViewModel.play() },
                    modifier = Modifier
                        .size(48.dp)
                        .background(MaterialTheme.colorScheme.primary, CircleShape)
                ) {
                    Icon(
                        imageVector = if (isPlaying) Icons.Filled.Pause else Icons.Filled.PlayArrow,
                        contentDescription = if (isPlaying) "Pause" else "Play",
                        tint = Color.White,
                        modifier = Modifier.size(28.dp)
                    )
                }

                Spacer(modifier = Modifier.width(12.dp))

                // Volume
                Icon(
                    imageVector = Icons.Filled.VolumeUp,
                    contentDescription = "Volume",
                    tint = Color.White.copy(alpha = 0.8f),
                    modifier = Modifier.size(20.dp)
                )
                Spacer(modifier = Modifier.width(8.dp))
                Slider(
                    value = volumeLevel,
                    onValueChange = { value ->
                        volumeLevel = value
                        playerViewModel.exoPlayer?.volume = value
                    },
                    modifier = Modifier.width(120.dp)
                )

                Spacer(modifier = Modifier.weight(1f))

                // Reaction buttons
                ReactionButton(emoji = "🔥") { playerViewModel.sendReaction("🔥") }
                ReactionButton(emoji = "👏") { playerViewModel.sendReaction("👏") }
                ReactionButton(emoji = "❤️") { playerViewModel.sendReaction("❤️") }
            }
        }
    }
}

/**
 * Animated overlay that shows [emoji] when [visible] is true.
 * Extracted into a top-level composable so that Kotlin's implicit-receiver resolution
 * never picks up [androidx.compose.foundation.layout.ColumnScope.AnimatedVisibility]
 * instead of the plain [AnimatedVisibility].
 */
@Composable
private fun ReactionOverlay(visible: Boolean, emoji: String, modifier: Modifier = Modifier) {
    Box(modifier = modifier) {
        AnimatedVisibility(
            visible = visible,
            enter   = fadeIn(),
            exit    = fadeOut(),
        ) {
            Text(
                text     = emoji,
                fontSize = 48.sp,
                modifier = Modifier
                    .background(Color.Black.copy(alpha = 0.2f), CircleShape)
                    .padding(8.dp)
            )
        }
    }
}

@Composable
private fun ReactionButton(emoji: String, onClick: () -> Unit) {
    IconButton(onClick = onClick, modifier = Modifier.size(40.dp)) {
        Text(text = emoji, fontSize = 20.sp)
    }
}

private fun formatMs(ms: Long): String {
    val totalSeconds = ms / 1000
    val hours = totalSeconds / 3600
    val minutes = (totalSeconds % 3600) / 60
    val seconds = totalSeconds % 60
    return if (hours > 0) {
        String.format("%d:%02d:%02d", hours, minutes, seconds)
    } else {
        String.format("%d:%02d", minutes, seconds)
    }
}
