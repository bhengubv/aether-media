using System.Collections.Concurrent;
using AetherMedia.Core.Models;

namespace AetherMedia.Core;

/// <summary>
/// Thread-safe, in-memory implementation of <see cref="IMediaLibrary"/> backed
/// by a <see cref="ConcurrentDictionary{TKey,TValue}"/>.
///
/// All operations complete synchronously under the hood but return
/// <see cref="Task"/> so that callers are fully decoupled from the
/// implementation strategy (e.g. swapping in a SQLite or remote store later
/// requires no changes to call sites).
///
/// Events are raised synchronously on the calling thread immediately after
/// each mutation.  Subscribers must not perform long-running work on the
/// event handler — offload to a <see cref="Task"/> or a background queue if
/// needed.
/// </summary>
public sealed class InMemoryMediaLibrary : IMediaLibrary
{
    // ── Storage ────────────────────────────────────────────────────────────

    // Key: ContentHash (SHA-256 hex, lowercase)
    private readonly ConcurrentDictionary<string, MediaContent> _store =
        new(StringComparer.OrdinalIgnoreCase);

    // ── Events ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public event EventHandler<MediaContent>? ContentAdded;

    /// <inheritdoc/>
    public event EventHandler<string>? ContentRemoved;

    // ── Helpers ────────────────────────────────────────────────────────────

    private static string NormaliseHash(string contentHash)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
            throw new ArgumentException("ContentHash must not be empty.", nameof(contentHash));
        return contentHash.Trim().ToLowerInvariant();
    }

    private void OnContentAdded(MediaContent content) =>
        ContentAdded?.Invoke(this, content);

    private void OnContentRemoved(string contentHash) =>
        ContentRemoved?.Invoke(this, contentHash);

    // ── Mutations ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// If an item with the same <see cref="MediaContent.ContentHash"/> already
    /// exists it is replaced (idempotent upsert).  <see cref="ContentAdded"/>
    /// is raised in both cases.
    /// </remarks>
    public Task AddAsync(MediaContent content, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(content);
        var key = NormaliseHash(content.ContentHash);

        _store[key] = content;
        OnContentAdded(content);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// No-ops gracefully when <paramref name="contentHash"/> is not present in
    /// the store.  <see cref="ContentRemoved"/> is only raised when an item
    /// was actually deleted.
    /// </remarks>
    public Task RemoveAsync(string contentHash, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = NormaliseHash(contentHash);

        if (_store.TryRemove(key, out _))
            OnContentRemoved(key);

        return Task.CompletedTask;
    }

    // ── Queries ────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<MediaContent?> GetAsync(string contentHash, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var key = NormaliseHash(contentHash);
        _store.TryGetValue(key, out var content);
        return Task.FromResult<MediaContent?>(content);
    }

    /// <inheritdoc/>
    /// <remarks>Results are ordered by <see cref="MediaContent.CreatedAtMs"/> descending.</remarks>
    public Task<IReadOnlyList<MediaContent>> GetAllAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<MediaContent> result = _store.Values
            .OrderByDescending(c => c.CreatedAtMs)
            .ToList();

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Performs a case-insensitive substring search over
    /// <see cref="MediaContent.Title"/> and each element of
    /// <see cref="MediaContent.Tags"/>.  A content item matches when
    /// <em>any</em> of its searchable fields contains
    /// <paramref name="query"/> as a substring.  Results are ordered by
    /// creation date descending.
    ///
    /// An empty or whitespace-only <paramref name="query"/> returns all items
    /// (equivalent to <see cref="GetAllAsync"/>).
    /// </remarks>
    public Task<IReadOnlyList<MediaContent>> SearchAsync(string query, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(query))
            return GetAllAsync(ct);

        var normalised = query.Trim();

        IReadOnlyList<MediaContent> result = _store.Values
            .Where(c => MatchesQuery(c, normalised))
            .OrderByDescending(c => c.CreatedAtMs)
            .ToList();

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    /// <remarks>Results are ordered by <see cref="MediaContent.CreatedAtMs"/> descending.</remarks>
    public Task<IReadOnlyList<MediaContent>> GetByCreatorAsync(
        string creatorUhid,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(creatorUhid))
            throw new ArgumentException("CreatorUhid must not be empty.", nameof(creatorUhid));

        IReadOnlyList<MediaContent> result = _store.Values
            .Where(c => string.Equals(c.CreatorUhid, creatorUhid, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.CreatedAtMs)
            .ToList();

        return Task.FromResult(result);
    }

    /// <inheritdoc/>
    public Task<int> CountAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_store.Count);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when <paramref name="content"/>'s title or any of
    /// its tags contains <paramref name="query"/> (case-insensitive substring).
    /// </summary>
    private static bool MatchesQuery(MediaContent content, string query)
    {
        if (content.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var tag in content.Tags)
        {
            if (tag.Contains(query, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
