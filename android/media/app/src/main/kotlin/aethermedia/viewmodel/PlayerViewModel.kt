package aethermedia.viewmodel

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import androidx.media3.common.MediaItem
import androidx.media3.common.Player
import androidx.media3.exoplayer.ExoPlayer
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

enum class PlayerState {
    Idle,
    Buffering,
    Playing,
    Paused,
    Ended,
    Error
}

class PlayerViewModel : ViewModel() {

    private val _playerState = MutableStateFlow(PlayerState.Idle)
    val playerState: StateFlow<PlayerState> = _playerState.asStateFlow()

    private val _currentPositionMs = MutableStateFlow(0L)
    val currentPositionMs: StateFlow<Long> = _currentPositionMs.asStateFlow()

    private val _durationMs = MutableStateFlow(0L)
    val durationMs: StateFlow<Long> = _durationMs.asStateFlow()

    private val _isWatchPartyActive = MutableStateFlow(false)
    val isWatchPartyActive: StateFlow<Boolean> = _isWatchPartyActive.asStateFlow()

    // Incoming mesh reactions — each emission is a single emoji string
    private val _reactionOverlay = MutableStateFlow<String?>(null)
    val reactionOverlay: StateFlow<String?> = _reactionOverlay.asStateFlow()

    var exoPlayer: ExoPlayer? = null
        private set

    private val playerListener = object : Player.Listener {
        override fun onPlaybackStateChanged(playbackState: Int) {
            _playerState.value = when (playbackState) {
                Player.STATE_IDLE -> PlayerState.Idle
                Player.STATE_BUFFERING -> PlayerState.Buffering
                Player.STATE_READY -> if (exoPlayer?.isPlaying == true) PlayerState.Playing else PlayerState.Paused
                Player.STATE_ENDED -> PlayerState.Ended
                else -> PlayerState.Idle
            }
        }

        override fun onIsPlayingChanged(isPlaying: Boolean) {
            if (_playerState.value != PlayerState.Ended) {
                _playerState.value = if (isPlaying) PlayerState.Playing else PlayerState.Paused
            }
        }
    }

    fun setupPlayer(context: Context, uri: String) {
        cleanUp()
        val player = ExoPlayer.Builder(context).build().also {
            it.addListener(playerListener)
            it.setMediaItem(MediaItem.fromUri(uri))
            it.prepare()
        }
        exoPlayer = player
        _durationMs.value = player.duration.coerceAtLeast(0L)
        _playerState.value = PlayerState.Buffering
    }

    fun play() {
        exoPlayer?.play()
    }

    fun pause() {
        exoPlayer?.pause()
    }

    fun seekTo(positionMs: Long) {
        exoPlayer?.seekTo(positionMs)
        _currentPositionMs.value = positionMs
    }

    fun updatePosition() {
        exoPlayer?.let { player ->
            _currentPositionMs.value = player.currentPosition
            _durationMs.value = player.duration.coerceAtLeast(0L)
        }
    }

    fun sendReaction(emoji: String) {
        // In production: broadcast over Aether mesh to co-viewers.
        // Locally trigger the overlay for immediate feedback.
        viewModelScope.launch {
            _reactionOverlay.value = emoji
        }
    }

    fun clearReactionOverlay() {
        _reactionOverlay.value = null
    }

    fun setWatchPartyActive(active: Boolean) {
        _isWatchPartyActive.value = active
    }

    fun cleanUp() {
        exoPlayer?.removeListener(playerListener)
        exoPlayer?.release()
        exoPlayer = null
        _playerState.value = PlayerState.Idle
    }

    override fun onCleared() {
        cleanUp()
        super.onCleared()
    }
}
