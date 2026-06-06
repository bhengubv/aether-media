// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;
using System.Xml;

namespace AetherMedia.LocalLibrary.Audio.Playlists;

/// <summary>
/// XSPF (XML Shareable Playlist Format) reader + writer. Used by VLC and the
/// modern Winamp playlist export. Duration is stored in milliseconds.
/// </summary>
public sealed class XspfPlaylist : IPlaylistReader, IPlaylistWriter
{
    private const string XspfNs = "http://xspf.org/ns/0/";

    /// <inheritdoc/>
    public string FormatId => "xspf";

    /// <inheritdoc/>
    public async Task<Playlist> ReadAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return await ReadAsync(fs, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<Playlist> ReadAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ct.ThrowIfCancellationRequested();

        var doc = new XmlDocument();
        doc.Load(stream);

        string? playlistTitle = null;
        var items = new List<PlaylistItem>();
        if (doc.DocumentElement is not { } root) return Task.FromResult(Playlist.Empty);

        var nsm = new XmlNamespaceManager(doc.NameTable);
        nsm.AddNamespace("x", XspfNs);

        var titleNode = root.SelectSingleNode("x:title", nsm) ?? root.SelectSingleNode("title");
        if (titleNode is not null && !string.IsNullOrWhiteSpace(titleNode.InnerText))
            playlistTitle = titleNode.InnerText.Trim();

        var trackList = root.SelectSingleNode("x:trackList", nsm)
                       ?? root.SelectSingleNode("trackList");
        if (trackList is null) return Task.FromResult(new Playlist(playlistTitle, items));

        foreach (XmlNode track in trackList.ChildNodes)
        {
            if (track.NodeType != XmlNodeType.Element) continue;
            var location = ChildText(track, "location", nsm);
            if (string.IsNullOrEmpty(location)) continue;

            var title = ChildText(track, "title", nsm);
            int? durationSeconds = null;
            var durationStr = ChildText(track, "duration", nsm);
            if (int.TryParse(durationStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var durMs) && durMs >= 0)
                durationSeconds = (int)Math.Round(durMs / 1000.0);

            items.Add(new PlaylistItem(DecodeLocation(location), title, durationSeconds));
        }

        return Task.FromResult(new Playlist(playlistTitle, items));
    }

    /// <inheritdoc/>
    public async Task WriteAsync(string filePath, Playlist playlist, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentNullException.ThrowIfNull(playlist);
        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await WriteAsync(fs, playlist, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task WriteAsync(Stream stream, Playlist playlist, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(playlist);

        var settings = new XmlWriterSettings
        {
            Async = true,
            Indent = true,
            IndentChars = "  ",
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        };

        await using var w = XmlWriter.Create(stream, settings);
        await w.WriteStartDocumentAsync().ConfigureAwait(false);
        await w.WriteStartElementAsync(prefix: null, localName: "playlist", ns: XspfNs).ConfigureAwait(false);
        await w.WriteAttributeStringAsync(prefix: null, localName: "version", ns: null, value: "1").ConfigureAwait(false);

        if (!string.IsNullOrEmpty(playlist.Title))
            await w.WriteElementStringAsync(prefix: null, localName: "title", ns: XspfNs, value: playlist.Title).ConfigureAwait(false);

        await w.WriteStartElementAsync(prefix: null, localName: "trackList", ns: XspfNs).ConfigureAwait(false);
        foreach (var item in playlist.Items)
        {
            ct.ThrowIfCancellationRequested();
            await w.WriteStartElementAsync(prefix: null, localName: "track", ns: XspfNs).ConfigureAwait(false);
            await w.WriteElementStringAsync(prefix: null, localName: "location", ns: XspfNs, value: EncodeLocation(item.Path)).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(item.Title))
                await w.WriteElementStringAsync(prefix: null, localName: "title", ns: XspfNs, value: item.Title).ConfigureAwait(false);
            if (item.DurationSeconds is { } s)
                await w.WriteElementStringAsync(prefix: null, localName: "duration", ns: XspfNs,
                    value: (s * 1000).ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
            await w.WriteEndElementAsync().ConfigureAwait(false);
        }
        await w.WriteEndElementAsync().ConfigureAwait(false);
        await w.WriteEndElementAsync().ConfigureAwait(false);
        await w.WriteEndDocumentAsync().ConfigureAwait(false);
        await w.FlushAsync().ConfigureAwait(false);
    }

    private static string? ChildText(XmlNode parent, string name, XmlNamespaceManager nsm)
    {
        var node = parent.SelectSingleNode($"x:{name}", nsm) ?? parent.SelectSingleNode(name);
        return node is null ? null : (string.IsNullOrWhiteSpace(node.InnerText) ? null : node.InnerText.Trim());
    }

    private static string EncodeLocation(string path)
    {
        // If it already looks URL-encoded or is a URI scheme, leave it alone.
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
            return uri.ToString();
        // Best-effort file URI for local paths.
        try { return new Uri(System.IO.Path.GetFullPath(path)).AbsoluteUri; }
        catch { return path; }
    }

    private static string DecodeLocation(string location)
    {
        if (Uri.TryCreate(location, UriKind.Absolute, out var uri) && uri.IsFile)
            return uri.LocalPath;
        return location;
    }
}
