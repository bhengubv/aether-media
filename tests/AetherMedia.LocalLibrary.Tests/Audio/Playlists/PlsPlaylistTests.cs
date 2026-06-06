// SPDX-License-Identifier: MIT

using System.Text;
using AetherMedia.LocalLibrary.Audio.Playlists;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Playlists;

public class PlsPlaylistTests
{
    [Fact]
    public async Task ReadsIndexedEntries_EvenWhenOutOfOrder()
    {
        const string content = """
            [playlist]
            NumberOfEntries=2
            File2=second.mp3
            Title2=Second
            Length2=200
            File1=first.mp3
            Title1=First
            Length1=100
            Version=2
            """;
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var pl = await new PlsPlaylist().ReadAsync(ms);

        Assert.Equal(2, pl.Items.Count);
        Assert.Equal("first.mp3",  pl.Items[0].Path);
        Assert.Equal("First",      pl.Items[0].Title);
        Assert.Equal(100,          pl.Items[0].DurationSeconds);
        Assert.Equal("second.mp3", pl.Items[1].Path);
    }

    [Fact]
    public async Task WriteThenRead_RoundTrips()
    {
        var input = new Playlist(null, new[]
        {
            new PlaylistItem("a.mp3", "A", 60),
            new PlaylistItem("b.mp3", null, 90),
        });
        using var ms = new MemoryStream();
        await new PlsPlaylist().WriteAsync(ms, input);
        ms.Position = 0;
        var output = await new PlsPlaylist().ReadAsync(ms);

        Assert.Equal(2, output.Items.Count);
        Assert.Equal("a.mp3", output.Items[0].Path);
        Assert.Equal("A",     output.Items[0].Title);
        Assert.Equal(60,      output.Items[0].DurationSeconds);
        Assert.Equal("b.mp3", output.Items[1].Path);
        Assert.Null(output.Items[1].Title);
    }

    [Fact]
    public async Task ParsesNegativeLengthAsNull()
    {
        const string content = """
            [playlist]
            File1=stream.mp3
            Length1=-1
            """;
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var pl = await new PlsPlaylist().ReadAsync(ms);

        Assert.Single(pl.Items);
        Assert.Null(pl.Items[0].DurationSeconds);
    }
}
