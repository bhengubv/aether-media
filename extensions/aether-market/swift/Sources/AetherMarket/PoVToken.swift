import Foundation

public enum PoVTransport: String, Codable, CaseIterable {
    case mesh = "MESH"
    case bluetooth = "BLUETOOTH"
    case nfc = "NFC"
    case qrCode = "QR_CODE"
    case directLink = "DIRECT_LINK"
}

public struct PoVToken: Codable, Equatable {
    public let id: UUID
    public let issuerId: UUID
    public let subjectId: UUID
    public let context: String
    public let claim: String
    public var evidence: String
    public var transport: PoVTransport
    public let signature: String
    public var publicKeyHint: String
    public var weight: Double
    public var isRevoked: Bool
    public var revokedReason: String
    public let issuedAt: Date
    public var expiresAt: Date?

    public init(
        id: UUID = UUID(),
        issuerId: UUID,
        subjectId: UUID,
        context: String,
        claim: String,
        evidence: String = "",
        transport: PoVTransport = .mesh,
        signature: String,
        publicKeyHint: String = "",
        weight: Double = 1.0,
        isRevoked: Bool = false,
        revokedReason: String = "",
        issuedAt: Date = Date(),
        expiresAt: Date? = nil
    ) {
        self.id = id
        self.issuerId = issuerId
        self.subjectId = subjectId
        self.context = context
        self.claim = claim
        self.evidence = evidence
        self.transport = transport
        self.signature = signature
        self.publicKeyHint = publicKeyHint
        self.weight = weight
        self.isRevoked = isRevoked
        self.revokedReason = revokedReason
        self.issuedAt = issuedAt
        self.expiresAt = expiresAt
    }
}
