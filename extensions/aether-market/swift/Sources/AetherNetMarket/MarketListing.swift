import Foundation

public enum MarketCategory: String, Codable, CaseIterable {
    case goods = "GOODS"
    case services = "SERVICES"
    case digital = "DIGITAL"
    case food = "FOOD"
    case transport = "TRANSPORT"
    case housing = "HOUSING"
    case labour = "LABOUR"
    case skills = "SKILLS"
    case barter = "BARTER"
    case other = "OTHER"
}

public struct MarketListing: Codable, Equatable {
    public let id: UUID
    public let sellerId: UUID
    public var spaceId: UUID?
    public var geoHash: String
    public var category: MarketCategory
    public var title: String
    public var description: String
    public var priceAmount: Double
    public var priceCurrency: String
    public var acceptsBarter: Bool
    public var barterDescription: String
    public var imageUrls: [String]
    public var tags: [String]
    public var isAvailable: Bool
    public var quantity: Int
    public var requiresEscrow: Bool
    public var minimumPoVScore: Double
    public var viewCount: Int
    public var enquiryCount: Int
    public let createdAt: Date
    public var updatedAt: Date
    public var expiresAt: Date?

    public init(
        id: UUID = UUID(),
        sellerId: UUID,
        spaceId: UUID? = nil,
        geoHash: String = "",
        category: MarketCategory,
        title: String,
        description: String,
        priceAmount: Double,
        priceCurrency: String = "ZAR",
        acceptsBarter: Bool = false,
        barterDescription: String = "",
        imageUrls: [String] = [],
        tags: [String] = [],
        isAvailable: Bool = true,
        quantity: Int = 1,
        requiresEscrow: Bool = false,
        minimumPoVScore: Double = 0.0,
        viewCount: Int = 0,
        enquiryCount: Int = 0,
        createdAt: Date = Date(),
        updatedAt: Date = Date(),
        expiresAt: Date? = nil
    ) {
        self.id = id
        self.sellerId = sellerId
        self.spaceId = spaceId
        self.geoHash = geoHash
        self.category = category
        self.title = title
        self.description = description
        self.priceAmount = priceAmount
        self.priceCurrency = priceCurrency
        self.acceptsBarter = acceptsBarter
        self.barterDescription = barterDescription
        self.imageUrls = imageUrls
        self.tags = tags
        self.isAvailable = isAvailable
        self.quantity = quantity
        self.requiresEscrow = requiresEscrow
        self.minimumPoVScore = minimumPoVScore
        self.viewCount = viewCount
        self.enquiryCount = enquiryCount
        self.createdAt = createdAt
        self.updatedAt = updatedAt
        self.expiresAt = expiresAt
    }
}
