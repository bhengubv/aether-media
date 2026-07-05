// SPDX-License-Identifier: MIT

using System.Text;
using AetherMedia.Ingest.Hls;

namespace AetherMedia.Ingest.Tests;

public sealed class HlsSourceAdapterTests
{
    [Fact]
    public async Task Resolves_master_then_media_and_yields_segment_bytes_verbatim()
    {
        const string masterUrl = "https://test.local/master.m3u8";
        const string mediaUrl = "https://test.local/media.m3u8";
        const string seg0Url = "https://test.local/seg0.ts";
        const string seg1Url = "https://test.local/seg1.ts";

        const string master = "#EXTM3U\n#EXT-X-STREAM-INF:BANDWIDTH=800000\nmedia.m3u8\n";
        const string media =
            "#EXTM3U\n#EXT-X-TARGETDURATION:2\n#EXT-X-MEDIA-SEQUENCE:0\n" +
            "#EXTINF:2.0,\nseg0.ts\n#EXTINF:2.0,\nseg1.ts\n#EXT-X-ENDLIST\n";
        var seg0 = new byte[] { 10, 20, 30 };
        var seg1 = new byte[] { 40, 50, 60 };

        var responses = new Dictionary<string, (string ContentType, byte[] Body)>
        {
            [masterUrl] = ("application/vnd.apple.mpegurl", Encoding.UTF8.GetBytes(master)),
            [mediaUrl] = ("application/vnd.apple.mpegurl", Encoding.UTF8.GetBytes(media)),
            [seg0Url] = ("video/mp2t", seg0),
            [seg1Url] = ("video/mp2t", seg1),
        };

        using var http = new HttpClient(new StubHttpMessageHandler(responses));
        var adapter = new HlsSourceAdapter(http);

        var descriptor = new SourceDescriptor { Uri = new Uri(masterUrl), Kind = SourceKind.Hls };
        var collected = new List<MediaSegment>();
        await foreach (var segment in adapter.ReadAsync(descriptor))
        {
            collected.Add(segment);
        }

        Assert.Equal(2, collected.Count);
        Assert.Equal(seg0, collected[0].Payload.ToArray());
        Assert.Equal(seg1, collected[1].Payload.ToArray());
        Assert.True(collected[0].IsKeyframe);
        Assert.Equal("ts", collected[0].Container);
        Assert.Equal(0u, collected[0].Sequence);
        Assert.Equal(1u, collected[1].Sequence);
        Assert.Equal(2000L, collected[0].DurationMs);
    }

    [Fact]
    public void CanHandle_matches_m3u8_extension_and_hls_kind()
    {
        using var http = new HttpClient(
            new StubHttpMessageHandler(new Dictionary<string, (string ContentType, byte[] Body)>()));
        var adapter = new HlsSourceAdapter(http);

        Assert.True(adapter.CanHandle(
            new SourceDescriptor { Uri = new Uri("https://x/y.m3u8"), Kind = SourceKind.Continuous }));
        Assert.True(adapter.CanHandle(
            new SourceDescriptor { Uri = new Uri("https://x/y"), Kind = SourceKind.Hls }));
        Assert.False(adapter.CanHandle(
            new SourceDescriptor { Uri = new Uri("https://x/y.mpd"), Kind = SourceKind.Dash }));
    }
}
