// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;

namespace AetherMedia.LocalLibrary.Audio.Playlists;

/// <summary>
/// PLS reader + writer — the INI-style playlist format Winamp adopted
/// alongside M3U. Entries are keyed by index:
/// <code>
/// [playlist]
/// NumberOfEntries=2
/// File1=track.mp3
/// Title1=Track One
/// Length1=240
/// </code>
/// </summary>
public sealed class PlsPlaylist : IPlaylistReader, IPlaylistWriter
{
    /// <inheritdoc/>
    public string FormatId => "pls";

    /// <inheritdoc/>
    public async Task<Playlist> ReadAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return await ReadAsync(fs, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Playlist> ReadAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var files = new SortedDictionary<int, string>();
        var titles = new SortedDictionary<int, string>();
        var lengths = new SortedDictionary<int, int>();

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        while (await reader.ReadLineAsync(ct).ConfigureAwait(false) is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('[') || trimmed.StartsWith(';')) continue;
            var eq = trimmed.IndexOf('=');
            if (eq <= 0) continue;
            var key = trimmed[..eq];
            var value = trimmed[(eq + 1)..];

            if (TryParseKey(key, "File", out var fi)) files[fi] = value;
            else if (TryParseKey(key, "Title", out var ti)) titles[ti] = value;
            else if (TryParseKey(key, "Length", out var li)
                     && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lv))
                lengths[li] = lv;
            // ignore NumberOfEntries / Version — files/titles/lengths drive output
        }

        var items = new List<PlaylistItem>(files.Count);
        foreach (var (idx, path) in files)
        {
            titles.TryGetValue(idx, out var title);
            int? duration = lengths.TryGetValue(idx, out var d) && d >= 0 ? d : null;
            items.Add(new PlaylistItem(path, title, duration));
        }
        return new Playlist(Title: null, Items: items);
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

        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 4096, leaveOpen: true)
        {
            NewLine = "\r\n", // PLS readers historically expect CRLF
        };

        await writer.WriteLineAsync("[playlist]").ConfigureAwait(false);
        await writer.WriteLineAsync($"NumberOfEntries={playlist.Items.Count.ToString(CultureInfo.InvariantCulture)}").ConfigureAwait(false);

        var idx = 1;
        foreach (var item in playlist.Items)
        {
            ct.ThrowIfCancellationRequested();
            await writer.WriteLineAsync($"File{idx}={item.Path}").ConfigureAwait(false);
            if (!string.IsNullOrEmpty(item.Title))
                await writer.WriteLineAsync($"Title{idx}={item.Title}").ConfigureAwait(false);
            await writer.WriteLineAsync($"Length{idx}={(item.DurationSeconds ?? -1).ToString(CultureInfo.InvariantCulture)}").ConfigureAwait(false);
            idx++;
        }
        await writer.WriteLineAsync("Version=2").ConfigureAwait(false);
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    private static bool TryParseKey(ReadOnlySpan<char> key, ReadOnlySpan<char> prefix, out int index)
    {
        index = 0;
        if (key.Length <= prefix.Length || !key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;
        return int.TryParse(key[prefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out index);
    }
}
