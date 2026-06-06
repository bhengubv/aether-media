// SPDX-License-Identifier: MIT
using AetherNet.Forge.Core;

namespace AetherNet.Forge.Tests;

/// <summary>
/// Tests that exercise the cache-hit and cache-miss paths in the
/// <see cref="InMemoryForgeService"/> test double.
/// </summary>
public sealed class ForgeEntryTests
{
    // ── In-memory test double ─────────────────────────────────────────────

    /// <summary>
    /// Minimal in-memory implementation of <see cref="IForgeService"/> used
    /// only for unit tests.
    /// </summary>
    private sealed class InMemoryForgeService : IForgeService
    {
        private readonly Dictionary<string, (ForgeEntry Entry, byte[] Bytes)> _cache = new();

        public IObservable<ForgeEntry> NewEntryAnnounced => NullObservable.Instance;

        // Minimal no-op IObservable so tests compile without System.Reactive.
        private sealed class NullObservable : IObservable<ForgeEntry>
        {
            public static readonly NullObservable Instance = new();
            public IDisposable Subscribe(IObserver<ForgeEntry> observer) =>
                NullDisposable.Instance;
        }

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose() { }
        }

        public Task<ForgeEntry?> QueryAsync(string packageId, CancellationToken ct = default)
        {
            var found = _cache.Values
                              .Where(x => x.Entry.PackageId == packageId)
                              .Select(x => (ForgeEntry?)x.Entry)
                              .FirstOrDefault();
            return Task.FromResult(found);
        }

        public async Task<ForgeEntry> CacheAsync(
            string packageId,
            Stream content,
            string contentHash,
            CancellationToken ct = default)
        {
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, ct).ConfigureAwait(false);
            var bytes = ms.ToArray();

            var entry = new ForgeEntry(
                ContentHash:   contentHash,
                PackageId:     packageId,
                FetchedAtUtc:  DateTime.UtcNow,
                SizeBytes:     bytes.Length,
                DownloadCount: 0);

            _cache[contentHash] = (entry, bytes);
            return entry;
        }

        public Task<Stream?> FetchAsync(string contentHash, CancellationToken ct = default)
        {
            if (_cache.TryGetValue(contentHash, out var tuple))
                return Task.FromResult<Stream?>(new MemoryStream(tuple.Bytes, writable: false));

            return Task.FromResult<Stream?>(null);
        }

        public Task<ForgeStats> GetStatsAsync(CancellationToken ct = default) =>
            Task.FromResult(new ForgeStats(
                TotalBytesSaved:  _cache.Values.Sum(x => x.Entry.SizeBytes),
                TotalPeersServed: 0,
                CatalogueSize:    _cache.Count,
                TopPackages:      _cache.Values
                                        .Select(x => x.Entry)
                                        .OrderByDescending(e => e.DownloadCount)
                                        .Take(5)
                                        .ToList()
                                        .AsReadOnly()));
    }

    // ── Cache miss path ────────────────────────────────────────────────────

    [Fact]
    public async Task Query_CacheMiss_ReturnsNull()
    {
        var svc = new InMemoryForgeService();

        var result = await svc.QueryAsync("npm:lodash@4.17.21");

        Assert.Null(result);
    }

    [Fact]
    public async Task Fetch_CacheMiss_ReturnsNull()
    {
        var svc = new InMemoryForgeService();

        var stream = await svc.FetchAsync("nonexistent_hash");

        Assert.Null(stream);
    }

    // ── Cache hit path ─────────────────────────────────────────────────────

    [Fact]
    public async Task CacheAndFetch_RoundTrip_PreservesByteCount()
    {
        var svc     = new InMemoryForgeService();
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        const string hash      = "aabbcc";
        const string packageId = "npm:lodash@4.17.21";

        var entry = await svc.CacheAsync(packageId, new MemoryStream(payload), hash);

        Assert.Equal(hash,      entry.ContentHash);
        Assert.Equal(packageId, entry.PackageId);
        Assert.Equal(5L,        entry.SizeBytes);

        var fetched = await svc.FetchAsync(hash);

        Assert.NotNull(fetched);
        using var ms = new MemoryStream();
        await fetched!.CopyToAsync(ms);
        Assert.Equal(payload, ms.ToArray());
    }

    [Fact]
    public async Task Query_AfterCache_ReturnsEntry()
    {
        var svc     = new InMemoryForgeService();
        var payload = new byte[] { 42 };
        const string hash      = "deadbeef";
        const string packageId = "cargo:serde@1.0.193";

        await svc.CacheAsync(packageId, new MemoryStream(payload), hash);

        var entry = await svc.QueryAsync(packageId);

        Assert.NotNull(entry);
        Assert.Equal(hash,      entry!.ContentHash);
        Assert.Equal(packageId, entry.PackageId);
    }

    [Fact]
    public async Task GetStats_ReflectsCachedEntries()
    {
        var svc = new InMemoryForgeService();
        await svc.CacheAsync("npm:a@1.0.0", new MemoryStream(new byte[100]), "hash1");
        await svc.CacheAsync("npm:b@1.0.0", new MemoryStream(new byte[200]), "hash2");

        var stats = await svc.GetStatsAsync();

        Assert.Equal(2,   stats.CatalogueSize);
        Assert.Equal(300L, stats.TotalBytesSaved);
    }
}
