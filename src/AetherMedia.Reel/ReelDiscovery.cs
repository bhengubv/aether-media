// SPDX-License-Identifier: MIT

using System.Text.Json;
using AetherMedia.Reel.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherMedia.Reel;

/// <summary>
/// JSON-backed Reel discovery and trending engine.
///
/// Two persistent files are maintained:
/// <list type="bullet">
///   <item><c>reel-index.json</c> — local index of known Reels (from self + peers).</item>
///   <item><c>reel-trending.json</c> — aggregate hashtag and sound counts.</item>
/// </list>
///
/// Trending counts are split into current window (last 24 h) and previous window
/// (prior 24 h) so that velocity (acceleration / deceleration) can be computed
/// without requiring a time-series database.
/// </summary>
public class ReelDiscovery : IReelDiscovery
{
    private readonly string _indexPath;
    private readonly string _trendingPath;
    private readonly ILogger<ReelDiscovery> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    // ── Constructors ──────────────────────────────────────────────────────────

    public ReelDiscovery(ILogger<ReelDiscovery>? logger = null)
        : this(null, logger) { }

    protected ReelDiscovery(string? dataDirectory, ILogger<ReelDiscovery>? logger = null)
    {
        var dir = dataDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "aether-media");
        Directory.CreateDirectory(dir);
        _indexPath    = Path.Combine(dir, "reel-index.json");
        _trendingPath = Path.Combine(dir, "reel-trending.json");
        _logger       = logger ?? NullLogger<ReelDiscovery>.Instance;
    }

    // ── IReelDiscovery ────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<TrendingHashtag>> GetTrendingHashtagsAsync(
        int count = 20,
        CancellationToken ct = default)
    {
        var trending = await LoadTrendingAsync(ct).ConfigureAwait(false);
        return trending.HashtagCurrent
            .Select(kv =>
            {
                var prev = trending.HashtagPrevious.GetValueOrDefault(kv.Key, 1L);
                return new TrendingHashtag(kv.Key, kv.Value, (float)kv.Value / Math.Max(prev, 1));
            })
            .OrderByDescending(t => t.Velocity)
            .ThenByDescending(t => t.Count24h)
            .Take(count)
            .ToList();
    }

    public async Task<IReadOnlyList<TrendingSound>> GetTrendingSoundsAsync(
        int count = 20,
        CancellationToken ct = default)
    {
        var trending = await LoadTrendingAsync(ct).ConfigureAwait(false);
        return trending.SoundCurrent
            .Select(kv =>
            {
                var prev = trending.SoundPrevious.GetValueOrDefault(kv.Key, 1L);
                var meta = trending.SoundMeta.GetValueOrDefault(kv.Key);
                return new TrendingSound(
                    kv.Key,
                    meta?.Title ?? kv.Key[..Math.Min(8, kv.Key.Length)],
                    meta?.ArtistName,
                    kv.Value,
                    (float)kv.Value / Math.Max(prev, 1));
            })
            .OrderByDescending(t => t.Velocity)
            .ThenByDescending(t => t.UseCount24h)
            .Take(count)
            .ToList();
    }

    public async Task<IReadOnlyList<Reel>> SearchAsync(
        string query,
        int    count = 20,
        CancellationToken ct = default)
    {
        var index = await LoadIndexAsync(ct).ConfigureAwait(false);

        // Empty query → return full index (used by GetForYouAsync candidate pool)
        if (string.IsNullOrWhiteSpace(query))
            return index.OrderByDescending(r => r.CreatedAtMs).Take(count).ToList();

        var q = query.Trim().ToLowerInvariant();

        return index
            .Where(r =>
                r.ContentHash.Contains(q, StringComparison.OrdinalIgnoreCase)    ||
                r.Title.Contains(q, StringComparison.OrdinalIgnoreCase)          ||
                r.CreatorUhid.Contains(q, StringComparison.OrdinalIgnoreCase)    ||
                r.Hashtags.Any(h => h.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(r => r.CreatedAtMs)
            .Take(count)
            .ToList();
    }

    public async Task AnnounceReelAsync(Reel reel, CancellationToken ct = default)
    {
        await IndexReelAsync(reel, ct).ConfigureAwait(false);

        // Increment local trending counters
        await ModifyTrendingAsync(trending =>
        {
            foreach (var tag in reel.Hashtags)
                trending.HashtagCurrent[tag] =
                    trending.HashtagCurrent.GetValueOrDefault(tag, 0L) + 1;

            if (reel.SoundHash is not null)
                trending.SoundCurrent[reel.SoundHash] =
                    trending.SoundCurrent.GetValueOrDefault(reel.SoundHash, 0L) + 1;
        }, ct).ConfigureAwait(false);
    }

    public async Task MergeGossipAsync(
        IReadOnlyDictionary<string, long> hashtagCounts,
        IReadOnlyDictionary<string, long> soundCounts,
        CancellationToken ct = default)
    {
        await ModifyTrendingAsync(trending =>
        {
            foreach (var (tag, count) in hashtagCounts)
                trending.HashtagCurrent[tag] =
                    Math.Max(trending.HashtagCurrent.GetValueOrDefault(tag, 0L), count);

            foreach (var (hash, count) in soundCounts)
                trending.SoundCurrent[hash] =
                    Math.Max(trending.SoundCurrent.GetValueOrDefault(hash, 0L), count);
        }, ct).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyDictionary<string, long> HashtagCounts,
                       IReadOnlyDictionary<string, long> SoundCounts)> BuildGossipPayloadAsync(
        CancellationToken ct = default)
    {
        var trending = await LoadTrendingAsync(ct).ConfigureAwait(false);
        return (trending.HashtagCurrent, trending.SoundCurrent);
    }

    public async Task IndexReelAsync(Reel reel, CancellationToken ct = default)
    {
        await ModifyIndexAsync(index =>
        {
            if (!index.Any(r => r.ContentHash == reel.ContentHash))
                index.Add(reel);
        }, ct).ConfigureAwait(false);
    }

    // ── Persistence helpers ───────────────────────────────────────────────────

    private async Task<List<Reel>> LoadIndexAsync(CancellationToken ct)
    {
        if (!File.Exists(_indexPath)) return [];
        try
        {
            var json = await File.ReadAllTextAsync(_indexPath, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<Reel>>(json, JsonOpts) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ReelDiscovery: failed to load reel index.");
            return [];
        }
    }

    private async Task ModifyIndexAsync(Action<List<Reel>> mutate, CancellationToken ct)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var index = await LoadIndexAsync(ct).ConfigureAwait(false);
            mutate(index);
            await WriteJsonAsync(_indexPath, index, ct).ConfigureAwait(false);
        }
        finally { _lock.Release(); }
    }

    private async Task<TrendingStore> LoadTrendingAsync(CancellationToken ct)
    {
        if (!File.Exists(_trendingPath)) return new TrendingStore();
        try
        {
            var json = await File.ReadAllTextAsync(_trendingPath, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<TrendingStore>(json, JsonOpts) ?? new TrendingStore();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ReelDiscovery: failed to load trending store.");
            return new TrendingStore();
        }
    }

    private async Task ModifyTrendingAsync(Action<TrendingStore> mutate, CancellationToken ct)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var store = await LoadTrendingAsync(ct).ConfigureAwait(false);
            store.MaybeRollWindow();
            mutate(store);
            await WriteJsonAsync(_trendingPath, store, ct).ConfigureAwait(false);
        }
        finally { _lock.Release(); }
    }

    private static async Task WriteJsonAsync<T>(string path, T value, CancellationToken ct)
    {
        var tmp  = path + ".tmp";
        var json = JsonSerializer.Serialize(value, JsonOpts);
        await File.WriteAllTextAsync(tmp, json, ct).ConfigureAwait(false);
        File.Move(tmp, path, overwrite: true);
    }

    // ── Internal store model ──────────────────────────────────────────────────

    private sealed class TrendingStore
    {
        public Dictionary<string, long> HashtagCurrent  { get; set; } = [];
        public Dictionary<string, long> HashtagPrevious { get; set; } = [];
        public Dictionary<string, long> SoundCurrent    { get; set; } = [];
        public Dictionary<string, long> SoundPrevious   { get; set; } = [];
        public Dictionary<string, SoundMeta> SoundMeta  { get; set; } = [];
        public DateTimeOffset WindowStart                { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Rolls current → previous when the 24-hour window has elapsed.
        /// </summary>
        public void MaybeRollWindow()
        {
            if ((DateTimeOffset.UtcNow - WindowStart).TotalHours < 24)
                return;

            HashtagPrevious = HashtagCurrent;
            SoundPrevious   = SoundCurrent;
            HashtagCurrent  = [];
            SoundCurrent    = [];
            WindowStart     = DateTimeOffset.UtcNow;
        }
    }

    private sealed class SoundMeta
    {
        public string  Title      { get; set; } = string.Empty;
        public string? ArtistName { get; set; }
    }
}
