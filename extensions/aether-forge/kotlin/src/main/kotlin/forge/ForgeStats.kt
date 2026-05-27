package forge

import java.time.Instant

data class ForgeStats(
    val totalEntries: Long = 0L,
    val totalSizeBytes: Long = 0L,
    val totalDownloads: Long = 0L,
    val uniqueEcosystems: Int = 0,
    val uniquePackages: Long = 0L,
    val verifiedPackages: Long = 0L,
    val hitRate: Double = 0.0,
    val missRate: Double = 0.0,
    val averagePackageSizeBytes: Long = 0L,
    val peakDownloadsPerHour: Long = 0L,
    val activePeers: Int = 0,
    val lastUpdated: Instant = Instant.now(),
    val ecosystemBreakdown: Map<String, Long> = emptyMap()
)
