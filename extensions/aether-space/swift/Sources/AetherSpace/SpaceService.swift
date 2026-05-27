import Foundation

public protocol SpaceServiceProtocol {
    func dropBreadcrumb(_ breadcrumb: SpaceBreadcrumb) async throws -> SpaceBreadcrumb
    func scan(geoHash: String, radiusKm: Double) async throws -> [SpaceBreadcrumb]
    func pinBreadcrumb(breadcrumbId: UUID, spaceId: UUID) async throws -> SpaceBreadcrumb
    func unpinBreadcrumb(breadcrumbId: UUID, spaceId: UUID) async throws -> SpaceBreadcrumb
    func deleteBreadcrumb(breadcrumbId: UUID, requesterId: UUID) async throws -> Bool
    func getById(_ breadcrumbId: UUID) async throws -> SpaceBreadcrumb?
    func listBySpace(_ spaceId: UUID, limit: Int, offset: Int) async throws -> [SpaceBreadcrumb]
    func react(breadcrumbId: UUID, userId: UUID, reaction: String) async throws -> Int
}

public class SpaceService: SpaceServiceProtocol {

    public init() {}

    public func dropBreadcrumb(_ breadcrumb: SpaceBreadcrumb) async throws -> SpaceBreadcrumb {
        fatalError("not implemented")
    }

    public func scan(geoHash: String, radiusKm: Double = 5.0) async throws -> [SpaceBreadcrumb] {
        fatalError("not implemented")
    }

    public func pinBreadcrumb(breadcrumbId: UUID, spaceId: UUID) async throws -> SpaceBreadcrumb {
        fatalError("not implemented")
    }

    public func unpinBreadcrumb(breadcrumbId: UUID, spaceId: UUID) async throws -> SpaceBreadcrumb {
        fatalError("not implemented")
    }

    public func deleteBreadcrumb(breadcrumbId: UUID, requesterId: UUID) async throws -> Bool {
        fatalError("not implemented")
    }

    public func getById(_ breadcrumbId: UUID) async throws -> SpaceBreadcrumb? {
        fatalError("not implemented")
    }

    public func listBySpace(_ spaceId: UUID, limit: Int = 50, offset: Int = 0) async throws -> [SpaceBreadcrumb] {
        fatalError("not implemented")
    }

    public func react(breadcrumbId: UUID, userId: UUID, reaction: String) async throws -> Int {
        fatalError("not implemented")
    }
}
