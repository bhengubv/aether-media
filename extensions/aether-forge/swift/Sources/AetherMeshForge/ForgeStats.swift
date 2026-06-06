import Foundation

public struct ForgeStats: Codable, Equatable {
    public var totalEntries: Int64
    public var totalSizeBytes: Int64
    public var totalDownloads: Int64
    public var uniqueEcosystems: Int
    public var uniquePackages: Int64
    public var verifiedPackages: Int64
    public var hitRate: Double
    public var missRate: Double
    public var averagePackageSizeBytes: Int64
    public var peakDownloadsPerHour: Int64
    public var activePeers: Int
    public let lastUpdated: Date
    public var ecosystemBreakdown: [String: Int64]

    public init(
        totalEntries: Int64 = 0,
        totalSizeBytes: Int64 = 0,
        totalDownloads: Int64 = 0,
        uniqueEcosystems: Int = 0,
        uniquePackages: Int64 = 0,
        verifiedPackages: Int64 = 0,
        hitRate: Double = 0.0,
        missRate: Double = 0.0,
        averagePackageSizeBytes: Int64 = 0,
        peakDownloadsPerHour: Int64 = 0,
        activePeers: Int = 0,
        lastUpdated: Date = Date(),
        ecosystemBreakdown: [String: Int64] = [:]
    ) {
        self.totalEntries = totalEntries
        self.totalSizeBytes = totalSizeBytes
        self.totalDownloads = totalDownloads
        self.uniqueEcosystems = uniqueEcosystems
        self.uniquePackages = uniquePackages
        self.verifiedPackages = verifiedPackages
        self.hitRate = hitRate
        self.missRate = missRate
        self.averagePackageSizeBytes = averagePackageSizeBytes
        self.peakDownloadsPerHour = peakDownloadsPerHour
        self.activePeers = activePeers
        self.lastUpdated = lastUpdated
        self.ecosystemBreakdown = ecosystemBreakdown
    }
}
