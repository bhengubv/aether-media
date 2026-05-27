import Foundation

public protocol ForgeServiceProtocol {
    func query(packageId: String, ecosystem: String, version: String?) async throws -> ForgeEntry?
    func cache(_ entry: ForgeEntry) async throws -> ForgeEntry
    func fetch(packageId: String, ecosystem: String, version: String) async throws -> Data
    func stats() async throws -> ForgeStats
    func evict(entryId: UUID) async throws -> Bool
    func listByEcosystem(_ ecosystem: String, limit: Int, offset: Int) async throws -> [ForgeEntry]
    func search(query: String, ecosystem: String?) async throws -> [ForgeEntry]
    func sync(peerNodeId: String) async throws -> Int
}

public class ForgeService: ForgeServiceProtocol {

    public init() {}

    public func query(packageId: String, ecosystem: String, version: String? = nil) async throws -> ForgeEntry? {
        fatalError("not implemented")
    }

    public func cache(_ entry: ForgeEntry) async throws -> ForgeEntry {
        fatalError("not implemented")
    }

    public func fetch(packageId: String, ecosystem: String, version: String) async throws -> Data {
        fatalError("not implemented")
    }

    public func stats() async throws -> ForgeStats {
        fatalError("not implemented")
    }

    public func evict(entryId: UUID) async throws -> Bool {
        fatalError("not implemented")
    }

    public func listByEcosystem(_ ecosystem: String, limit: Int = 50, offset: Int = 0) async throws -> [ForgeEntry] {
        fatalError("not implemented")
    }

    public func search(query: String, ecosystem: String? = nil) async throws -> [ForgeEntry] {
        fatalError("not implemented")
    }

    public func sync(peerNodeId: String) async throws -> Int {
        fatalError("not implemented")
    }
}
