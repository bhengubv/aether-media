// SPDX-License-Identifier: MIT

using System.Text.Json;
using AetherNet.Media.Core.Models;
using AetherNet.Media.LocalLibrary.Interfaces;
using AetherNet.Media.LocalLibrary.Models;
using Microsoft.Extensions.Logging;

namespace AetherNet.Media.LocalLibrary;

/// <summary>
/// JSON-backed implementation of <see cref="ICollectionService"/>.
///
/// All collections are stored in a single file:
/// <c>%APPDATA%/aether-media/collections.json</c> (or the XDG/macOS equivalent).
/// Writes are serialised through a <see cref="SemaphoreSlim"/> so concurrent saves
/// from different ViewModels never corrupt the file.
/// </summary>
public class CollectionService : ICollectionService
{
    private readonly string _filePath;
    private readonly ILogger<CollectionService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented          = true,
        PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public CollectionService(ILogger<CollectionService> logger)
        : this(logger, dataDirectory: null) { }

    /// <summary>
    /// Constructor that accepts an explicit data directory.
    /// Used in tests to store data in a temp folder instead of AppData.
    /// </summary>
    protected CollectionService(ILogger<CollectionService> logger, string? dataDirectory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var dir = dataDirectory
                  ?? Path.Combine(
                         Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "aether-media");

        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "collections.json");
    }

    // ── ICollectionService ─────────────────────────────────────────────────

    public async Task<IReadOnlyList<MediaCollection>> GetAllAsync(CancellationToken ct = default)
    {
        var store = await LoadAsync(ct).ConfigureAwait(false);
        return store.Collections
                    .OrderByDescending(c => c.UpdatedAt)
                    .ToList();
    }

    public async Task<MediaCollection?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var store = await LoadAsync(ct).ConfigureAwait(false);
        return store.Collections.FirstOrDefault(c => c.Id == id);
    }

    public async Task<MediaCollection> CreateAsync(
        string name,
        CollectionType type,
        SmartCollectionFilter? filter = null,
        CancellationToken ct = default)
    {
        var collection = new MediaCollection
        {
            Name   = name,
            Type   = type,
            Filter = filter
        };

        await ModifyAsync(store => store.Collections.Add(collection), ct)
            .ConfigureAwait(false);

        return collection;
    }

    public async Task UpdateAsync(MediaCollection collection, CancellationToken ct = default)
    {
        collection.UpdatedAt = DateTime.UtcNow;

        await ModifyAsync(store =>
        {
            var index = store.Collections.FindIndex(c => c.Id == collection.Id);
            if (index >= 0)
                store.Collections[index] = collection;
        }, ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await ModifyAsync(store =>
            store.Collections.RemoveAll(c => c.Id == id), ct)
            .ConfigureAwait(false);
    }

    public async Task AddContentAsync(string collectionId, string contentHash,
        CancellationToken ct = default)
    {
        await ModifyAsync(store =>
        {
            var col = store.Collections.FirstOrDefault(c => c.Id == collectionId);
            if (col is null || col.Type != CollectionType.Manual)
                return;

            if (!col.ContentHashes.Contains(contentHash))
                col.ContentHashes.Add(contentHash);

            col.UpdatedAt = DateTime.UtcNow;
        }, ct).ConfigureAwait(false);
    }

    public async Task RemoveContentAsync(string collectionId, string contentHash,
        CancellationToken ct = default)
    {
        await ModifyAsync(store =>
        {
            var col = store.Collections.FirstOrDefault(c => c.Id == collectionId);
            if (col is null)
                return;

            col.ContentHashes.Remove(contentHash);
            col.UpdatedAt = DateTime.UtcNow;
        }, ct).ConfigureAwait(false);
    }

    public IReadOnlyList<MediaContent> EvaluateFilter(
        SmartCollectionFilter filter,
        IEnumerable<MediaContent> catalogue)
    {
        var results = catalogue.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filter.Genre))
            results = results.Where(c =>
                c.Tags.Any(t => t.Equals(filter.Genre, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(filter.Artist))
            results = results.Where(c =>
                c.CreatorUhid.Contains(filter.Artist, StringComparison.OrdinalIgnoreCase));

        if (filter.YearFrom.HasValue)
            results = results.Where(c =>
                DateTimeOffset.FromUnixTimeMilliseconds(c.CreatedAtMs).Year >= filter.YearFrom.Value);

        if (filter.YearTo.HasValue)
            results = results.Where(c =>
                DateTimeOffset.FromUnixTimeMilliseconds(c.CreatedAtMs).Year <= filter.YearTo.Value);

        if (filter.MaxDurationMs.HasValue)
            results = results.Where(c => c.DurationMs <= filter.MaxDurationMs.Value);

        if (filter.RequiredTags.Length > 0)
            results = results.Where(c =>
                filter.RequiredTags.All(rt =>
                    c.Tags.Any(t => t.Equals(rt, StringComparison.OrdinalIgnoreCase))));

        return results.ToList();
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task<CollectionStore> LoadAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
            return new CollectionStore();

        try
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await using var stream = File.OpenRead(_filePath);
                return await JsonSerializer.DeserializeAsync<CollectionStore>(
                    stream, JsonOptions, ct).ConfigureAwait(false)
                    ?? new CollectionStore();
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CollectionService: failed to load {Path}", _filePath);
            return new CollectionStore();
        }
    }

    private async Task ModifyAsync(Action<CollectionStore> mutate, CancellationToken ct)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            CollectionStore store;

            if (File.Exists(_filePath))
            {
                await using var stream = File.OpenRead(_filePath);
                store = await JsonSerializer.DeserializeAsync<CollectionStore>(
                    stream, JsonOptions, ct).ConfigureAwait(false)
                    ?? new CollectionStore();
            }
            else
            {
                store = new CollectionStore();
            }

            mutate(store);

            var json    = JsonSerializer.Serialize(store, JsonOptions);
            var tmpPath = _filePath + ".tmp";
            await File.WriteAllTextAsync(tmpPath, json, ct).ConfigureAwait(false);
            File.Move(tmpPath, _filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CollectionService: failed to save {Path}", _filePath);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    // ── Store model ────────────────────────────────────────────────────────

    private sealed class CollectionStore
    {
        public List<MediaCollection> Collections { get; set; } = [];
    }
}
