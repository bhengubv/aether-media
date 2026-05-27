package market

import java.time.Instant
import java.util.UUID

data class PoVScore(
    val subjectId: UUID,
    val overallScore: Double,
    val tradeScore: Double = 0.0,
    val reliabilityScore: Double = 0.0,
    val responseScore: Double = 0.0,
    val disputeScore: Double = 0.0,
    val tokenCount: Int = 0,
    val positiveTokens: Int = 0,
    val negativeTokens: Int = 0,
    val neutralTokens: Int = 0,
    val successfulTrades: Int = 0,
    val failedTrades: Int = 0,
    val disputesRaised: Int = 0,
    val disputesResolved: Int = 0,
    val level: String = "UNRANKED",
    val lastUpdated: Instant = Instant.now()
) {
    val trustPercent: Double
        get() = overallScore.coerceIn(0.0, 100.0)

    val completionRate: Double
        get() {
            val total = successfulTrades + failedTrades
            return if (total == 0) 0.0 else (successfulTrades.toDouble() / total.toDouble()) * 100.0
        }
}
