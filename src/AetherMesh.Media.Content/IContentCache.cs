// SPDX-License-Identifier: MIT

namespace AetherMesh.Media.Content;

/// <summary>
/// In-process cache for raw content bytes indexed by content hash.
/// Implementations must be thread-safe.
/// </summary>
public interface IContentCache
{
    /// <summary>Attempt to retrieve cached bytes. Returns <c>true</c> and sets <paramref name="data"/> on hit.</summary>
    bool TryGet(string contentHash, out byte[] data);

    /// <summary>Insert or replace the bytes stored under <paramref name="contentHash"/>.</summary>
    void Set(string contentHash, byte[] data);

    /// <summary>Remove an entry from the cache. No-op if the key is absent.</summary>
    void Evict(string contentHash);

    /// <summary>Number of entries currently held in the cache.</summary>
    int Count { get; }

    /// <summary>Total number of raw bytes currently held in the cache.</summary>
    long TotalBytes { get; }
}
