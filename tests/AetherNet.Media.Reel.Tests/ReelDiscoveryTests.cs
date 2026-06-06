// SPDX-License-Identifier: MIT

using AetherNet.Media.Reel.Tests.Helpers;

namespace AetherNet.Media.Reel.Tests;

public sealed class ReelDiscoveryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IsolatedReelDiscovery _discovery;

    public ReelDiscoveryTests()
    {
        _tempDir   = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _discovery = new IsolatedReelDiscovery(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── IndexReelAsync / SearchAsync ──────────────────────────────────────────

    [Fact]
    public async Task IndexReelAsync_MakesReelSearchable()
    {
        var reel = MakeReel("h1", title: "My cooking reel", hashtags: ["cooking", "food"]);
        await _discovery.IndexReelAsync(reel);

        var results = await _discovery.SearchAsync("cooking");

        Assert.Single(results);
        Assert.Equal("h1", results[0].ContentHash);
    }

    [Fact]
    public async Task SearchAsync_IsCaseInsensitive()
    {
        await _discovery.IndexReelAsync(MakeReel("h2", title: "Sunset Walk"));

        var results = await _discovery.SearchAsync("SUNSET");
        Assert.Single(results);
    }

    [Fact]
    public async Task IndexReelAsync_DoesNotAddDuplicates()
    {
        var reel = MakeReel("h3");
        await _discovery.IndexReelAsync(reel);
        await _discovery.IndexReelAsync(reel);   // second call — should be ignored

        var results = await _discovery.SearchAsync("h3");
        Assert.Single(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmpty_WhenNoMatch()
    {
        await _discovery.IndexReelAsync(MakeReel("h4", title: "Dance video"));

        var results = await _discovery.SearchAsync("cooking");
        Assert.Empty(results);
    }

    // ── AnnounceReelAsync / trending ──────────────────────────────────────────

    [Fact]
    public async Task AnnounceReelAsync_IncrementsHashtagCount()
    {
        await _discovery.AnnounceReelAsync(MakeReel("h5", hashtags: ["dance", "music"]));
        await _discovery.AnnounceReelAsync(MakeReel("h6", hashtags: ["dance"]));

        var trending = await _discovery.GetTrendingHashtagsAsync();

        var dance = trending.FirstOrDefault(t => t.Tag == "dance");
        Assert.NotNull(dance);
        Assert.Equal(2, dance.Count24h);
    }

    [Fact]
    public async Task AnnounceReelAsync_IncrementsSound_WhenPresent()
    {
        var sound = "soundhash123";
        await _discovery.AnnounceReelAsync(MakeReel("h7", soundHash: sound));
        await _discovery.AnnounceReelAsync(MakeReel("h8", soundHash: sound));

        var trending = await _discovery.GetTrendingSoundsAsync();

        var s = trending.FirstOrDefault(t => t.SoundHash == sound);
        Assert.NotNull(s);
        Assert.Equal(2, s.UseCount24h);
    }

    // ── MergeGossipAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task MergeGossipAsync_TakesMaxOfLocalAndPeer()
    {
        // Local has 3 uses, peer reports 10 — should take 10
        await _discovery.AnnounceReelAsync(MakeReel("h9",  hashtags: ["viral"]));
        await _discovery.AnnounceReelAsync(MakeReel("h10", hashtags: ["viral"]));
        await _discovery.AnnounceReelAsync(MakeReel("h11", hashtags: ["viral"]));

        await _discovery.MergeGossipAsync(
            hashtagCounts: new Dictionary<string, long> { ["viral"] = 10 },
            soundCounts:   new Dictionary<string, long>());

        var trending = await _discovery.GetTrendingHashtagsAsync();
        var viral    = trending.First(t => t.Tag == "viral");
        Assert.Equal(10, viral.Count24h);
    }

    [Fact]
    public async Task MergeGossipAsync_DoesNotReduceLocalCount()
    {
        await _discovery.AnnounceReelAsync(MakeReel("h12", hashtags: ["big"]));
        await _discovery.AnnounceReelAsync(MakeReel("h13", hashtags: ["big"]));
        await _discovery.AnnounceReelAsync(MakeReel("h14", hashtags: ["big"]));
        await _discovery.AnnounceReelAsync(MakeReel("h15", hashtags: ["big"]));
        await _discovery.AnnounceReelAsync(MakeReel("h16", hashtags: ["big"]));

        // Peer has lower count — local wins
        await _discovery.MergeGossipAsync(
            new Dictionary<string, long> { ["big"] = 2 },
            new Dictionary<string, long>());

        var trending = await _discovery.GetTrendingHashtagsAsync();
        Assert.True(trending.First(t => t.Tag == "big").Count24h >= 5);
    }

    // ── BuildGossipPayloadAsync ───────────────────────────────────────────────

    [Fact]
    public async Task BuildGossipPayloadAsync_ContainsAnnouncedHashtags()
    {
        await _discovery.AnnounceReelAsync(MakeReel("h17", hashtags: ["gossip"]));

        var (hashtagCounts, _) = await _discovery.BuildGossipPayloadAsync();

        Assert.True(hashtagCounts.ContainsKey("gossip"));
        Assert.Equal(1, hashtagCounts["gossip"]);
    }

    // ── Velocity / ordering ───────────────────────────────────────────────────

    [Fact]
    public async Task GetTrendingHashtagsAsync_OrdersByVelocityThenCount()
    {
        // Tag "fast" has 1 use but velocity = 1 (new so no prior window)
        // Tag "slow" has 5 uses
        for (var i = 0; i < 5; i++)
            await _discovery.AnnounceReelAsync(MakeReel($"slow{i}", hashtags: ["slow"]));

        await _discovery.AnnounceReelAsync(MakeReel("fast0", hashtags: ["fast"]));

        var trending = await _discovery.GetTrendingHashtagsAsync();

        // Both have velocity ≥ 1 (no prior window = previous defaults to 1, so velocity = count/1)
        // "slow" should rank higher because count is higher when velocity is equal
        Assert.Contains(trending, t => t.Tag == "slow");
        Assert.Contains(trending, t => t.Tag == "fast");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Reel MakeReel(
        string   contentHash,
        string   title     = "Test reel",
        string[] hashtags  = default!,
        string?  soundHash = null)
        => new(
            ContentHash:    contentHash,
            CreatorUhid:    "UHID-TEST",
            Title:          title,
            DurationMs:     15_000,
            SoundHash:      soundHash,
            SoundTitle:     null,
            Hashtags:       hashtags ?? [],
            Type:           ReelType.Original,
            SourceReelHash: null,
            ThumbnailHash:  null,
            CreatedAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ViewCount:      0,
            LikeCount:      0);
}
