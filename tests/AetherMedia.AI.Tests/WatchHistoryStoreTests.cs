// SPDX-License-Identifier: MIT

namespace AetherMedia.AI.Tests;

/// <summary>
/// Unit tests for <see cref="InMemoryWatchHistoryStore"/>.
/// </summary>
public sealed class WatchHistoryStoreTests
{
    // ── No-history returns null ────────────────────────────────────────────

    [Fact]
    public async Task GetCompletionRate_UnknownViewer_ReturnsNull()
    {
        var store = new InMemoryWatchHistoryStore();

        var result = await store.GetCompletionRateAsync("nobody", "hash-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetCompletionRate_KnownViewer_UnknownContent_ReturnsNull()
    {
        var store = new InMemoryWatchHistoryStore();
        await store.RecordWatchEventAsync("viewer-1", "hash-A", watchedMs: 60_000, durationMs: 60_000);

        var result = await store.GetCompletionRateAsync("viewer-1", "hash-B");

        Assert.Null(result);
    }

    // ── Record then retrieve ───────────────────────────────────────────────

    [Fact]
    public async Task RecordAndGet_FullCompletion_ReturnsOne()
    {
        var store = new InMemoryWatchHistoryStore();
        await store.RecordWatchEventAsync("v", "h", watchedMs: 120_000, durationMs: 120_000);

        var rate = await store.GetCompletionRateAsync("v", "h");

        Assert.NotNull(rate);
        Assert.Equal(1.0, rate!.Value, precision: 9);
    }

    [Fact]
    public async Task RecordAndGet_ZeroWatch_ReturnsZero()
    {
        var store = new InMemoryWatchHistoryStore();
        await store.RecordWatchEventAsync("v", "h", watchedMs: 0, durationMs: 60_000);

        var rate = await store.GetCompletionRateAsync("v", "h");

        Assert.NotNull(rate);
        Assert.Equal(0.0, rate!.Value, precision: 9);
    }

    [Fact]
    public async Task RecordAndGet_PartialWatch_ClampsToOne()
    {
        var store = new InMemoryWatchHistoryStore();
        // watchedMs > durationMs (e.g. looping) — should clamp to 1.0
        await store.RecordWatchEventAsync("v", "h", watchedMs: 200_000, durationMs: 100_000);

        var rate = await store.GetCompletionRateAsync("v", "h");

        Assert.NotNull(rate);
        Assert.Equal(1.0, rate!.Value, precision: 9);
    }

    // ── Live-stream handling ───────────────────────────────────────────────

    [Fact]
    public async Task RecordWatchEvent_LiveStream_AnyWatchTimeCountsAsFull()
    {
        // durationMs == 0 means live; any positive watchedMs → 1.0
        var store = new InMemoryWatchHistoryStore();
        await store.RecordWatchEventAsync("v", "live", watchedMs: 5_000, durationMs: 0);

        var rate = await store.GetCompletionRateAsync("v", "live");

        Assert.NotNull(rate);
        Assert.Equal(1.0, rate!.Value, precision: 9);
    }

    [Fact]
    public async Task RecordWatchEvent_LiveStream_ZeroWatchTime_ReturnsZero()
    {
        var store = new InMemoryWatchHistoryStore();
        await store.RecordWatchEventAsync("v", "live", watchedMs: 0, durationMs: 0);

        var rate = await store.GetCompletionRateAsync("v", "live");

        Assert.NotNull(rate);
        Assert.Equal(0.0, rate!.Value, precision: 9);
    }

    // ── EWMA blend on re-watch ─────────────────────────────────────────────

    [Fact]
    public async Task RecordWatchEvent_Rewatch_BlendsWithEwma()
    {
        // α = 0.4 → new = 0.4 × observed + 0.6 × prior
        // First watch: observed = 1.0 → stored = 1.0
        // Second watch: observed = 0.0 → new = 0.4 × 0.0 + 0.6 × 1.0 = 0.6
        var store = new InMemoryWatchHistoryStore();
        await store.RecordWatchEventAsync("v", "h", watchedMs: 60_000, durationMs: 60_000);
        await store.RecordWatchEventAsync("v", "h", watchedMs: 0,      durationMs: 60_000);

        var rate = await store.GetCompletionRateAsync("v", "h");

        Assert.NotNull(rate);
        // 0.4 × 0.0 + 0.6 × 1.0 = 0.6
        Assert.Equal(0.6, rate!.Value, precision: 9);
    }

    [Fact]
    public async Task RecordWatchEvent_MultipleRewatches_ConvergesCorrectly()
    {
        // Seed at 1.0; then three watches at 0.0; each step: rate × 0.6
        // After 1st skip: 0.6; after 2nd: 0.36; after 3rd: 0.216
        var store = new InMemoryWatchHistoryStore();
        await store.RecordWatchEventAsync("v", "h", 60_000, 60_000); // 1.0

        for (var i = 0; i < 3; i++)
            await store.RecordWatchEventAsync("v", "h", 0, 60_000);

        var rate = await store.GetCompletionRateAsync("v", "h");
        Assert.NotNull(rate);
        Assert.InRange(rate!.Value, 0.21, 0.22); // 0.216
    }

    // ── Blank / null inputs are no-ops ────────────────────────────────────

    [Fact]
    public async Task RecordWatchEvent_BlankViewerUhid_IsNoOp()
    {
        var store = new InMemoryWatchHistoryStore();
        await store.RecordWatchEventAsync("  ", "hash", watchedMs: 60_000, durationMs: 60_000);

        var rate = await store.GetCompletionRateAsync("  ", "hash");
        Assert.Null(rate); // nothing was stored
    }

    [Fact]
    public async Task RecordWatchEvent_BlankContentHash_IsNoOp()
    {
        var store = new InMemoryWatchHistoryStore();
        await store.RecordWatchEventAsync("viewer", "", watchedMs: 60_000, durationMs: 60_000);

        var rate = await store.GetCompletionRateAsync("viewer", "");
        Assert.Null(rate);
    }

    // ── Per-viewer cap eviction ────────────────────────────────────────────

    [Fact]
    public async Task RecordWatchEvent_AtCap_EvictsOldestEntry()
    {
        var store = new InMemoryWatchHistoryStore();

        // Fill to exactly MaxEntriesPerViewer
        for (var i = 0; i < InMemoryWatchHistoryStore.MaxEntriesPerViewer; i++)
            await store.RecordWatchEventAsync("v", $"hash-{i}", 1_000, 60_000);

        // Adding one more should evict "hash-0" (oldest)
        await store.RecordWatchEventAsync("v", "hash-new", 60_000, 60_000);

        var evicted = await store.GetCompletionRateAsync("v", "hash-0");
        var newest  = await store.GetCompletionRateAsync("v", "hash-new");

        Assert.Null(evicted);       // oldest was evicted
        Assert.NotNull(newest);     // new entry is present
    }

    // ── Viewer isolation ─────────────────────────────────────────────────

    [Fact]
    public async Task RecordWatchEvent_DifferentViewers_AreiIsolated()
    {
        var store = new InMemoryWatchHistoryStore();
        await store.RecordWatchEventAsync("viewer-A", "hash", 60_000, 60_000); // 1.0
        await store.RecordWatchEventAsync("viewer-B", "hash", 0,      60_000); // 0.0

        var rateA = await store.GetCompletionRateAsync("viewer-A", "hash");
        var rateB = await store.GetCompletionRateAsync("viewer-B", "hash");

        Assert.Equal(1.0, rateA!.Value, precision: 9);
        Assert.Equal(0.0, rateB!.Value, precision: 9);
    }
}
