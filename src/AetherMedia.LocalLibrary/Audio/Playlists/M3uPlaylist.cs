// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;

namespace AetherMedia.LocalLibrary.Audio.Playlists;

/// <summary>
/// M3U / M3U8 reader + writer. <c>.m3u</c> is the original Winamp playlist
/// format — one path per line, with optional <c>#EXTINF:duration,title</c>
/// header lines preceding each entry. <c>.m3u8</c> is identical but
/// canonically UTF-8.
/// </summary>
public sealed class M3uPlaylist : IPlaylistReader, IPlaylistWriter
{
    /// <inheritdoc/>
    public string FormatId => "m3u";

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
        var items = new List<PlaylistItem>();
        string? pendingTitle = null;
        int? pendingDuration = null;

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        while (await reader.ReadLineAsync(ct).ConfigureAwait(false) is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            if (trimmed.StartsWith("#EXTINF:", StringComparison.Ordinal))
            {
                ParseExtInf(trimmed.AsSpan(8), out pendingDuration, out pendingTitle);
                continue;
            }

            if (trimmed.StartsWith('#')) continue; // #EXTM3U + arbitrary comments

            items.Add(new PlaylistItem(trimmed, pendingTitle, pendingDuration));
            pendingTitle = null;
            pendingDuration = null;
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
            NewLine = "\n",
        };

        await writer.WriteLineAsync("#EXTM3U").ConfigureAwait(false);
        foreach (var item in playlist.Items)
        {
            ct.ThrowIfCancellationRequested();
            if (item.DurationSeconds is { } d || !string.IsNullOrEmpty(item.Title))
            {
                var dur = item.DurationSeconds ?? -1;
                var title = item.Title ?? "";
                await writer.WriteLineAsync($"#EXTINF:{dur.ToString(CultureInfo.InvariantCulture)},{title}").ConfigureAwait(false);
            }
            await writer.WriteLineAsync(item.Path).ConfigureAwait(false);
        }
        await writer.FlushAsync(ct).ConfigureAwait(false);
    }

    private static void ParseExtInf(ReadOnlySpan<char> body, out int? duration, out string? title)
    {
        duration = null;
        title = null;
        var comma = body.IndexOf(',');
        if (comma < 0)
        {
            if (int.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out var d))
                duration = d >= 0 ? d : null;
            return;
        }
        var durSpan = body[..comma];
        if (int.TryParse(durSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dur))
            duration = dur >= 0 ? dur : null;
        var titleSpan = body[(comma + 1)..].Trim();
        if (titleSpan.Length > 0) title = titleSpan.ToString();
    }
}
