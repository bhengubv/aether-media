// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Library;

/// <summary>
/// Thread-safe in-memory <see cref="IPlayHistoryStore"/>. Persistent SQLite
/// or LiteDB stores can implement the same interface for production use.
/// </summary>
public sealed class InMemoryPlayHistoryStore : IPlayHistoryStore
{
    private readonly object _gate = new();
    private readonly List<PlayEvent> _events = [];

    /// <inheritdoc/>
    public Task RecordAsync(PlayEvent ev, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ev);
        lock (_gate) _events.Add(ev);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<PlayStatistics> GetAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        lock (_gate)
        {
            var matches = _events.Where(e => string.Equals(e.FilePath, filePath, StringComparison.OrdinalIgnoreCase)).ToList();
            var stats = new PlayStatistics(
                FilePath: filePath,
                PlayCount: matches.Count,
                TotalListenedMs: matches.Sum(e => e.ListenedMs),
                LastPlayedUtc: matches.Count == 0 ? null : matches.Max(e => e.PlayedAtUtc));
            return Task.FromResult(stats);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<PlayStatistics>> MostPlayedAsync(int limit, CancellationToken ct = default)
    {
        return Task.FromResult(AggregateOrdered((_, count) => -count, limit));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<PlayStatistics>> LeastPlayedAsync(int limit, CancellationToken ct = default)
    {
        return Task.FromResult(AggregateOrdered((_, count) => count, limit));
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<PlayStatistics>> RecentlyPlayedAsync(int limit, CancellationToken ct = default)
    {
        lock (_gate)
        {
            var grouped = _events
                .GroupBy(e => e.FilePath, StringComparer.OrdinalIgnoreCase)
                .Select(g => new PlayStatistics(
                    FilePath: g.Key,
                    PlayCount: g.Count(),
                    TotalListenedMs: g.Sum(e => e.ListenedMs),
                    LastPlayedUtc: g.Max(e => e.PlayedAtUtc)))
                .OrderByDescending(s => s.LastPlayedUtc)
                .Take(limit)
                .ToList();
            return Task.FromResult<IReadOnlyList<PlayStatistics>>(grouped);
        }
    }

    private IReadOnlyList<PlayStatistics> AggregateOrdered(Func<string, int, IComparable> orderKey, int limit)
    {
        lock (_gate)
        {
            return _events
                .GroupBy(e => e.FilePath, StringComparer.OrdinalIgnoreCase)
                .Select(g => new PlayStatistics(
                    FilePath: g.Key,
                    PlayCount: g.Count(),
                    TotalListenedMs: g.Sum(e => e.ListenedMs),
                    LastPlayedUtc: g.Max(e => e.PlayedAtUtc)))
                .Where(s => s.PlayCount > 0)
                .OrderBy(s => orderKey(s.FilePath, s.PlayCount))
                .Take(limit)
                .ToList();
        }
    }
}
