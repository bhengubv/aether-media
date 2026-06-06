package aethermedia.tv

import android.os.Bundle
import android.view.KeyEvent
import android.view.View
import android.view.ViewGroup
import android.widget.FrameLayout
import android.widget.ImageButton
import android.widget.LinearLayout
import android.widget.ProgressBar
import android.widget.SeekBar
import android.widget.TextView
import androidx.fragment.app.FragmentActivity
import androidx.media3.common.MediaItem
import androidx.media3.common.Player
import androidx.media3.exoplayer.ExoPlayer
import androidx.media3.ui.PlayerView

/**
 * Full-screen player Activity for Android TV.
 *
 * Accepts extras:
 *  - [EXTRA_CONTENT_ID]    — content identifier
 *  - [EXTRA_CONTENT_TITLE] — display title
 *  - [EXTRA_STREAM_URI]    — HLS or MP4 URI to play
 *
 * D-pad controls:
 *  - DPAD_CENTER / ENTER → play/pause toggle
 *  - DPAD_LEFT           → seek back 10 s
 *  - DPAD_RIGHT          → seek forward 10 s
 *  - BACK                → finish
 */
class TvPlayerActivity : FragmentActivity() {

    companion object {
        const val EXTRA_CONTENT_ID = "content_id"
        const val EXTRA_CONTENT_TITLE = "content_title"
        const val EXTRA_STREAM_URI = "stream_uri"
        private const val SEEK_STEP_MS = 10_000L
        private const val CONTROLS_HIDE_DELAY_MS = 4_000L
    }

    private lateinit var playerView: PlayerView
    private lateinit var titleView: TextView
    private lateinit var seekBar: SeekBar
    private lateinit var positionView: TextView
    private lateinit var durationView: TextView
    private lateinit var playPauseButton: ImageButton
    private lateinit var bufferingIndicator: ProgressBar
    private lateinit var controlsContainer: View

    private var exoPlayer: ExoPlayer? = null
    private val hideControlsRunnable = Runnable { hideControls() }

    private val seekBarChangeListener = object : SeekBar.OnSeekBarChangeListener {
        override fun onProgressChanged(seekBar: SeekBar, progress: Int, fromUser: Boolean) {
            if (fromUser) {
                exoPlayer?.seekTo(progress.toLong())
                updatePositionLabel(progress.toLong())
            }
        }
        override fun onStartTrackingTouch(seekBar: SeekBar) {}
        override fun onStopTrackingTouch(seekBar: SeekBar) {}
    }

    private val playerListener = object : Player.Listener {
        override fun onPlaybackStateChanged(playbackState: Int) {
            when (playbackState) {
                Player.STATE_BUFFERING -> bufferingIndicator.visibility = View.VISIBLE
                Player.STATE_READY -> {
                    bufferingIndicator.visibility = View.GONE
                    val durationMs = exoPlayer?.duration ?: 0L
                    seekBar.max = durationMs.toInt().coerceAtLeast(0)
                    updateDurationLabel(durationMs)
                    updatePlayPauseIcon()
                }
                Player.STATE_ENDED -> updatePlayPauseIcon()
                else -> { /* no-op for IDLE */ }
            }
        }

        override fun onIsPlayingChanged(isPlaying: Boolean) {
            updatePlayPauseIcon()
        }
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        val title = intent.getStringExtra(EXTRA_CONTENT_TITLE) ?: "Aether Media"
        val streamUri = intent.getStringExtra(EXTRA_STREAM_URI)
            ?: "https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"

        val layout = buildLayout()
        setContentView(layout)

        titleView.text = title

        initPlayer(streamUri)
        scheduleHideControls()
    }

    override fun onResume() {
        super.onResume()
        exoPlayer?.play()
    }

    override fun onPause() {
        super.onPause()
        exoPlayer?.pause()
    }

    override fun onDestroy() {
        super.onDestroy()
        controlsContainer.removeCallbacks(hideControlsRunnable)
        exoPlayer?.removeListener(playerListener)
        exoPlayer?.release()
        exoPlayer = null
    }

    // ── D-pad input ───────────────────────────────────────────────────────────

    override fun onKeyDown(keyCode: Int, event: KeyEvent?): Boolean {
        showControls()
        scheduleHideControls()
        return when (keyCode) {
            KeyEvent.KEYCODE_DPAD_CENTER, KeyEvent.KEYCODE_ENTER, KeyEvent.KEYCODE_MEDIA_PLAY_PAUSE -> {
                togglePlayPause()
                true
            }
            KeyEvent.KEYCODE_DPAD_LEFT, KeyEvent.KEYCODE_MEDIA_REWIND -> {
                seekRelative(-SEEK_STEP_MS)
                true
            }
            KeyEvent.KEYCODE_DPAD_RIGHT, KeyEvent.KEYCODE_MEDIA_FAST_FORWARD -> {
                seekRelative(SEEK_STEP_MS)
                true
            }
            else -> super.onKeyDown(keyCode, event)
        }
    }

    // ── Player ────────────────────────────────────────────────────────────────

