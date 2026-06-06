// SPDX-License-Identifier: MIT

using System.Text.Json;
using AetherNet.Media.Reel.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherNet.Media.Reel;

/// <summary>
/// JSON-backed on-device engagement tracker. All data lives in
/// <c>%APPDATA%/aether-media/reel-engagement.json</c> (or an injected path for
/// testing). A <see cref="SemaphoreSlim"/> ensures concurrent writes are safe.
/// </summary>
public class ReelEngagementTracker : IReelEngagementTracker
{
    private readonly string _filePath;
    private readonly ILogger<ReelEngagementTracker> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    // ── Constructors ──────────────────────────────────────────────────────────

    public ReelEngagementTracker(ILogger<ReelEngagementTracker>? logger = null)
        : this(null, logger) { }

    // Protected constructor allows test subclasses to inject a temp directory.
    protected ReelEngagementTracker(string? dataDirectory, ILogger<ReelEngagementTracker>? logger = null)
    {
        var dir = dataDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "aether-media");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "reel-engagement.json");
        _logger   = logger ?? NullLogger<ReelEngagementTracker>.Instance;
    }

    // ── IReelEngagementTracker ────────────────────────────────────────────────

    public async Task RecordAsync(ReelEngagementSignal signal, CancellationToken ct = default)
    {
        await ModifyAsync(signals =>
        {
            var idx = signals.FindIndex(s => s.ReelHash == signal.ReelHash);
            if (idx < 0)
            {
                signals.Add(signal);
            }
            else
            {
                var existing = signals[idx];
                signals[idx] = existing with
                {
                    WatchedMs       = existing.WatchedMs + signal.WatchedMs,
                    ReplayCount     = existing.ReplayCount + signal.ReplayCount,
                    Liked           = existing.Liked || signal.Liked,
                    Shared          = existing.Shared || signal.Shared,
                    Skipped         = existing.Skipped || signal.Skipped,
                    LastWatchedAt   = signal.LastWatchedAt > existing.LastWatchedAt
                                        ? signal.LastWatchedAt
                                        : existing.LastWatchedAt,
                };
            }
        }, ct).ConfigureAwait(false);
    }

    public async Task<ReelEngagementSignal?> GetAsync(string reelHash, CancellationToken ct = default)
    {
        var all = await LoadAsync(ct).ConfigureAwait(false);
        return all.Find(s => s.ReelHash == reelHash);
    }

    public async Task<IReadOnlyList<ReelEngagementSignal>> GetAllAsync(CancellationToken ct = default)
        => await LoadAsync(ct).ConfigureAwait(false);

    public async Task<IReadOnlyDictionary<string, float>> GetHashtagAffinitiesAsync(CancellationToken ct = default)
    {
        // Affinity requires access to the Reel index (hashtag lists).
        // We compute a proxy here: average completion ratio across all reels,
        // grouped by the hashtags stored in the persisted meta.  Because the
        // engagement file only stores per-reel signals we return an empty dict;
        // ReelFeed enriches this with the full Reel model.
        await Task.CompletedTask.ConfigureAwait(false);
        return new Dictionary<string, float>();
    }

    public async Task<IReadOnlyDictionary<string, float>> GetCreatorAffinitiesAsync(CancellationToken ct = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        return new Dictionary<string, float>();
    }

    public async Task<IReadOnlySet<string>> GetRecentHashtagsAsync(
        long windowMs = 3_600_000,
        CancellationToken ct = default)
    {
        // Returns hashes of reels watched within the window — ReelFeed maps to hashtags.
        var cutoff = DateTimeOffset.UtcNow.AddMilliseconds(-windowMs);
        var all    = await LoadAsync(ct).ConfigureAwait(false);
        return all
            .Where(s => s.LastWatchedAt >= cutoff)
            .Select(s => s.ReelHash)
            .ToHashSet();
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        await ModifyAsync(signals => signals.Clear(), ct).ConfigureAwait(false);
        _logger.LogInformation("ReelEngagementTracker: engagement data reset.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<List<ReelEngagementSignal>> LoadAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
            return [];

        try
        {
            var json = await File.ReadAllTextAsync(_filePath, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<ReelEngagementSignal>>(json, JsonOpts) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ReelEngagementTracker: failed to load engagement file, starting fresh.");
            return [];
        }
    }

    private async Task ModifyAsync(Action<List<ReelEngagementSignal>> mutate, CancellationToken ct)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var signals = await LoadAsync(ct).ConfigureAwait(false);
            mutate(signals);

            var tmp = _filePath + ".tmp";
            var json = JsonSerializer.Serialize(signals, JsonOpts);
            await File.WriteAllTextAsync(tmp, json, ct).ConfigureAwait(false);
            File.Move(tmp, _filePath, overwrite: true);
        }
        finally
        {
            _lock.Release();
        }
    }
}
