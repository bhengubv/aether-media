package market

import java.util.UUID

interface IPoVService {
    suspend fun issueToken(token: PoVToken): PoVToken
    suspend fun revokeToken(tokenId: UUID, reason: String): Boolean
    suspend fun getScore(subjectId: UUID): PoVScore
    suspend fun getTokensFor(subjectId: UUID): List<PoVToken>
    suspend fun getTokensBy(issuerId: UUID): List<PoVToken>
    suspend fun verifyToken(tokenId: UUID): Boolean
    suspend fun syncTokens(peerNodeId: String): Int
}

class PoVServiceImpl : IPoVService {

    override suspend fun issueToken(token: PoVToken): PoVToken {
        TODO("not implemented")
    }

    override suspend fun revokeToken(tokenId: UUID, reason: String): Boolean {
        TODO("not implemented")
    }

    override suspend fun getScore(subjectId: UUID): PoVScore {
        TODO("not implemented")
    }

    override suspend fun getTokensFor(subjectId: UUID): List<PoVToken> {
        TODO("not implemented")
    }

    override suspend fun getTokensBy(issuerId: UUID): List<PoVToken> {
        TODO("not implemented")
    }

    override suspend fun verifyToken(tokenId: UUID): Boolean {
        TODO("not implemented")
    }

    override suspend fun syncTokens(peerNodeId: String): Int {
        TODO("not implemented")
    }
}
