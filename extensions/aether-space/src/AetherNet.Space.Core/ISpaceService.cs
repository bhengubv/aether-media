// SPDX-License-Identifier: MIT
namespace AetherNet.Space.Core;

/// <summary>
/// Provides operations for dropping, scanning, pinning, and deleting
/// geo-anchored <see cref="SpaceBreadcrumb"/> entries on the Aether mesh.
/// </summary>
public interface ISpaceService
{
    // ── Events ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when a breadcrumb is received from the mesh (local or remote drop).
    /// </summary>
    event EventHandler<SpaceBreadcrumb>? BreadcrumbReceived;

    /// <summary>
    /// Raised when a breadcrumb's TTL elapses and it is pruned from the local
    /// cache.
    /// </summary>
    event EventHandler<SpaceBreadcrumb>? BreadcrumbExpired;

    // ── Mutations ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates and broadcasts a new breadcrumb at <paramref name="geoHash"/>
    /// using the payload from <paramref name="content"/>.
    /// </summary>
    /// <param name="geoHash">Target geohash cell.</param>
    /// <param name="content">Payload stream for the breadcrumb content.</param>
    /// <param name="type">Semantic type of the breadcrumb.</param>
    /// <param name="ttlHours">Time-to-live in hours (must be &gt; 0).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created <see cref="SpaceBreadcrumb"/>.</returns>
    Task<SpaceBreadcrumb> DropAsync(
        GeoHash geoHash,
        Stream content,
        BreadcrumbType type,
        int ttlHours,
        CancellationToken ct = default);

    /// <summary>
    /// Pins a pre-existing <paramref name="breadcrumb"/> to the local node's
    /// cache without re-broadcasting it.
    /// </summary>
    /// <param name="breadcrumb">Breadcrumb to pin.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PinAsync(SpaceBreadcrumb breadcrumb, CancellationToken ct = default);

    /// <summary>
    /// Deletes a breadcrumb from the local cache and broadcasts a retract
    /// message to neighbouring nodes.
    /// </summary>
    /// <param name="breadcrumb">Breadcrumb to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(SpaceBreadcrumb breadcrumb, CancellationToken ct = default);

    // ── Queries ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all live (non-expired) breadcrumbs within
    /// <paramref name="radiusCells"/> geohash cells of <paramref name="geoHash"/>.
    /// </summary>
    /// <param name="geoHash">Centre cell to scan from.</param>
    /// <param name="radiusCells">Number of cells to expand outward (default 3).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<SpaceBreadcrumb>> ScanAsync(
        GeoHash geoHash,
        int radiusCells = 3,
        CancellationToken ct = default);
}
