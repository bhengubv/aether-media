// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Bookmarks;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Bookmarks;

public class InMemoryBookmarkStoreTests
{
    [Fact]
    public async Task AddAndList_RoundTripsBookmarks()
    {
        var store = new InMemoryBookmarkStore();
        await store.AddAsync(new Bookmark("a.mp3", 60_000, Label: "riff"));
        await store.AddAsync(new Bookmark("a.mp3", 90_000));
        await store.AddAsync(new Bookmark("b.mp3", 30_000));

        var all = await store.ListAsync();
        Assert.Equal(3, all.Count);

        var forA = await store.ListAsync("a.mp3");
        Assert.Equal(2, forA.Count);
    }

    [Fact]
    public async Task ResumeFor_ReturnsMostRecentBookmarkForFile()
    {
        var store = new InMemoryBookmarkStore();
        await store.AddAsync(new Bookmark("a.mp3", 60_000, CreatedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-10)));
        await store.AddAsync(new Bookmark("a.mp3", 90_000, CreatedAtUtc: DateTimeOffset.UtcNow));
        var resume = await store.ResumeFor("a.mp3");
        Assert.NotNull(resume);
        Assert.Equal(90_000, resume!.PositionMs);
    }
}
