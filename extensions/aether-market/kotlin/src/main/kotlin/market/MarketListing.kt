package market

import java.time.Instant
import java.util.UUID

enum class MarketCategory {
    GOODS,
    SERVICES,
    DIGITAL,
    FOOD,
    TRANSPORT,
    HOUSING,
    LABOUR,
    SKILLS,
    BARTER,
    OTHER
}

data class MarketListing(
    val id: UUID = UUID.randomUUID(),
    val sellerId: UUID,
    val spaceId: UUID? = null,
    val geoHash: String = "",
    val category: MarketCategory,
    val title: String,
    val description: String,
    val priceAmount: Double,
    val priceCurrency: String = "ZAR",
    val acceptsBarter: Boolean = false,
    val barterDescription: String = "",
    val imageUrls: List<String> = emptyList(),
    val tags: List<String> = emptyList(),
    val isAvailable: Boolean = true,
    val quantity: Int = 1,
    val requiresEscrow: Boolean = false,
    val minimumPoVScore: Double = 0.0,
    val viewCount: Int = 0,
    val enquiryCount: Int = 0,
    val createdAt: Instant = Instant.now(),
    val updatedAt: Instant = Instant.now(),
    val expiresAt: Instant? = null
)
