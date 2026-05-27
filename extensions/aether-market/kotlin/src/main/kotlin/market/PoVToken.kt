package market

import java.time.Instant
import java.util.UUID

enum class PoVTransport {
    MESH,
    BLUETOOTH,
    NFC,
    QR_CODE,
    DIRECT_LINK
}

data class PoVToken(
    val id: UUID = UUID.randomUUID(),
    val issuerId: UUID,
    val subjectId: UUID,
    val context: String,
    val claim: String,
    val evidence: String = "",
    val transport: PoVTransport = PoVTransport.MESH,
    val signature: String,
    val publicKeyHint: String = "",
    val weight: Double = 1.0,
    val isRevoked: Boolean = false,
    val revokedReason: String = "",
    val issuedAt: Instant = Instant.now(),
    val expiresAt: Instant? = null
)
