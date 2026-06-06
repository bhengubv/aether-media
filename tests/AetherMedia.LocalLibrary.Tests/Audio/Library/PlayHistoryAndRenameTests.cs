// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Library;
using AetherMedia.LocalLibrary.Audio.Tags;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Library;

public class PlayHistoryAndRenameTests
{
    [Fact]
    public async Task PlayHistory_AggregatesStats_AndOrders()
    {
        var store = new InMemoryPlayHistoryStore();
        await store.RecordAsync(new PlayEvent("a.mp3", DateTimeOffset.UtcNow.AddMinutes(-10), 240_000));
        await store.RecordAsync(new PlayEvent("a.mp3", DateTimeOffset.UtcNow.AddMinutes(-5),  240_000));
        await store.RecordAsync(new PlayEvent("b.mp3", DateTimeOffset.UtcNow,                180_000));

        var a = await store.GetAsync("a.mp3");
        Assert.Equal(2, a.PlayCount);
        Assert.Equal(480_000, a.TotalListenedMs);

        var top = await store.MostPlayedAsync(10);
        Assert.Equal("a.mp3", top[0].FilePath);

        var recent = await store.RecentlyPlayedAsync(10);
        Assert.Equal("b.mp3", recent[0].FilePath);
    }

    [Fact]
    public async Task TagBasedRenamer_RendersTemplate_WithTags()
    {
        var fakeReader = new ConstReader(new AudioTags(
            Title: "Track One",
            Artist: "Sample Artist",
            Album: "Sample Album",
            Year: 2023,
            TrackNumber: 4,
            Genre: "Pop",
            ReplayGainTrackDb: null,
            ReplayGainTrackPeakDbfs: null));

        var renamer = new TagBasedRenamer(fakeReader);
        var newPath = await renamer.ComputeNewPathAsync(
            @"C:\Music\original.mp3",
            "{Artist}/{Album}/{Track:00} - {Title}.{Ext}");
        Assert.EndsWith(System.IO.Path.Combine("Sample Artist", "Sample Album", "04 - Track One.mp3"), newPath);
    }

    private sealed class ConstReader : IAudioTagReader
    {
        private readonly AudioTags _tags;
        public ConstReader(AudioTags tags) { _tags = tags; }
        public Task<AudioTags?> ReadAsync(string filePath, CancellationToken ct = default) => Task.FromResult<AudioTags?>(_tags);
        public Task<AudioTags?> ReadAsync(Stream stream, CancellationToken ct = default) => Task.FromResult<AudioTags?>(_tags);
    }
}
