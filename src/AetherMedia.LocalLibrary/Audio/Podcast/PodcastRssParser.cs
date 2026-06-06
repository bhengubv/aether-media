// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace AetherMedia.LocalLibrary.Audio.Podcast;

/// <summary>
/// Parses standard RSS 2.0 podcast feeds, including the iTunes namespace
/// extensions (<c>itunes:duration</c>, <c>itunes:image</c>, etc.).
/// </summary>
public sealed class PodcastRssParser
{
    private const string Itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";
    private static readonly Regex DurationHms =
        new(@"^(?:(\d+):)?(\d+):(\d+)$", RegexOptions.Compiled);

    /// <summary>Parse from a UTF-8 string.</summary>
    public PodcastFeed Parse(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        return ProjectDoc(doc);
    }

    /// <summary>Parse from a stream.</summary>
    public async Task<PodcastFeed> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ct.ThrowIfCancellationRequested();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        return Parse(text);
    }

    private PodcastFeed ProjectDoc(XmlDocument doc)
    {
        var nsm = new XmlNamespaceManager(doc.NameTable);
        nsm.AddNamespace("itunes", Itunes);

        var channel = doc.SelectSingleNode("/rss/channel")
                     ?? throw new FormatException("Not an RSS 2.0 feed (no /rss/channel).");

        var title = ChildText(channel, "title") ?? "(untitled)";
        var description = ChildText(channel, "description");
        Uri.TryCreate(ChildText(channel, "link"), UriKind.Absolute, out var link);
        var language = ChildText(channel, "language");

        // image: standard <image><url>… or iTunes <itunes:image href="…"/>.
        Uri? image = null;
        var imageUrl = ChildText(channel.SelectSingleNode("image"), "url");
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var img)) image = img;
        if (image is null)
        {
            var itImage = channel.SelectSingleNode("itunes:image", nsm);
            if (itImage?.Attributes?["href"]?.Value is { } href
                && Uri.TryCreate(href, UriKind.Absolute, out var it))
                image = it;
        }

        var episodes = new List<PodcastEpisode>();
        foreach (XmlNode item in channel.SelectNodes("item") ?? (System.Collections.IEnumerable)Array.Empty<XmlNode>())
        {
            var enc = item.SelectSingleNode("enclosure");
            var audioUrlStr = enc?.Attributes?["url"]?.Value;
            if (!Uri.TryCreate(audioUrlStr, UriKind.Absolute, out var audioUrl)) continue;

            var lengthAttr = enc?.Attributes?["length"]?.Value;
            long? lengthBytes = long.TryParse(lengthAttr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lb)
                ? lb : null;

            var mime = enc?.Attributes?["type"]?.Value;
            var guid = ChildText(item, "guid") ?? audioUrl.ToString();
            var itemTitle = ChildText(item, "title") ?? "(untitled)";
            var itemDesc = ChildText(item, "description");
            var pubDateStr = ChildText(item, "pubDate");

            var pubDate = DateTimeOffset.UtcNow;
            if (DateTimeOffset.TryParse(pubDateStr, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d))
                pubDate = d;

            TimeSpan? duration = ParseDuration(ChildText(item.SelectSingleNode("itunes:duration", nsm)));

            episodes.Add(new PodcastEpisode(
                Guid: guid,
                Title: itemTitle,
                Description: itemDesc,
                PublishedAtUtc: pubDate,
                AudioUrl: audioUrl,
                LengthBytes: lengthBytes,
                MimeType: mime,
                Duration: duration));
        }

        // Newest first — that's what every player expects.
        episodes.Sort((a, b) => b.PublishedAtUtc.CompareTo(a.PublishedAtUtc));

        return new PodcastFeed(title, description, link, image, language, episodes);
    }

    private static string? ChildText(XmlNode? parent, string name)
    {
        if (parent is null) return null;
        var node = parent.SelectSingleNode(name);
        return node is null
            ? (string.IsNullOrWhiteSpace(parent.InnerText) ? null : null)
            : string.IsNullOrWhiteSpace(node.InnerText) ? null : node.InnerText.Trim();
    }

    private static string? ChildText(XmlNode? node)
    {
        if (node is null) return null;
        return string.IsNullOrWhiteSpace(node.InnerText) ? null : node.InnerText.Trim();
    }

    private static TimeSpan? ParseDuration(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
            return TimeSpan.FromSeconds(seconds);
        var m = DurationHms.Match(text);
        if (!m.Success) return null;
        var h = m.Groups[1].Success ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
        var min = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        var sec = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
        return new TimeSpan(h, min, sec);
    }
}
