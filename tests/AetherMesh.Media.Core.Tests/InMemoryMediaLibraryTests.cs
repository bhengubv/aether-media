// SPDX-License-Identifier: MIT

using AetherMesh.Media.Core.Models;

namespace AetherMesh.Media.Core.Tests;

/// <summary>
/// Unit tests for <see cref="InMemoryMediaLibrary"/>: CRUD behaviour, search
/// correctness, and the <see cref="InMemoryMediaLibrary.ContentAdded"/> event.
/// </summary>
public sealed class InMemoryMediaLibraryTests
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private static MediaContent MakeContent(
        string hash        = "abc123",
        string title       = "My Video",
        string contentType = "video/mp4",
        string creatorUhid = "creator-1",
        IReadOnlyList<string>? tags = null)
        => new(
            ContentHash:   hash,
            Title:         title,
            DurationMs:    60_000,
            Codec:         "h264",
            ContentType:   contentType,
            CreatorUhid:   creatorUhid,
            SizeBytes:     4096,
            CreatedAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ThumbnailHash: null,
            Tags:          tags ?? Array.Empty<string>());

    private static InMemoryMediaLibrary MakeLibrary() => new();

    // ── Add / Get ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_AddsContent_CanBeRetrieved()
    {
        var lib     = MakeLibrary();
        var content = MakeContent(hash: "hash-001", title: "Hello World");

        await lib.AddAsync(content);
        var retrieved = await lib.GetAsync("hash-001");

        Assert.NotNull(retrieved);
        Assert.Equal("Hello World", retrieved!.Title);
    }

    // ── Remove ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_RemovesContent_ReturnsNullAfter()
    {
        var lib     = MakeLibrary();
        var content = MakeContent(hash: "hash-002");

        await lib.AddAsync(content);
        await lib.RemoveAsync("hash-002");

        var retrieved = await lib.GetAsync("hash-002");
        Assert.Null(retrieved);
    }

    // ── Search ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAsync_MatchesTitle()
    {
        var lib = MakeLibrary();
        await lib.AddAsync(MakeContent(hash: "h1", title: "Sunrise Timelapse"));
        await lib.AddAsync(MakeContent(hash: "h2", title: "Cooking Tutorial"));

        var results = await lib.SearchAsync("Timelapse");

        Assert.Single(results);
        Assert.Equal("Sunrise Timelapse", results[0].Title);
    }

    [Fact]
    public async Task SearchAsync_MatchesTag()
    {
        var lib = MakeLibrary();
        await lib.AddAsync(MakeContent(hash: "h3", title: "Video A", tags: ["nature", "4k"]));
        await lib.AddAsync(MakeContent(hash: "h4", title: "Video B", tags: ["urban", "night"]));

        var results = await lib.SearchAsync("4k");

        Assert.Single(results);
        Assert.Equal("Video A", results[0].Title);
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsAll()
    {
        var lib = MakeLibrary();
        await lib.AddAsync(MakeContent(hash: "h5", title: "Alpha"));
        await lib.AddAsync(MakeContent(hash: "h6", title: "Beta"));
        await lib.AddAsync(MakeContent(hash: "h7", title: "Gamma"));

        var results = await lib.SearchAsync(string.Empty);

        Assert.Equal(3, results.Count);
    }

    // ── ContentAdded event ─────────────────────────────────────────────────

    [Fact]
    public async Task ContentAdded_Event_Fires()
    {
        var lib     = MakeLibrary();
        var content = MakeContent(hash: "h8", title: "EventTest");

        MediaContent? received = null;
        lib.ContentAdded += (_, c) => received = c;

        await lib.AddAsync(content);

        Assert.NotNull(received);
        Assert.Equal("h8",        received!.ContentHash);
        Assert.Equal("EventTest", received.Title);
    }
}
