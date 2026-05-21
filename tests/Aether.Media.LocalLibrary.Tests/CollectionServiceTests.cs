// SPDX-License-Identifier: MIT

using Aether.Media.Core.Models;
using Aether.Media.LocalLibrary;
using Aether.Media.LocalLibrary.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aether.Media.LocalLibrary.Tests;

/// <summary>
/// All tests use an isolated <see cref="CollectionService"/> pointed at a temp directory
/// so they never touch the real AppData folder and run safely in parallel.
/// </summary>
public sealed class CollectionServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IsolatedCollectionService _service;

    public CollectionServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _service = new IsolatedCollectionService(
            NullLogger<CollectionService>.Instance, _tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Create ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_AddsCollection()
    {
        var col = await _service.CreateAsync("My Playlist", CollectionType.Manual);

        Assert.NotNull(col);
        Assert.Equal("My Playlist",    col.Name);
        Assert.Equal(CollectionType.Manual, col.Type);
        Assert.False(string.IsNullOrEmpty(col.Id));
    }

    [Fact]
    public async Task CreateAsync_PersistsAcrossInstances()
    {
        await _service.CreateAsync("Persisted", CollectionType.Manual);

        // Spin up a second service on the same directory
        var second = new IsolatedCollectionService(
            NullLogger<CollectionService>.Instance, _tempDir);

        var all = await second.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("Persisted", all[0].Name);
    }

    // ── GetAll ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoCollections()
    {
        var all = await _service.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMostRecentFirst()
    {
        await _service.CreateAsync("A", CollectionType.Manual);
        await Task.Delay(5);  // ensure UpdatedAt differs
        await _service.CreateAsync("B", CollectionType.Manual);

        var all = await _service.GetAllAsync();
        Assert.Equal(2,   all.Count);
        Assert.Equal("B", all[0].Name);
    }

    // ── Update ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ChangesName()
    {
        var col = await _service.CreateAsync("Old Name", CollectionType.Manual);
        col.Name = "New Name";
        await _service.UpdateAsync(col);

        var fetched = await _service.GetByIdAsync(col.Id);
        Assert.NotNull(fetched);
        Assert.Equal("New Name", fetched!.Name);
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesCollection()
    {
        var col = await _service.CreateAsync("Temp", CollectionType.Manual);
        await _service.DeleteAsync(col.Id);

        var all = await _service.GetAllAsync();
        Assert.Empty(all);
    }

    // ── AddContent / RemoveContent ─────────────────────────────────────────

    [Fact]
    public async Task AddContentAsync_AppendsHash()
    {
        var col = await _service.CreateAsync("Playlist", CollectionType.Manual);
        await _service.AddContentAsync(col.Id, "abc123");
        await _service.AddContentAsync(col.Id, "def456");

        var fetched = await _service.GetByIdAsync(col.Id);
        Assert.NotNull(fetched);
        Assert.Equal(["abc123", "def456"], fetched!.ContentHashes);
    }

    [Fact]
    public async Task AddContentAsync_DoesNotAddDuplicateHash()
    {
        var col = await _service.CreateAsync("Playlist", CollectionType.Manual);
        await _service.AddContentAsync(col.Id, "abc123");
        await _service.AddContentAsync(col.Id, "abc123"); // duplicate

        var fetched = await _service.GetByIdAsync(col.Id);
        Assert.NotNull(fetched);
        Assert.Single(fetched!.ContentHashes);
    }

    [Fact]
    public async Task RemoveContentAsync_RemovesHash()
    {
        var col = await _service.CreateAsync("Playlist", CollectionType.Manual);
        await _service.AddContentAsync(col.Id, "abc123");
        await _service.RemoveContentAsync(col.Id, "abc123");

        var fetched = await _service.GetByIdAsync(col.Id);
        Assert.NotNull(fetched);
        Assert.Empty(fetched!.ContentHashes);
    }

    // ── EvaluateFilter ─────────────────────────────────────────────────────

    [Fact]
    public void EvaluateFilter_GenreMatch_ReturnsMatchingItems()
    {
        var catalogue = new[]
        {
            MakeContent("hash1", tags: ["Rock"]),
            MakeContent("hash2", tags: ["Jazz"]),
            MakeContent("hash3", tags: ["Rock", "Blues"])
        };

        var filter  = new SmartCollectionFilter { Genre = "rock" };
        var results = _service.EvaluateFilter(filter, catalogue);

        Assert.Equal(2, results.Count);
        Assert.All(results, c => Assert.Contains("Rock",
            c.Tags, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void EvaluateFilter_MaxDuration_FiltersLongContent()
    {
        var catalogue = new[]
        {
            MakeContent("h1", durationMs: 60_000),
            MakeContent("h2", durationMs: 180_000),
            MakeContent("h3", durationMs: 240_000)
        };

        var filter  = new SmartCollectionFilter { MaxDurationMs = 120_000 };
        var results = _service.EvaluateFilter(filter, catalogue);

        Assert.Single(results);
        Assert.Equal("h1", results[0].ContentHash);
    }

    [Fact]
    public void EvaluateFilter_RequiredTags_RequiresAll()
    {
        var catalogue = new[]
        {
            MakeContent("h1", tags: ["Chill", "Lofi"]),
            MakeContent("h2", tags: ["Chill"]),
            MakeContent("h3", tags: ["Lofi"])
        };

        var filter  = new SmartCollectionFilter { RequiredTags = ["Chill", "Lofi"] };
        var results = _service.EvaluateFilter(filter, catalogue);

        Assert.Single(results);
        Assert.Equal("h1", results[0].ContentHash);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static MediaContent MakeContent(
        string   hash,
        string[] tags       = null!,
        long     durationMs = 60_000) =>
        new(
            ContentHash   : hash,
            Title         : hash,
            DurationMs    : durationMs,
            Codec         : "aac",
            ContentType   : "audio/aac",
            CreatorUhid   : "local",
            SizeBytes     : 1024,
            CreatedAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ThumbnailHash : null,
            Tags          : tags ?? []);

    /// <summary>
    /// Subclass that stores in the test's temp directory instead of AppData.
    /// </summary>
    private sealed class IsolatedCollectionService : CollectionService
    {
        public IsolatedCollectionService(
            Microsoft.Extensions.Logging.ILogger<CollectionService> logger,
            string tempDir)
            : base(logger, tempDir) { }
    }
}
