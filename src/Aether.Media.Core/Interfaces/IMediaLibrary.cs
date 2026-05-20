using Aether.Media.Core.Models;

namespace Aether.Media.Core;

/// <summary>
/// A queryable, event-driven store of <see cref="MediaContent"/> items.
/// Implementations may back this with an in-memory dictionary, a local SQLite
/// database, or a remote Aether node — the caller does not need to know.
/// </summary>
public interface IMediaLibrary
{
    // ── Events ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised after a new item has been successfully persisted to the library.
    /// </summary>
    event EventHandler<MediaContent>? ContentAdded;

    /// <summary>
    /// Raised after an item has been removed from the library.  The argument
    /// is the <see cref="MediaContent.ContentHash"/> of the removed item.
    /// </summary>
    event EventHandler<string>? ContentRemoved;

    // ── Mutations ──────────────────────────────────────────────────────────

    /// <summary>
    /// Adds <paramref name="content"/> to the library.  If an item with the
    /// same <see cref="MediaContent.ContentHash"/> already exists it is
    /// silently replaced (idempotent upsert).
    /// </summary>
    Task AddAsync(MediaContent content, CancellationToken ct = default);

    /// <summary>
    /// Removes the item identified by <paramref name="contentHash"/> from the
    /// library.  No-ops gracefully if the hash does not exist.
    /// </summary>
    Task RemoveAsync(string contentHash, CancellationToken ct = default);

    // ── Queries ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the <see cref="MediaContent"/> whose
    /// <see cref="MediaContent.ContentHash"/> matches
    /// <paramref name="contentHash"/>, or <c>null</c> if not found.
    /// </summary>
    Task<MediaContent?> GetAsync(string contentHash, CancellationToken ct = default);

    /// <summary>Returns all content items in the library, ordered by creation date descending.</summary>
    Task<IReadOnlyList<MediaContent>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Performs a case-insensitive full-text search over
    /// <see cref="MediaContent.Title"/> and <see cref="MediaContent.Tags"/>.
    /// Returns matching items ordered by creation date descending.
    /// </summary>
    Task<IReadOnlyList<MediaContent>> SearchAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Returns all content items uploaded by <paramref name="creatorUhid"/>,
    /// ordered by creation date descending.
    /// </summary>
    Task<IReadOnlyList<MediaContent>> GetByCreatorAsync(string creatorUhid, CancellationToken ct = default);

    /// <summary>Returns the total number of items currently held in the library.</summary>
    Task<int> CountAsync(CancellationToken ct = default);
}
