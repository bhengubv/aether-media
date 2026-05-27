package forge

import java.util.UUID

interface IForgeService {
    suspend fun query(packageId: String, ecosystem: String, version: String? = null): ForgeEntry?
    suspend fun cache(entry: ForgeEntry): ForgeEntry
    suspend fun fetch(packageId: String, ecosystem: String, version: String): ByteArray
    suspend fun stats(): ForgeStats
    suspend fun evict(entryId: UUID): Boolean
    suspend fun listByEcosystem(ecosystem: String, limit: Int = 50, offset: Int = 0): List<ForgeEntry>
    suspend fun search(query: String, ecosystem: String? = null): List<ForgeEntry>
    suspend fun sync(peerNodeId: String): Int
}

class ForgeServiceImpl : IForgeService {

    override suspend fun query(packageId: String, ecosystem: String, version: String?): ForgeEntry? {
        TODO("not implemented")
    }

    override suspend fun cache(entry: ForgeEntry): ForgeEntry {
        TODO("not implemented")
    }

    override suspend fun fetch(packageId: String, ecosystem: String, version: String): ByteArray {
        TODO("not implemented")
    }

    override suspend fun stats(): ForgeStats {
        TODO("not implemented")
    }

    override suspend fun evict(entryId: UUID): Boolean {
        TODO("not implemented")
    }

    override suspend fun listByEcosystem(ecosystem: String, limit: Int, offset: Int): List<ForgeEntry> {
        TODO("not implemented")
    }

    override suspend fun search(query: String, ecosystem: String?): List<ForgeEntry> {
        TODO("not implemented")
    }

    override suspend fun sync(peerNodeId: String): Int {
        TODO("not implemented")
    }
}
