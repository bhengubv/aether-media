// SPDX-License-Identifier: MIT

using System.Text;
using AetherMedia.LocalLibrary.Audio.Playlists;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Playlists;

public class XspfPlaylistTests
{
    [Fact]
    public async Task ReadsTracks_Including_DurationInMillis()
    {
        const string content = """
            <?xml version="1.0" encoding="UTF-8"?>
            <playlist version="1" xmlns="http://xspf.org/ns/0/">
              <title>Demo</title>
              <trackList>
                <track>
                  <location>track.mp3</location>
                  <title>Track</title>
                  <duration>180000</duration>
                </track>
              </trackList>
            </playlist>
            """;
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var pl = await new XspfPlaylist().ReadAsync(ms);

        Assert.Equal("Demo", pl.Title);
        Assert.Single(pl.Items);
        Assert.Equal("Track", pl.Items[0].Title);
        Assert.Equal(180, pl.Items[0].DurationSeconds);
    }

    [Fact]
    public async Task WriteThenRead_PreservesTitleAndDuration()
    {
        var input = new Playlist("My List", new[]
        {
            new PlaylistItem("https://example.com/stream.mp3", "Stream", 0),
            new PlaylistItem("https://example.com/track.mp3",  "Track",  300),
        });
        using var ms = new MemoryStream();
        await new XspfPlaylist().WriteAsync(ms, input);
        ms.Position = 0;
        var output = await new XspfPlaylist().ReadAsync(ms);

        Assert.Equal("My List", output.Title);
        Assert.Equal(2, output.Items.Count);
        Assert.Equal("Track",   output.Items[1].Title);
        Assert.Equal(300,       output.Items[1].DurationSeconds);
        Assert.Equal("https://example.com/track.mp3", output.Items[1].Path);
    }
}
