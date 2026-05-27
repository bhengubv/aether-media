package vault

import java.time.Instant
import java.util.UUID

data class VaultHealth(
    val manifestId: UUID,
    val totalShards: Int,
    val availableShards: Int,
    val parityShards: Int,
    val availableParityShards: Int,
    val minShardsForRecovery: Int,
    val replicationFactor: Int,
    val degradedNodes: List<String> = emptyList(),
    val lastCheckedAt: Instant = Instant.now()
) {
    val isRecoverable: Boolean
        get() = availableShards >= minShardsForRecovery

    val isHealthy: Boolean
        get() = availableShards == totalShards && availableParityShards == parityShards

    val isDegraded: Boolean
        get() = isRecoverable && !isHealthy

    val healthPercent: Double
        get() = if (totalShards == 0) 0.0
                else (availableShards.toDouble() / totalShards.toDouble()) * 100.0
}
