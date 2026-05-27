package market

import java.time.Instant
import java.util.UUID

enum class TradeState {
    INITIATED,
    FUNDED,
    GOODS_SENT,
    GOODS_RECEIVED,
    DISPUTED,
    RESOLVED,
    CANCELLED,
    EXPIRED,
    COMPLETED
}

enum class TradeRole {
    BUYER,
    SELLER,
    ARBITER
}

data class TradeEscrow(
    val id: UUID = UUID.randomUUID(),
    val listingId: UUID,
    val buyerId: UUID,
    val sellerId: UUID,
    val arbiterId: UUID? = null,
    val state: TradeState = TradeState.INITIATED,
    val amount: Double,
    val currency: String = "ZAR",
    val description: String = "",
    val buyerPoVScore: Double = 0.0,
    val sellerPoVScore: Double = 0.0,
    val buyerConfirmed: Boolean = false,
    val sellerConfirmed: Boolean = false,
    val disputeReason: String = "",
    val resolutionNotes: String = "",
    val escrowAddress: String = "",
    val meshTransactionId: String = "",
    val timeoutHours: Int = 72,
    val createdAt: Instant = Instant.now(),
    val updatedAt: Instant = Instant.now(),
    val completedAt: Instant? = null,
    val expiresAt: Instant? = null
) {
    val isActive: Boolean
        get() = state in listOf(
            TradeState.INITIATED,
            TradeState.FUNDED,
            TradeState.GOODS_SENT,
            TradeState.DISPUTED
        )

    val isTerminal: Boolean
        get() = state in listOf(
            TradeState.COMPLETED,
            TradeState.CANCELLED,
            TradeState.EXPIRED,
            TradeState.RESOLVED
        )
}
