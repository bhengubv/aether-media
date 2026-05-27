import Foundation

public protocol PoVServiceProtocol {
    func issueToken(_ token: PoVToken) async throws -> PoVToken
    func revokeToken(tokenId: UUID, reason: String) async throws -> Bool
    func getScore(subjectId: UUID) async throws -> PoVScore
    func getTokensFor(subjectId: UUID) async throws -> [PoVToken]
    func getTokensBy(issuerId: UUID) async throws -> [PoVToken]
    func verifyToken(tokenId: UUID) async throws -> Bool
    func syncTokens(peerNodeId: String) async throws -> Int
}

public class PoVService: PoVServiceProtocol {

    public init() {}

    public func issueToken(_ token: PoVToken) async throws -> PoVToken {
        fatalError("not implemented")
    }

    public func revokeToken(tokenId: UUID, reason: String) async throws -> Bool {
        fatalError("not implemented")
    }

    public func getScore(subjectId: UUID) async throws -> PoVScore {
        fatalError("not implemented")
    }

    public func getTokensFor(subjectId: UUID) async throws -> [PoVToken] {
        fatalError("not implemented")
    }

    public func getTokensBy(issuerId: UUID) async throws -> [PoVToken] {
        fatalError("not implemented")
    }

    public func verifyToken(tokenId: UUID) async throws -> Bool {
        fatalError("not implemented")
    }

    public func syncTokens(peerNodeId: String) async throws -> Int {
        fatalError("not implemented")
    }
}
