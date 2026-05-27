import Foundation

public struct VaultManifest: Codable, Equatable {
    public let id: UUID
    public let ownerId: UUID
    public let name: String
    public var description: String
    public let originalSizeBytes: Int64
    public let encodedSizeBytes: Int64
    public let shardCount: Int
    public let parityShardCount: Int
    public let minShardsForRecovery: Int
    public let checksum: String
    public var checksumAlgorithm: String
    public var encryptionAlgorithm: String
    public var encryptedKeyHint: String
    public var contentType: String
    public var tags: [String]
    public var shardIds: [UUID]
    public var replicationFactor: Int
    public let createdAt: Date
    public var updatedAt: Date
    public var expiresAt: Date?
    public var metadata: [String: String]

    public init(
        id: UUID = UUID(),
        ownerId: UUID,
        name: String,
        description: String = "",
        originalSizeBytes: Int64,
        encodedSizeBytes: Int64,
        shardCount: Int,
        parityShardCount: Int,
        minShardsForRecovery: Int,
        checksum: String,
        checksumAlgorithm: String = "sha256",
        encryptionAlgorithm: String = "AES-256-GCM",
        encryptedKeyHint: String = "",
        contentType: String = "application/octet-stream",
        tags: [String] = [],
        shardIds: [UUID] = [],
        replicationFactor: Int = 3,
        createdAt: Date = Date(),
        updatedAt: Date = Date(),
        expiresAt: Date? = nil,
        metadata: [String: String] = [:]
    ) {
        self.id = id
        self.ownerId = ownerId
        self.name = name
        self.description = description
        self.originalSizeBytes = originalSizeBytes
        self.encodedSizeBytes = encodedSizeBytes
        self.shardCount = shardCount
        self.parityShardCount = parityShardCount
        self.minShardsForRecovery = minShardsForRecovery
        self.checksum = checksum
        self.checksumAlgorithm = checksumAlgorithm
        self.encryptionAlgorithm = encryptionAlgorithm
        self.encryptedKeyHint = encryptedKeyHint
        self.contentType = contentType
        self.tags = tags
        self.shardIds = shardIds
        self.replicationFactor = replicationFactor
        self.createdAt = createdAt
        self.updatedAt = updatedAt
        self.expiresAt = expiresAt
        self.metadata = metadata
    }
}
