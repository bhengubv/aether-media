// SPDX-License-Identifier: MIT

namespace Aether.Media.Content;

/// <summary>
/// Thread-safe LRU content cache backed by a doubly-linked list and a dictionary
/// for O(1) get, set, and evict operations. When the total stored bytes exceed
/// <see cref="CapacityBytes"/> the least-recently-used entries are evicted until
/// the cache fits within the limit.
/// </summary>
public sealed class LruContentCache : IContentCache
{
    /// <summary>Default capacity: 500 MiB.</summary>
    public const long DefaultCapacityBytes = 500L * 1024 * 1024;

    private sealed class CacheEntry
    {
        public string Key { get; }
        public byte[] Data { get; set; }

        public CacheEntry(string key, byte[] data)
        {
            Key = key;
            Data = data;
        }
    }

    private readonly long _capacityBytes;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly LinkedList<CacheEntry> _lruList = new();
    private readonly Dictionary<string, LinkedListNode<CacheEntry>> _index
        = new(StringComparer.OrdinalIgnoreCase);

    private long _totalBytes;

    /// <param name="capacityBytes">
    /// Maximum number of bytes to hold before evicting LRU entries.
    /// Defaults to 500 MiB when zero or negative.
    /// </param>
    public LruContentCache(long capacityBytes = 0)
    {
        _capacityBytes = capacityBytes > 0 ? capacityBytes : DefaultCapacityBytes;
    }

    /// <inheritdoc/>
    public int Count
    {
        get
        {
            _lock.Wait();
            try { return _index.Count; }
            finally { _lock.Release(); }
        }
    }

    /// <inheritdoc/>
    public long TotalBytes
    {
        get
        {
            _lock.Wait();
            try { return _totalBytes; }
            finally { _lock.Release(); }
        }
    }

    /// <inheritdoc/>
    public bool TryGet(string contentHash, out byte[] data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);

        _lock.Wait();
        try
        {
            if (!_index.TryGetValue(contentHash, out var node))
            {
                data = Array.Empty<byte>();
                return false;
            }

            // Move to front (most recently used).
            _lruList.Remove(node);
            _lruList.AddFirst(node);

            data = node.Value.Data;
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public void Set(string contentHash, byte[] data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        ArgumentNullException.ThrowIfNull(data);

        _lock.Wait();
        try
        {
            // If the single entry is larger than the entire capacity, skip caching it.
            if (data.Length > _capacityBytes)
                return;

            if (_index.TryGetValue(contentHash, out var existing))
            {
                // Update in place: adjust byte accounting and promote to front.
                _totalBytes -= existing.Value.Data.Length;
                _totalBytes += data.Length;
                existing.Value.Data = data;
                _lruList.Remove(existing);
                _lruList.AddFirst(existing);
            }
            else
            {
                var entry = new CacheEntry(contentHash, data);
                var node = _lruList.AddFirst(entry);
                _index[contentHash] = node;
                _totalBytes += data.Length;
            }

            // Evict from the tail until we are within capacity.
            while (_totalBytes > _capacityBytes && _lruList.Last is not null)
            {
                var tail = _lruList.Last;
                _lruList.RemoveLast();
                _index.Remove(tail.Value.Key);
                _totalBytes -= tail.Value.Data.Length;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public void Evict(string contentHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);

        _lock.Wait();
        try
        {
            if (!_index.TryGetValue(contentHash, out var node))
                return;

            _lruList.Remove(node);
            _index.Remove(contentHash);
            _totalBytes -= node.Value.Data.Length;
        }
        finally
        {
            _lock.Release();
        }
    }
}
