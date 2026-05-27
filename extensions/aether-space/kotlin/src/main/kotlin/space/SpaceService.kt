package space

import java.util.UUID

interface ISpaceService {
    suspend fun dropBreadcrumb(breadcrumb: SpaceBreadcrumb): SpaceBreadcrumb
    suspend fun scan(geoHash: String, radiusKm: Double = 5.0): List<SpaceBreadcrumb>
    suspend fun pinBreadcrumb(breadcrumbId: UUID, spaceId: UUID): SpaceBreadcrumb
    suspend fun unpinBreadcrumb(breadcrumbId: UUID, spaceId: UUID): SpaceBreadcrumb
    suspend fun deleteBreadcrumb(breadcrumbId: UUID, requesterId: UUID): Boolean
    suspend fun getById(breadcrumbId: UUID): SpaceBreadcrumb?
    suspend fun listBySpace(spaceId: UUID, limit: Int = 50, offset: Int = 0): List<SpaceBreadcrumb>
    suspend fun react(breadcrumbId: UUID, userId: UUID, reaction: String): Int
}

class SpaceServiceImpl : ISpaceService {

    override suspend fun dropBreadcrumb(breadcrumb: SpaceBreadcrumb): SpaceBreadcrumb {
        TODO("not implemented")
    }

    override suspend fun scan(geoHash: String, radiusKm: Double): List<SpaceBreadcrumb> {
        TODO("not implemented")
    }

    override suspend fun pinBreadcrumb(breadcrumbId: UUID, spaceId: UUID): SpaceBreadcrumb {
        TODO("not implemented")
    }

    override suspend fun unpinBreadcrumb(breadcrumbId: UUID, spaceId: UUID): SpaceBreadcrumb {
        TODO("not implemented")
    }

    override suspend fun deleteBreadcrumb(breadcrumbId: UUID, requesterId: UUID): Boolean {
        TODO("not implemented")
    }

    override suspend fun getById(breadcrumbId: UUID): SpaceBreadcrumb? {
        TODO("not implemented")
    }

    override suspend fun listBySpace(spaceId: UUID, limit: Int, offset: Int): List<SpaceBreadcrumb> {
        TODO("not implemented")
    }

    override suspend fun react(breadcrumbId: UUID, userId: UUID, reaction: String): Int {
        TODO("not implemented")
    }
}
