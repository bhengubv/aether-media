import Foundation

public enum TradeState: String, Codable, CaseIterable {
    case initiated = "INITIATED"
    case funded = "FUNDED"
    case goodsSent = "GOODS_SENT"
    case goodsReceived = "GOODS_RECEIVED"
    case disputed = "DISPUTED"
    case resolved = "RESOLVED"
    case cancelled = "CANCELLED"
    case expired = "EXPIRED"
    case completed = "COMPLETED"
}

public enum TradeRole: String, Codable, CaseIterable {
    case buyer = "BUYER"
    case seller = "SELLER"
    case arbiter = "ARBITER"
}

public struct TradeEscrow: Codable, Equatable {
    public let id: UUID
    public let listingId: UUID
    public let buyerId: UUID
    public let sellerId: UUID
    public var arbiterId: UUID?
    public var state: TradeState
    public let amount: Double
    public var currency: String
    public var description: String
    public var buyerPoVScore: Double
    public var sellerPoVScore: Double
    public var buyerConfirmed: Bool
    public var sellerConfirmed: Bool
    public var disputeReason: String
    public var resolutionNotes: String
    public var escrowAddress: String
    public var meshTransactionId: String
    public var timeoutHours: Int
    public let createdAt: Date
    public var updatedAt: Date
    public var completedAt: Date?
    public var expiresAt: Date?

    public var isActive: Bool {
        [.initiated, .funded, .goodsSent, .disputed].contains(state)
    }

    public var isTerminal: Bool {
        [.completed, .cancelled, .expired, .resolved].contains(state)
    }

    public init(
        id: UUID = UUID(),
        listingId: UUID,
        buyerId: UUID,
        sellerId: UUID,
        arbiterId: UUID? = nil,
        state: TradeState = .initiated,
        amount: Double,
        currency: String = "ZAR",
        description: String = "",
        buyerPoVScore: Double = 0.0,
        sellerPoVScore: Double = 0.0,
        buyerConfirmed: Bool = false,
        sellerConfirmed: Bool = false,
        disputeReason: String = "",
        resolutionNotes: String = "",
        escrowAddress: String = "",
        meshTransactionId: String = "",
        timeoutHours: Int = 72,
        createdAt: Date = Date(),
        updatedAt: Date = Date(),
        completedAt: Date? = nil,
        expiresAt: Date? = nil
    ) {
        self.id = id
        self.listingId = listingId
        self.buyerId = buyerId
        self.sellerId = sellerId
        self.arbiterId = arbiterId
        self.state = state
        self.amount = amount
        self.currency = currency
        self.description = description
        self.buyerPoVScore = buyerPoVScore
        self.sellerPoVScore = sellerPoVScore
        self.buyerConfirmed = buyerConfirmed
        self.sellerConfirmed = sellerConfirmed
        self.disputeReason = disputeReason
        self.resolutionNotes = resolutionNotes
        self.escrowAddress = escrowAddress
        self.meshTransactionId = meshTransactionId
        self.timeoutHours = timeoutHours
        self.createdAt = createdAt
        self.updatedAt = updatedAt
        self.completedAt = completedAt
        self.expiresAt = expiresAt
    }
}
