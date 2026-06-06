// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Podcast;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Podcast;

public class PodcastRssParserTests
{
    [Fact]
    public void Parses_FeedWithItunesNamespace()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <rss xmlns:itunes="http://www.itunes.com/dtds/podcast-1.0.dtd">
              <channel>
                <title>Demo Show</title>
                <description>Demo description.</description>
                <link>https://demo.example/</link>
                <language>en</language>
                <itunes:image href="https://demo.example/cover.jpg"/>
                <item>
                  <title>Episode 2</title>
                  <description>Newer episode.</description>
                  <pubDate>Wed, 03 Jan 2024 10:00:00 GMT</pubDate>
                  <enclosure url="https://demo.example/ep2.mp3" length="1024000" type="audio/mpeg"/>
                  <itunes:duration>05:30</itunes:duration>
                  <guid>ep2-guid</guid>
                </item>
                <item>
                  <title>Episode 1</title>
                  <pubDate>Mon, 01 Jan 2024 10:00:00 GMT</pubDate>
                  <enclosure url="https://demo.example/ep1.mp3" length="2048000" type="audio/mpeg"/>
                  <itunes:duration>720</itunes:duration>
                  <guid>ep1-guid</guid>
                </item>
              </channel>
            </rss>
            """;
        var feed = new PodcastRssParser().Parse(xml);
        Assert.Equal("Demo Show", feed.Title);
        Assert.Equal("https://demo.example/cover.jpg", feed.ImageUrl!.ToString());
        Assert.Equal(2, feed.Episodes.Count);
        Assert.Equal("Episode 2", feed.Episodes[0].Title); // newest first
        Assert.Equal(TimeSpan.FromMinutes(12), feed.Episodes[1].Duration); // "720" seconds → 12 min
    }
}
