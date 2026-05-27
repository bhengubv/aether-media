package market

import java.util.UUID

interface IMarketService {
    suspend fun createListing(listing: MarketListing): MarketListing
    suspend fun updateListing(listing: MarketListing): MarketListing
    suspend fun deleteListing(listingId: UUID, requesterId: UUID): Boolean
    suspend fun getListing(listingId: UUID): MarketListing?
    suspend fun search(query: String, category: MarketCategory? = null, geoHash: String? = null): List<MarketListing>
    suspend fun listBySeller(sellerId: UUID, limit: Int = 50, offset: Int = 0): List<MarketListing>
    suspend fun listBySpace(spaceId: UUID, limit: Int = 50, offset: Int = 0): List<MarketListing>
    suspend fun initiateEscrow(listingId: UUID, buyerId: UUID): TradeEscrow
    suspend fun fundEscrow(escrowId: UUID, buyerId: UUID): TradeEscrow
    suspend fun confirmDelivery(escrowId: UUID, buyerId: UUID): TradeEscrow
    suspend fun confirmDispatch(escrowId: UUID, sellerId: UUID): TradeEscrow
    suspend fun raiseDispute(escrowId: UUID, raiserId: UUID, reason: String): TradeEscrow
    suspend fun resolveDispute(escrowId: UUID, arbiterId: UUID, notes: String, favourBuyer: Boolean): TradeEscrow
    suspend fun cancelEscrow(escrowId: UUID, requesterId: UUID): TradeEscrow
    suspend fun getEscrow(escrowId: UUID): TradeEscrow?
}

class MarketServiceImpl : IMarketService {

    override suspend fun createListing(listing: MarketListing): MarketListing {
        TODO("not implemented")
    }

    override suspend fun updateListing(listing: MarketListing): MarketListing {
        TODO("not implemented")
    }

    override suspend fun deleteListing(listingId: UUID, requesterId: UUID): Boolean {
        TODO("not implemented")
    }

    override suspend fun getListing(listingId: UUID): MarketListing? {
        TODO("not implemented")
    }

    override suspend fun search(query: String, category: MarketCategory?, geoHash: String?): List<MarketListing> {
        TODO("not implemented")
    }

    override suspend fun listBySeller(sellerId: UUID, limit: Int, offset: Int): List<MarketListing> {
        TODO("not implemented")
    }

    override suspend fun listBySpace(spaceId: UUID, limit: Int, offset: Int): List<MarketListing> {
        TODO("not implemented")
    }

    override suspend fun initiateEscrow(listingId: UUID, buyerId: UUID): TradeEscrow {
        TODO("not implemented")
    }

    override suspend fun fundEscrow(escrowId: UUID, buyerId: UUID): TradeEscrow {
        TODO("not implemented")
    }

    override suspend fun confirmDelivery(escrowId: UUID, buyerId: UUID): TradeEscrow {
        TODO("not implemented")
    }

    override suspend fun confirmDispatch(escrowId: UUID, sellerId: UUID): TradeEscrow {
        TODO("not implemented")
    }

    override suspend fun raiseDispute(escrowId: UUID, raiserId: UUID, reason: String): TradeEscrow {
        TODO("not implemented")
    }

    override suspend fun resolveDispute(escrowId: UUID, arbiterId: UUID, notes: String, favourBuyer: Boolean): TradeEscrow {
        TODO("not implemented")
    }

    override suspend fun cancelEscrow(escrowId: UUID, requesterId: UUID): TradeEscrow {
        TODO("not implemented")
    }

    override suspend fun getEscrow(escrowId: UUID): TradeEscrow? {
        TODO("not implemented")
    }
}
