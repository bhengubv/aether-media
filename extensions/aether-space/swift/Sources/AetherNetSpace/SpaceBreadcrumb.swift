import Foundation

public enum BreadcrumbType: String, Codable, Equatable, CaseIterable {
    case post = "POST"
    case event = "EVENT"
    case alert = "ALERT"
    case offer = "OFFER"
    case notice = "NOTICE"
    case pinned = "PINNED"
}

public struct SpaceBreadcrumb: Codable, Equatable {
    public let id: UUID
    public let spaceId: UUID
    public let authorId: UUID
    public let geoHash: String
    public let type: BreadcrumbType
    public let title: String
    public let body: String
    public var mediaUrls: [String]
    public var tags: [String]
    public var expiresAt: Date?
    public var isPinned: Bool
    public var reactionCount: Int
    public var replyCount: Int
    public let createdAt: Date
    public var updatedAt: Date

    public init(
        id: UUID = UUID(),
        spaceId: UUID,
        authorId: UUID,
        geoHash: String,
        type: BreadcrumbType,
        title: String,
        body: String,
        mediaUrls: [String] = [],
        tags: [String] = [],
        expiresAt: Date? = nil,
        isPinned: Bool = false,
        reactionCount: Int = 0,
        replyCount: Int = 0,
        createdAt: Date = Date(),
        updatedAt: Date = Date()
    ) {
        self.id = id
        self.spaceId = spaceId
        self.authorId = authorId
        self.geoHash = geoHash
        self.type = type
        self.title = title
        self.body = body
        self.mediaUrls = mediaUrls
        self.tags = tags
        self.expiresAt = expiresAt
        self.isPinned = isPinned
        self.reactionCount = reactionCount
        self.replyCount = replyCount
        self.createdAt = createdAt
        self.updatedAt = updatedAt
    }
}