    private fun initPlayer(streamUri: String) {
        val player = ExoPlayer.Builder(this).build().also {
            it.addListener(playerListener)
            it.setMediaItem(MediaItem.fromUri(streamUri))
            it.prepare()
            it.play()
        }
        exoPlayer = player
        playerView.player = player

        // Poll position for seekbar
        seekBar.setOnSeekBarChangeListener(seekBarChangeListener)

        playerView.postDelayed(object : Runnable {
            override fun run() {
                val p = player
                if (p != null && !isDestroyed) {
                    val pos = p.currentPosition
                    seekBar.progress = pos.toInt()
                    updatePositionLabel(pos)
                    playerView.postDelayed(this, 500)
                }
            }
        }, 500)
    }

    private fun togglePlayPause() {
        exoPlayer?.let { player ->
            if (player.isPlaying) player.pause() else player.play()
        }
    }

    private fun seekRelative(offsetMs: Long) {
        exoPlayer?.let { player ->
            val target = (player.currentPosition + offsetMs).coerceIn(0L, player.duration)
            player.seekTo(target)
        }
    }

    private fun updatePlayPauseIcon() {
        val isPlaying = exoPlayer?.isPlaying == true
        playPauseButton.setImageResource(
            if (isPlaying) android.R.drawable.ic_media_pause
            else android.R.drawable.ic_media_play
        )
    }

    // ── Controls visibility ───────────────────────────────────────────────────

    private fun showControls() {
        controlsContainer.visibility = View.VISIBLE
    }

    private fun hideControls() {
        controlsContainer.visibility = View.GONE
    }

    private fun scheduleHideControls() {
        controlsContainer.removeCallbacks(hideControlsRunnable)
        controlsContainer.postDelayed(hideControlsRunnable, CONTROLS_HIDE_DELAY_MS)
    }

    // ── Label helpers ─────────────────────────────────────────────────────────

    private fun updatePositionLabel(posMs: Long) {
        positionView.text = formatMs(posMs)
    }

    private fun updateDurationLabel(durationMs: Long) {
        durationView.text = if (durationMs > 0) formatMs(durationMs) else "LIVE"
    }

    private fun formatMs(ms: Long): String {
        val totalSec = ms / 1000
        val h = totalSec / 3600
        val m = (totalSec % 3600) / 60
        val s = totalSec % 60
        return if (h > 0) String.format("%d:%02d:%02d", h, m, s)
        else String.format("%d:%02d", m, s)
    }

    // ── Layout (programmatic) ─────────────────────────────────────────────────

    private fun buildLayout(): View {
        val density = resources.displayMetrics.density
        fun dp(v: Int) = (v * density + 0.5f).toInt()

        // Root fills screen
        val root = FrameLayout(this).apply {
            layoutParams = ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MATCH_PARENT,
                ViewGroup.LayoutParams.MATCH_PARENT
            )
            setBackgroundColor(0xFF000000.toInt())
        }

        // PlayerView
        playerView = PlayerView(this).apply {
            layoutParams = FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.MATCH_PARENT
            )
            useController = false
        }
        root.addView(playerView)

        // Buffering indicator
        bufferingIndicator = ProgressBar(this).apply {
            layoutParams = FrameLayout.LayoutParams(
                dp(48), dp(48), android.view.Gravity.CENTER
            )
        }
        root.addView(bufferingIndicator)

        // Controls overlay (bottom)
        val controls = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL
            setBackgroundColor(0xCC000000.toInt())
            val lp = FrameLayout.LayoutParams(
                FrameLayout.LayoutParams.MATCH_PARENT,
                FrameLayout.LayoutParams.WRAP_CONTENT,
                android.view.Gravity.BOTTOM
            )
            layoutParams = lp
            setPadding(dp(24), dp(8), dp(24), dp(16))
        }
        controlsContainer = controls

        titleView = TextView(this).apply {
            textSize = 18f
            setTextColor(0xFFFFFFFF.toInt())
            maxLines = 1
            ellipsize = android.text.TextUtils.TruncateAt.END
        }
        controls.addView(titleView)

        seekBar = SeekBar(this).apply {
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            )
        }
        controls.addView(seekBar)

        val timeRow = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            layoutParams = LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MATCH_PARENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
            )
        }

        positionView = TextView(this).apply {
            textSize = 13f
            setTextColor(0xB3FFFFFF.toInt())
            text = "0:00"
        }

        val timeSpacer = View(this).apply {
            layoutParams = LinearLayout.LayoutParams(0, 1, 1f)
        }

        playPauseButton = ImageButton(this).apply {
            setImageResource(android.R.drawable.ic_media_pause)
            setBackgroundColor(android.graphics.Color.TRANSPARENT)
            setOnClickListener { togglePlayPause() }
            layoutParams = LinearLayout.LayoutParams(dp(40), dp(40))
        }

        durationView = TextView(this).apply {
            textSize = 13f
            setTextColor(0xB3FFFFFF.toInt())
            text = "0:00"
        }

        timeRow.addView(positionView)
        timeRow.addView(timeSpacer)
        timeRow.addView(playPauseButton)
        val spacer2 = View(this).apply { layoutParams = LinearLayout.LayoutParams(0, 1, 1f) }
        timeRow.addView(spacer2)
        timeRow.addView(durationView)
        controls.addView(timeRow)

        root.addView(controls)
        return root
    }
}
