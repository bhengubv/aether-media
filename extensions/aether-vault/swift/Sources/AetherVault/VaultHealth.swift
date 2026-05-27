import Foundation

public struct VaultHealth: Codable, Equatable {
    public let manifestId: UUID
    public let totalShards: Int
    public let availableShards: Int
    public let parityShards: Int
    public let availableParityShards: Int
    public let minShardsForRecovery: Int
    public let replicationFactor: Int
    public var degradedNodes: [String]
    public let lastCheckedAt: Date

    public init(
        manifestId: UUID,
        totalShards: Int,
        availableShards: Int,
        parityShards: Int,
        availableParityShards: Int,
        minShardsForRecovery: Int,
        replicationFactor: Int,
        degradedNodes: [String] = [],
        lastCheckedAt: Date = Date()
    ) {
        self.manifestId = manifestId
        self.totalShards = totalShards
        self.availableShards = availableShards
        self.parityShards = parityShards
        self.availableParityShards = availableParityShards
        self.minShardsForRecovery = minShardsForRecovery
        self.replicationFactor = replicationFactor
        self.degradedNodes = degradedNodes
        self.lastCheckedAt = lastCheckedAt
    }

    public var isRecoverable: Bool {
        availableShards >= minShardsForRecovery
    }

    public var isHealthy: Bool {
        availableShards == totalShards && availableParityShards == parityShards
    }

    public var isDegraded: Bool {
        isRecoverable && !isHealthy
    }

    public var healthPercent: Double {
        guard totalShards > 0 else { return 0.0 }
        return (Double(availableShards) / Double(totalShards)) * 100.0
    }
}
