// SPDX-License-Identifier: MIT

namespace AetherMesh.Media.Content.Tests;

/// <summary>Unit tests for <see cref="LruContentCache"/>.</summary>
public sealed class LruContentCacheTests
{
    // ── Construction ───────────────────────────────────────────────────────

    [Fact]
    public void Constructor_DefaultCapacity_Is500Mib()
    {
        var cache = new LruContentCache();
        // Default capacity (500 MiB) must fit a 1-byte entry without eviction.
        cache.Set("k", new byte[1]);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Constructor_ZeroCapacity_FallsBackToDefault()
    {
        var cache = new LruContentCache(capacityBytes: 0);
        cache.Set("k", new byte[1]);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void Constructor_NegativeCapacity_FallsBackToDefault()
    {
        var cache = new LruContentCache(capacityBytes: -100);
        cache.Set("k", new byte[1]);
        Assert.Equal(1, cache.Count);
    }

    // ── Initial state ──────────────────────────────────────────────────────

    [Fact]
    public void Count_IsZeroOnConstruction()
    {
        var cache = new LruContentCache();
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void TotalBytes_IsZeroOnConstruction()
    {
        var cache = new LruContentCache();
        Assert.Equal(0L, cache.TotalBytes);
    }

    // ── TryGet ─────────────────────────────────────────────────────────────

    [Fact]
    public void TryGet_EmptyCache_ReturnsFalse()
    {
        var cache = new LruContentCache();
        var hit = cache.TryGet("no-such-key", out var data);
        Assert.False(hit);
        Assert.Empty(data);
    }

    [Fact]
    public void TryGet_AfterSet_ReturnsTrueAndCorrectData()
    {
        var cache = new LruContentCache();
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        cache.Set("abc", bytes);

        var hit = cache.TryGet("abc", out var retrieved);

        Assert.True(hit);
        Assert.Equal(bytes, retrieved);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryGet_NullOrWhitespaceKey_Throws(string key)
    {
        var cache = new LruContentCache();
        Assert.Throws<ArgumentException>(() => cache.TryGet(key, out _));
    }

    // ── Set ────────────────────────────────────────────────────────────────

    [Fact]
    public void Set_IncreasesCountAndTotalBytes()
    {
        var cache = new LruContentCache();
        cache.Set("a", new byte[100]);
        cache.Set("b", new byte[200]);

        Assert.Equal(2, cache.Count);
        Assert.Equal(300L, cache.TotalBytes);
    }

    [Fact]
    public void Set_SameKey_UpdatesDataInPlace()
    {
        var cache = new LruContentCache();
        cache.Set("key", new byte[100]);
        cache.Set("key", new byte[50]);   // smaller update

        Assert.Equal(1, cache.Count);
        Assert.Equal(50L, cache.TotalBytes);

        var hit = cache.TryGet("key", out var data);
        Assert.True(hit);
        Assert.Equal(50, data.Length);
    }

    [Fact]
    public void Set_NullData_Throws()
    {
        var cache = new LruContentCache();
        Assert.Throws<ArgumentNullException>(() => cache.Set("k", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Set_NullOrWhitespaceKey_Throws(string key)
    {
        var cache = new LruContentCache();
        Assert.Throws<ArgumentException>(() => cache.Set(key, new byte[1]));
    }

    [Fact]
    public void Set_ItemLargerThanCapacity_IsNotCached()
    {
        // Capacity: 10 bytes; entry: 11 bytes → must be silently skipped.
        var cache = new LruContentCache(capacityBytes: 10);
        cache.Set("big", new byte[11]);

        Assert.Equal(0, cache.Count);
        Assert.Equal(0L, cache.TotalBytes);
    }

    // ── Eviction ───────────────────────────────────────────────────────────

    [Fact]
    public void Set_ExceedsCapacity_EvictsLruEntry()
    {
        // Capacity: 10 bytes; add three 4-byte entries → first is LRU → evicted.
        var cache = new LruContentCache(capacityBytes: 10);
        cache.Set("first",  new byte[4]);   // LRU
        cache.Set("second", new byte[4]);
        cache.Set("third",  new byte[4]);   // Adding this pushes total to 12; evict "first"

        Assert.Equal(2, cache.Count);
        Assert.False(cache.TryGet("first",  out _));
        Assert.True(cache.TryGet("second", out _));
        Assert.True(cache.TryGet("third",  out _));
    }

    [Fact]
    public void Set_ThenGet_PromotesMruPreservesHit()
    {
        // Capacity: 8 bytes.  Add "a"(4) then "b"(4).
        // Access "a" to promote it to MRU, then add "c"(4).
        // "b" is now LRU and must be evicted; "a" survives.
        var cache = new LruContentCache(capacityBytes: 8);
        cache.Set("a", new byte[4]);   // LRU
        cache.Set("b", new byte[4]);   // MRU
        cache.TryGet("a", out _);      // "a" becomes MRU; "b" becomes LRU
        cache.Set("c", new byte[4]);   // evicts "b"

        Assert.True(cache.TryGet("a",  out _), "'a' should survive (was MRU)");
        Assert.True(cache.TryGet("c",  out _), "'c' should be present");
        Assert.False(cache.TryGet("b", out _), "'b' should have been evicted (was LRU)");
    }

    // ── Evict ──────────────────────────────────────────────────────────────

    [Fact]
    public void Evict_RemovesEntry_DecreasesCountAndBytes()
    {
        var cache = new LruContentCache();
        cache.Set("x", new byte[64]);
        cache.Evict("x");

        Assert.Equal(0, cache.Count);
        Assert.Equal(0L, cache.TotalBytes);
        Assert.False(cache.TryGet("x", out _));
    }

    [Fact]
    public void Evict_NonExistentKey_IsNoOp()
    {
        var cache = new LruContentCache();
        cache.Set("a", new byte[10]);

        // Should not throw and should not corrupt other state.
        cache.Evict("does-not-exist");

        Assert.Equal(1, cache.Count);
        Assert.Equal(10L, cache.TotalBytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Evict_WhitespaceKey_Throws(string key)
    {
        var cache = new LruContentCache();
        Assert.Throws<ArgumentException>(() => cache.Evict(key));
    }

    // ── Key lookup is case-insensitive ─────────────────────────────────────

    [Fact]
    public void TryGet_IsCaseInsensitive()
    {
        var cache = new LruContentCache();
        cache.Set("ABC", new byte[] { 42 });

        Assert.True(cache.TryGet("abc", out var data));
        Assert.Equal(42, data[0]);
    }
}
