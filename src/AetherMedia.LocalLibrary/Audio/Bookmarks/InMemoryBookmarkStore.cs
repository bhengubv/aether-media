// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Bookmarks;

/// <summary>
/// Thread-safe in-memory <see cref="IBookmarkStore"/>. Useful for tests and
/// as the default until a persistent (LiteDB / SQLite) store is wired up by
/// the host shell.
/// </summary>
public sealed class InMemoryBookmarkStore : IBookmarkStore
{
    private readonly object _gate = new();
    private readonly List<Bookmark> _items = [];

    /// <inheritdoc/>
    public Task AddAsync(Bookmark bookmark, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(bookmark);
        var stamped = bookmark.CreatedAtUtc == default
            ? bookmark with { CreatedAtUtc = DateTimeOffset.UtcNow }
            : bookmark;
        lock (_gate) _items.Add(stamped);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Bookmark>> ListAsync(string? filePath = null, CancellationToken ct = default)
    {
        lock (_gate)
        {
            IEnumerable<Bookmark> q = _items;
            if (!string.IsNullOrEmpty(filePath))
                q = q.Where(b => string.Equals(b.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<IReadOnlyList<Bookmark>>(q.OrderByDescending(b => b.CreatedAtUtc).ToList());
        }
    }

    /// <inheritdoc/>
    public Task<Bookmark?> ResumeFor(string filePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        lock (_gate)
        {
            var newest = _items
                .Where(b => string.Equals(b.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(b => b.CreatedAtUtc)
                .FirstOrDefault();
            return Task.FromResult(newest);
        }
    }

    /// <inheritdoc/>
    public Task RemoveAsync(Bookmark bookmark, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(bookmark);
        lock (_gate) _items.Remove(bookmark);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ClearAsync(CancellationToken ct = default)
    {
        lock (_gate) _items.Clear();
        return Task.CompletedTask;
    }
}
