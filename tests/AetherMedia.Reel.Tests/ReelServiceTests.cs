// SPDX-License-Identifier: MIT

using AetherMedia.Reel.Tests.Helpers;

namespace AetherMedia.Reel.Tests;

public sealed class ReelServiceTests : IDisposable
{
    private readonly string                 _tempDir;
    private readonly NoOpContentService     _content;
    private readonly IsolatedReelDiscovery  _discovery;
    private readonly IsolatedReelService    _service;

    public ReelServiceTests()
    {
        _tempDir   = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);

        _content   = new NoOpContentService();
        _discovery = new IsolatedReelDiscovery(_tempDir);
        _service   = new IsolatedReelService(_content, _discovery, "UHID-TEST", _tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── PublishAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task PublishAsync_PublishesAndAnnouncesToContentService()
    {
        var file = CreateTempVideo(durationHint: 15_000);
        try
        {
            await _service.PublishAsync(file, soundHash: null, hashtags: ["test"]);

            Assert.Single(_content.Published);
            Assert.Single(_content.Announced);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task PublishAsync_FiresReelPublishedEvent()
    {
        Reel? fired = null;
        _service.ReelPublished += (_, r) => fired = r;

        var file = CreateTempVideo();
        try
        {
            await _service.PublishAsync(file, soundHash: null, hashtags: []);
            Assert.NotNull(fired);
            Assert.Equal("UHID-TEST", fired.CreatorUhid);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task PublishAsync_NormalisesHashtags_ToLowerCaseWithoutHash()
    {
        var file = CreateTempVideo();
        try
        {
            var reel = await _service.PublishAsync(file, null, ["#Dance", "#MUSIC"]);
            Assert.Contains("dance", reel.Hashtags);
            Assert.Contains("music", reel.Hashtags);
            Assert.DoesNotContain("#dance", reel.Hashtags);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task PublishAsync_Throws_WhenFileNotFound()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _service.PublishAsync("/nonexistent/path.mp4", null, []));
    }

    // ── PublishDuetAsync / PublishStitchAsync ─────────────────────────────────

    [Fact]
    public async Task PublishDuetAsync_SetsTypeAndSourceHash()
    {
        var file = CreateTempVideo();
        try
        {
            var reel = await _service.PublishDuetAsync(file, "source-hash-abc");
            Assert.Equal(ReelType.Duet, reel.Type);
            Assert.Equal("source-hash-abc", reel.SourceReelHash);
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task PublishStitchAsync_SetsTypeAndSourceHash()
    {
        var file = CreateTempVideo();
        try
        {
            var reel = await _service.PublishStitchAsync(file, "source-hash-xyz");
            Assert.Equal(ReelType.Stitch, reel.Type);
            Assert.Equal("source-hash-xyz", reel.SourceReelHash);
        }
        finally { File.Delete(file); }
    }

    // ── Like / Unlike ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LikeAsync_RecordsLike()
    {
        await _service.LikeAsync("reel1");
        Assert.True(await _service.IsLikedAsync("reel1"));
    }

    [Fact]
    public async Task UnlikeAsync_RemovesLike()
    {
        await _service.LikeAsync("reel1");
        await _service.UnlikeAsync("reel1");
        Assert.False(await _service.IsLikedAsync("reel1"));
    }

    [Fact]
    public async Task LikeAsync_IsIdempotent()
    {
        await _service.LikeAsync("reel1");
        await _service.LikeAsync("reel1");
        Assert.True(await _service.IsLikedAsync("reel1"));
    }

    // ── Bookmark ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task BookmarkAsync_RecordsBookmark()
    {
        await _service.BookmarkAsync("b1");
        Assert.True(await _service.IsBookmarkedAsync("b1"));
    }

    [Fact]
    public async Task UnbookmarkAsync_RemovesBookmark()
    {
        await _service.BookmarkAsync("b1");
        await _service.UnbookmarkAsync("b1");
        Assert.False(await _service.IsBookmarkedAsync("b1"));
    }

    // ── Comments ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddCommentAsync_PersistsComment()
    {
        var comment = await _service.AddCommentAsync("reel1", "great reel!");

        var all = await _service.GetCommentsAsync("reel1");
        Assert.Single(all);
        Assert.Equal("great reel!", all[0].Text);
        Assert.Equal("UHID-TEST", all[0].AuthorUhid);
    }

    [Fact]
    public async Task AddCommentAsync_SupportsReplies()
    {
        var parent = await _service.AddCommentAsync("reel1", "parent comment");
        var reply  = await _service.AddCommentAsync("reel1", "reply", parent.CommentId);

        Assert.Equal(parent.CommentId, reply.ParentCommentId);
    }

    [Fact]
    public async Task GetCommentsAsync_ReturnsOnlyCommentsForThatReel()
    {
        await _service.AddCommentAsync("reel1", "comment on reel1");
        await _service.AddCommentAsync("reel2", "comment on reel2");

        var reel1Comments = await _service.GetCommentsAsync("reel1");
        Assert.Single(reel1Comments);
        Assert.Equal("comment on reel1", reel1Comments[0].Text);
    }

    // ── GetByCreatorAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByCreatorAsync_ReturnsOwnReels()
    {
        var f1 = CreateTempVideo();
        var f2 = CreateTempVideo();
        try
        {
            await _service.PublishAsync(f1, null, []);
            await _service.PublishAsync(f2, null, []);

            var mine = await _service.GetByCreatorAsync("UHID-TEST");
            Assert.Equal(2, mine.Count);
            Assert.All(mine, r => Assert.Equal("UHID-TEST", r.CreatorUhid));
        }
        finally
        {
            File.Delete(f1);
            File.Delete(f2);
        }
    }

    // ── Reel model ────────────────────────────────────────────────────────────

    [Fact]
    public void Reel_Hashtags_DefaultsToEmpty_WhenNull()
    {
        var reel = new Reel("hash", "uhid", "title", 15_000, null, null,
                            null!,  // null Hashtags — should default to []
                            ReelType.Original, null, null, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 0, 0);
        Assert.NotNull(reel.Hashtags);
        Assert.Empty(reel.Hashtags);
    }

    [Fact]
    public void Reel_Allows_Exactly60s()
    {
        var reel = new Reel("hash", "uhid", "title", 60_000, null, null, [],
                            ReelType.Original, null, null, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 0, 0);
        Assert.Equal(60_000, reel.DurationMs);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string CreateTempVideo(long durationHint = 10_000)
    {
        // Each file gets unique content so SHA-256 hashes differ (content-addressed storage deduplicates identical bytes)
        var path  = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".mp4");
        var bytes = new byte[512];
        Guid.NewGuid().ToByteArray().CopyTo(bytes, 0);   // first 16 bytes differ per call
        File.WriteAllBytes(path, bytes);
        return path;
    }
}
