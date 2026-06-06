// SPDX-License-Identifier: MIT

using AetherMedia.Core.Models;
using AetherMedia.LocalLibrary.Models;

namespace AetherMedia.LocalLibrary.Interfaces;

/// <summary>
/// Manages manual playlists and smart collections.  State is persisted as JSON in the
/// platform application-data directory so it survives app restarts.
/// </summary>
public interface ICollectionService
{
    /// <summary>Returns all collections ordered by <see cref="MediaCollection.UpdatedAt"/> descending.</summary>
    Task<IReadOnlyList<MediaCollection>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns a single collection by ID, or <c>null</c> if not found.</summary>
    Task<MediaCollection?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>Creates and persists a new collection.</summary>
    Task<MediaCollection> CreateAsync(
        string name,
        CollectionType type,
        SmartCollectionFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>Saves changes to an existing collection.</summary>
    Task UpdateAsync(MediaCollection collection, CancellationToken ct = default);

    /// <summary>Permanently removes a collection.  Does not delete any media files.</summary>
    Task DeleteAsync(string id, CancellationToken ct = default);

    // ── Manual collection helpers ──────────────────────────────────────────────

    /// <summary>Appends <paramref name="contentHash"/> to the end of a manual collection.</summary>
    Task AddContentAsync(string collectionId, string contentHash, CancellationToken ct = default);

    /// <summary>Removes <paramref name="contentHash"/> from a manual collection.</summary>
    Task RemoveContentAsync(string collectionId, string contentHash, CancellationToken ct = default);

    // ── Smart collection evaluation ────────────────────────────────────────────

    /// <summary>
    /// Filters <paramref name="catalogue"/> against <paramref name="filter"/> and returns
    /// matching items.  This is a pure, synchronous, in-memory evaluation — no I/O.
    /// </summary>
    IReadOnlyList<MediaContent> EvaluateFilter(
        SmartCollectionFilter filter,
        IEnumerable<MediaContent> catalogue);
}
