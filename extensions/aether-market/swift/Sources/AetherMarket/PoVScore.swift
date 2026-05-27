import Foundation

public struct PoVScore: Codable, Equatable {
    public let subjectId: UUID
    public var overallScore: Double
    public var tradeScore: Double
    public var reliabilityScore: Double
    public var responseScore: Double
    public var disputeScore: Double
    public var tokenCount: Int
    public var positiveTokens: Int
    public var negativeTokens: Int
    public var neutralTokens: Int
    public var successfulTrades: Int
    public var failedTrades: Int
    public var disputesRaised: Int
    public var disputesResolved: Int
    public var level: String
    public let lastUpdated: Date

    public var trustPercent: Double {
        min(max(overallScore, 0.0), 100.0)
    }

    public var completionRate: Double {
        let total = successfulTrades + failedTrades
        guard total > 0 else { return 0.0 }
        return (Double(successfulTrades) / Double(total)) * 100.0
    }

    public init(
        subjectId: UUID,
        overallScore: Double,
        tradeScore: Double = 0.0,
        reliabilityScore: Double = 0.0,
        responseScore: Double = 0.0,
        disputeScore: Double = 0.0,
        tokenCount: Int = 0,
        positiveTokens: Int = 0,
        negativeTokens: Int = 0,
        neutralTokens: Int = 0,
        successfulTrades: Int = 0,
        failedTrades: Int = 0,
        disputesRaised: Int = 0,
        disputesResolved: Int = 0,
        level: String = "UNRANKED",
        lastUpdated: Date = Date()
    ) {
        self.subjectId = subjectId
        self.overallScore = overallScore
        self.tradeScore = tradeScore
        self.reliabilityScore = reliabilityScore
        self.responseScore = responseScore
        self.disputeScore = disputeScore
        self.tokenCount = tokenCount
        self.positiveTokens = positiveTokens
        self.negativeTokens = negativeTokens
        self.neutralTokens = neutralTokens
        self.successfulTrades = successfulTrades
        self.failedTrades = failedTrades
        self.disputesRaised = disputesRaised
        self.disputesResolved = disputesResolved
        self.level = level
        self.lastUpdated = lastUpdated
    }
}
