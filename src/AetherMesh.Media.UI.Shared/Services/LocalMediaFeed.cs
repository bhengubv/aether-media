// SPDX-License-Identifier: MIT

using AetherMesh.Media.Core;
using AetherMesh.Media.Core.Models;

namespace AetherMesh.Media.UI.Shared.Services;

/// <summary>
/// Cross-platform in-process implementation of <see cref="IMediaFeed"/>.
/// Stores items in memory; push-style updates via <see cref="Push"/>.
/// </summary>
public sealed class LocalMediaFeed : IMediaFeed
{
    private const int MaxItems = 200;

    private readonly List<MediaFeedItem> _items = [];
    private readonly Lock                _lock  = new();

    public event EventHandler<MediaFeedItem>? ItemAdded;

    public Task<IReadOnlyList<MediaFeedItem>> GetFeedAsync(
        int limit = 50, int offset = 0, CancellationToken ct = default)
    {
        if (limit  <= 0) limit  = 50;
        if (offset <  0) offset = 0;

        lock (_lock)
        {
            if (offset >= _items.Count)
                return Task.FromResult<IReadOnlyList<MediaFeedItem>>(Array.Empty<MediaFeedItem>());

            var take = Math.Min(limit, _items.Count - offset);
            return Task.FromResult<IReadOnlyList<MediaFeedItem>>(_items.GetRange(offset, take));
        }
    }

    public Task<IReadOnlyList<LiveStream>> GetNearbyLiveStreamsAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            var live = _items
                .Where(i => i.IsLive && i.StreamId.HasValue)
                .Select(i => new LiveStream(
                    StreamId: i.StreamId!.Value, Title: i.Content.Title,
                    CreatorUhid: i.Content.CreatorUhid, Codec: i.Content.Codec,
                    SegmentDurationMs: 2000, StartedAtMs: i.PublishedAtMs,
                    ViewerCount: i.WatchCount, IsActive: true, Tags: []))
                .ToList();
            return Task.FromResult<IReadOnlyList<LiveStream>>(live);
        }
    }

    public Task MarkWatchedAsync(string contentHash, long watchedMs, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contentHash) || watchedMs <= 0) return Task.CompletedTask;
        lock (_lock)
        {
            for (var i = 0; i < _items.Count; i++)
            {
                if (string.Equals(_items[i].Content.ContentHash, contentHash, StringComparison.OrdinalIgnoreCase))
                {
                    _items[i] = _items[i] with { WatchCount = _items[i].WatchCount + 1 };
                    break;
                }
            }
        }
        return Task.CompletedTask;
    }

    public Task RefreshAsync(CancellationToken ct = default) => Task.CompletedTask;

    /// <summary>Adds an item to the head of the feed and raises <see cref="ItemAdded"/>.</summary>
    public void Push(MediaFeedItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        lock (_lock)
        {
            if (_items.Any(x => string.Equals(x.Content.ContentHash, item.Content.ContentHash, StringComparison.OrdinalIgnoreCase)))
                return;
            _items.Insert(0, item);
            if (_items.Count > MaxItems) _items.RemoveAt(_items.Count - 1);
        }
        ItemAdded?.Invoke(this, item);
    }
}
