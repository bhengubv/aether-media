package vault

import java.util.UUID

interface IVaultService {
    suspend fun store(ownerId: UUID, name: String, data: ByteArray, tags: List<String> = emptyList()): VaultManifest
    suspend fun recover(manifestId: UUID, requesterId: UUID): ByteArray
    suspend fun health(manifestId: UUID): VaultHealth
    suspend fun delete(manifestId: UUID, requesterId: UUID): Boolean
    suspend fun listManifests(ownerId: UUID, limit: Int = 50, offset: Int = 0): List<VaultManifest>
    suspend fun replicateShard(shardId: UUID, targetNodeId: String): VaultShard
    suspend fun verifyShard(shardId: UUID): Boolean
    suspend fun getManifest(manifestId: UUID): VaultManifest?
    suspend fun getShard(shardId: UUID): VaultShard?
}

class VaultServiceImpl : IVaultService {

    override suspend fun store(ownerId: UUID, name: String, data: ByteArray, tags: List<String>): VaultManifest {
        TODO("not implemented")
    }

    override suspend fun recover(manifestId: UUID, requesterId: UUID): ByteArray {
        TODO("not implemented")
    }

    override suspend fun health(manifestId: UUID): VaultHealth {
        TODO("not implemented")
    }

    override suspend fun delete(manifestId: UUID, requesterId: UUID): Boolean {
        TODO("not implemented")
    }

    override suspend fun listManifests(ownerId: UUID, limit: Int, offset: Int): List<VaultManifest> {
        TODO("not implemented")
    }

    override suspend fun replicateShard(shardId: UUID, targetNodeId: String): VaultShard {
        TODO("not implemented")
    }

    override suspend fun verifyShard(shardId: UUID): Boolean {
        TODO("not implemented")
    }

    override suspend fun getManifest(manifestId: UUID): VaultManifest? {
        TODO("not implemented")
    }

    override suspend fun getShard(shardId: UUID): VaultShard? {
        TODO("not implemented")
    }
}
