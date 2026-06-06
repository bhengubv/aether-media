// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Lyrics;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Lyrics;

public class LrcTests
{
    [Fact]
    public void Parser_ReadsMetadataAndTimedLines()
    {
        const string lrc = """
            [ti:Test]
            [ar:Sample]
            [00:00.50]Hello
            [00:02.00]World
            [00:04.500]End
            """;
        var file = new LrcParser().Parse(lrc);
        Assert.Equal("Test", file.Title);
        Assert.Equal(3, file.Lines.Count);
        Assert.Equal(TimeSpan.FromMilliseconds(500), file.Lines[0].Offset);
        Assert.Equal("World", file.Lines[1].Text);
    }

    [Fact]
    public void Parser_HonoursOffsetMs()
    {
        const string lrc = "[offset:-500]\n[00:01.00]Hi";
        var file = new LrcParser().Parse(lrc);
        Assert.Single(file.Lines);
        Assert.Equal(TimeSpan.FromMilliseconds(500), file.Lines[0].Offset);
    }

    [Fact]
    public void Synchronizer_PicksLineAtOrBeforePosition()
    {
        var file = new LrcParser().Parse("[00:00.00]A\n[00:02.00]B\n[00:04.00]C");
        var sync = new LrcSynchronizer(file);

        Assert.Equal("A", sync.GetActiveLine(TimeSpan.FromMilliseconds(500))!.Text);
        Assert.Equal("B", sync.GetActiveLine(TimeSpan.FromSeconds(3))!.Text);
        Assert.Equal("C", sync.GetActiveLine(TimeSpan.FromSeconds(10))!.Text);
    }
}
