// SPDX-License-Identifier: MIT

using System.Text;
using AetherMedia.LocalLibrary.Audio.Playlists;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Playlists;

public class M3uPlaylistTests
{
    [Fact]
    public async Task Reads_EXTINF_Annotations_AndPlainPaths()
    {
        const string content = """
            #EXTM3U
            #EXTINF:240,Sample Artist - Track One
            track1.mp3
            #EXTINF:-1,Unknown duration
            track2.mp3
            track3.mp3
            """;
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var pl = await new M3uPlaylist().ReadAsync(ms);

        Assert.Equal(3, pl.Items.Count);
        Assert.Equal("track1.mp3", pl.Items[0].Path);
        Assert.Equal(240, pl.Items[0].DurationSeconds);
        Assert.Equal("Sample Artist - Track One", pl.Items[0].Title);
        Assert.Equal("track2.mp3", pl.Items[1].Path);
        Assert.Null(pl.Items[1].DurationSeconds);
        Assert.Equal("Unknown duration", pl.Items[1].Title);
        Assert.Equal("track3.mp3", pl.Items[2].Path);
        Assert.Null(pl.Items[2].Title);
    }

    [Fact]
    public async Task IgnoresCommentsAndBlanks()
    {
        const string content = "#EXTM3U\n# random comment\n\ntrack.mp3\n";
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var pl = await new M3uPlaylist().ReadAsync(ms);
        Assert.Single(pl.Items);
        Assert.Equal("track.mp3", pl.Items[0].Path);
    }

    [Fact]
    public async Task WriteThenRead_RoundTripsAllItems()
    {
        var input = new Playlist(null, new[]
        {
            new PlaylistItem("a.mp3", "Song A", 100),
            new PlaylistItem("b.mp3", "Song B", 200),
            new PlaylistItem("c.mp3"),
        });
        using var ms = new MemoryStream();
        await new M3uPlaylist().WriteAsync(ms, input);
        ms.Position = 0;
        var output = await new M3uPlaylist().ReadAsync(ms);

        Assert.Equal(3, output.Items.Count);
        Assert.Equal("a.mp3", output.Items[0].Path);
        Assert.Equal("Song A", output.Items[0].Title);
        Assert.Equal(100, output.Items[0].DurationSeconds);
        Assert.Equal("c.mp3", output.Items[2].Path);
    }
}
