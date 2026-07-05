// SPDX-License-Identifier: MIT

using AetherMedia.Ingest.Hls;

namespace AetherMedia.Ingest.Tests;

public sealed class HlsPlaylistParserTests
{
    private static readonly Uri Base = new("https://cdn.example/live/index.m3u8");

    [Fact]
    public void Parses_media_playlist_segments_sequence_and_endlist()
    {
        const string content =
            "#EXTM3U\n" +
            "#EXT-X-VERSION:3\n" +
            "#EXT-X-TARGETDURATION:4\n" +
            "#EXT-X-MEDIA-SEQUENCE:10\n" +
            "#EXTINF:4.0,\n" +
            "seg10.ts\n" +
            "#EXTINF:4.0,\n" +
            "seg11.ts\n" +
            "#EXT-X-ENDLIST\n";

        var playlist = HlsPlaylistParser.Parse(content, Base);

        Assert.True(playlist.HasEndList);
        Assert.Equal(4.0, playlist.TargetDuration);
        Assert.Equal(10L, playlist.MediaSequence);
        Assert.Equal(2, playlist.Segments.Count);
        Assert.Equal(10L, playlist.Segments[0].MediaSequence);
        Assert.Equal(11L, playlist.Segments[1].MediaSequence);
        Assert.Equal(new Uri("https://cdn.example/live/seg10.ts"), playlist.Segments[0].Uri);
        Assert.Equal(4.0, playlist.Segments[0].DurationSeconds);
    }

    [Fact]
    public void Detects_master_and_resolves_variants_relative_to_base()
    {
        const string content =
            "#EXTM3U\n" +
            "#EXT-X-STREAM-INF:BANDWIDTH=800000,RESOLUTION=640x360\n" +
            "media_360.m3u8\n";

        Assert.True(HlsPlaylistParser.IsMaster(content));

        var variants = HlsPlaylistParser.ParseMasterVariants(content, Base);

        Assert.Single(variants);
        Assert.Equal(new Uri("https://cdn.example/live/media_360.m3u8"), variants[0]);
    }

    [Fact]
    public void Throws_on_non_hls_content()
    {
        Assert.Throws<FormatException>(() => HlsPlaylistParser.Parse("not a playlist", Base));
    }
}
