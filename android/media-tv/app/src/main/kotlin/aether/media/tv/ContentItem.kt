package aether.media.tv

data class ContentItem(
    val id: String,
    val title: String,
    val description: String,
    val durationMs: Long,
    val creatorTag: String,
    val streamUri: String
) {
    val formattedDuration: String
        get() {
            val totalSeconds = durationMs / 1000
            val hours = totalSeconds / 3600
            val minutes = (totalSeconds % 3600) / 60
            val seconds = totalSeconds % 60
            return if (hours > 0) {
                String.format("%dh %02dm %02ds", hours, minutes, seconds)
            } else {
                String.format("%d:%02d", minutes, seconds)
            }
        }
}
