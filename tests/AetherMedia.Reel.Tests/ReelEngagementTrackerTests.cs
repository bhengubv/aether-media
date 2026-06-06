// SPDX-License-Identifier: MIT

using AetherMedia.Reel.Tests.Helpers;

namespace AetherMedia.Reel.Tests;

public sealed class ReelEngagementTrackerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IsolatedReelEngagementTracker _tracker;

    public ReelEngagementTrackerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _tracker = new IsolatedReelEngagementTracker(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── RecordAsync / GetAsync ────────────────────────────────────────────────

    [Fact]
    public async Task RecordAsync_PersistsSignal()
    {
        var signal = MakeSignal("hash1", watchedMs: 30_000, durationMs: 60_000);
        await _tracker.RecordAsync(signal);

        var retrieved = await _tracker.GetAsync("hash1");

        Assert.NotNull(retrieved);
        Assert.Equal("hash1", retrieved.ReelHash);
        Assert.Equal(30_000, retrieved.WatchedMs);
    }

    [Fact]
    public async Task RecordAsync_MergesWatchTime_WhenCalledTwice()
    {
        var s1 = MakeSignal("hash1", watchedMs: 20_000, durationMs: 60_000);
        var s2 = MakeSignal("hash1", watchedMs: 15_000, durationMs: 60_000);

        await _tracker.RecordAsync(s1);
        await _tracker.RecordAsync(s2);

        var result = await _tracker.GetAsync("hash1");
        Assert.NotNull(result);
        Assert.Equal(35_000, result.WatchedMs);
    }

    [Fact]
    public async Task RecordAsync_MergesReplayCount()
    {
        await _tracker.RecordAsync(MakeSignal("r1", replayCount: 2));
        await _tracker.RecordAsync(MakeSignal("r1", replayCount: 1));

        var result = await _tracker.GetAsync("r1");
        Assert.Equal(3, result!.ReplayCount);
    }

    [Fact]
    public async Task RecordAsync_OrsMergesBoolFlags()
    {
        await _tracker.RecordAsync(MakeSignal("r2", liked: false, shared: false));
        await _tracker.RecordAsync(MakeSignal("r2", liked: true,  shared: false));

        var result = await _tracker.GetAsync("r2");
        Assert.True(result!.Liked);
        Assert.False(result.Shared);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenNotSeen()
    {
        var result = await _tracker.GetAsync("unknown");
        Assert.Null(result);
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllStoredSignals()
    {
        await _tracker.RecordAsync(MakeSignal("a"));
        await _tracker.RecordAsync(MakeSignal("b"));
        await _tracker.RecordAsync(MakeSignal("c"));

        var all = await _tracker.GetAllAsync();
        Assert.Equal(3, all.Count);
    }

    // ── CompletionRatio ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(60_000, 60_000, 1.0f)]
    [InlineData(30_000, 60_000, 0.5f)]
    [InlineData(0,      60_000, 0.0f)]
    [InlineData(70_000, 60_000, 1.0f)]   // clamped to 1
    public void CompletionRatio_IsComputedCorrectly(long watchedMs, long durationMs, float expected)
    {
        var signal = MakeSignal("x", watchedMs: watchedMs, durationMs: durationMs);
        Assert.Equal(expected, signal.CompletionRatio, precision: 3);
    }

    [Fact]
    public void CompletionRatio_IsZero_WhenDurationIsZero()
    {
        var signal = MakeSignal("x", durationMs: 0);
        Assert.Equal(0f, signal.CompletionRatio);
    }

    // ── RecentHashtags ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRecentHashtagsAsync_ReturnsHashesWithinWindow()
    {
        var recent = MakeSignal("recent", lastWatchedAt: DateTimeOffset.UtcNow.AddMinutes(-10));
        var old    = MakeSignal("old",    lastWatchedAt: DateTimeOffset.UtcNow.AddHours(-25));

        await _tracker.RecordAsync(recent);
        await _tracker.RecordAsync(old);

        // window = 1 hour (3 600 000 ms)
        var result = await _tracker.GetRecentHashtagsAsync(windowMs: 3_600_000);

        Assert.Contains("recent", result);
        Assert.DoesNotContain("old", result);
    }

    // ── ResetAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetAsync_ClearsAllSignals()
    {
        await _tracker.RecordAsync(MakeSignal("a"));
        await _tracker.RecordAsync(MakeSignal("b"));

        await _tracker.ResetAsync();

        var all = await _tracker.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task ResetAsync_AllowsNewRecordsAfterReset()
    {
        await _tracker.RecordAsync(MakeSignal("a"));
        await _tracker.ResetAsync();
        await _tracker.RecordAsync(MakeSignal("b"));

        var all = await _tracker.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("b", all[0].ReelHash);
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Signals_SurviveTrackerRecreation()
    {
        await _tracker.RecordAsync(MakeSignal("persistent"));

        // Create a new tracker pointing at the same directory
        var tracker2 = new IsolatedReelEngagementTracker(_tempDir);
        var result   = await tracker2.GetAsync("persistent");

        Assert.NotNull(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ReelEngagementSignal MakeSignal(
        string           reelHash,
        long             watchedMs     = 10_000,
        long             durationMs    = 30_000,
        int              replayCount   = 0,
        bool             liked         = false,
        bool             shared        = false,
        bool             skipped       = false,
        DateTimeOffset?  lastWatchedAt = null)
        => new(reelHash, watchedMs, durationMs, replayCount, liked, shared, skipped,
               lastWatchedAt ?? DateTimeOffset.UtcNow);
}
