import Foundation

public struct ForgeEntry: Codable, Equatable {
    public let id: UUID
    public let packageId: String
    public let ecosystem: String
    public let version: String
    public let name: String
    public var description: String
    public var author: String
    public var licenseId: String
    public var sizeBytes: Int64
    public let checksum: String
    public var checksumAlgorithm: String
    public let downloadUrl: String
    public var mirrorUrls: [String]
    public var dependencies: [String]
    public var tags: [String]
    public var isVerified: Bool
    public var downloadCount: Int64
    public let cachedAt: Date
    public var expiresAt: Date?
    public var metadata: [String: String]

    public init(
        id: UUID = UUID(),
        packageId: String,
        ecosystem: String,
        version: String,
        name: String,
        description: String = "",
        author: String = "",
        licenseId: String = "",
        sizeBytes: Int64 = 0,
        checksum: String,
        checksumAlgorithm: String = "sha256",
        downloadUrl: String,
        mirrorUrls: [String] = [],
        dependencies: [String] = [],
        tags: [String] = [],
        isVerified: Bool = false,
        downloadCount: Int64 = 0,
        cachedAt: Date = Date(),
        expiresAt: Date? = nil,
        metadata: [String: String] = [:]
    ) {
        self.id = id
        self.packageId = packageId
        self.ecosystem = ecosystem
        self.version = version
        self.name = name
        self.description = description
        self.author = author
        self.licenseId = licenseId
        self.sizeBytes = sizeBytes
        self.checksum = checksum
        self.checksumAlgorithm = checksumAlgorithm
        self.downloadUrl = downloadUrl
        self.mirrorUrls = mirrorUrls
        self.dependencies = dependencies
        self.tags = tags
        self.isVerified = isVerified
        self.downloadCount = downloadCount
        self.cachedAt = cachedAt
        self.expiresAt = expiresAt
        self.metadata = metadata
    }
}
