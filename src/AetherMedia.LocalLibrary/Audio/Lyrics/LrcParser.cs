// SPDX-License-Identifier: MIT

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AetherMedia.LocalLibrary.Audio.Lyrics;

/// <summary>
/// Parses standard LRC files. Recognises:
/// <list type="bullet">
///   <item><description>Metadata: <c>[ti:Title]</c>, <c>[ar:Artist]</c>, <c>[al:Album]</c>, <c>[length:mm:ss]</c>, <c>[offset:NNN]</c>.</description></item>
///   <item><description>Time-stamped lines: <c>[mm:ss.xx]Lyric text</c> — with multiple stamps allowed per line (enhanced LRC).</description></item>
///   <item><description>Karaoke per-word stamps <c>&lt;mm:ss.xx&gt;</c> — kept inline in the text.</description></item>
/// </list>
/// </summary>
public sealed class LrcParser
{
    private static readonly Regex TimestampRegex =
        new(@"\[(\d{1,2}):(\d{2})(?:\.(\d{1,3}))?\]", RegexOptions.Compiled);
    private static readonly Regex MetadataRegex =
        new(@"^\[(ti|ar|al|length|offset|au|by):\s*(.+)\]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Parse from a UTF-8 string.</summary>
    public LrcFile Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string? title = null, artist = null, album = null;
        var offsetMs = 0;
        var lines = new List<LyricLine>();

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim().TrimEnd('\r');
            if (line.Length == 0) continue;

            var meta = MetadataRegex.Match(line);
            if (meta.Success)
            {
                var key = meta.Groups[1].Value.ToLowerInvariant();
                var value = meta.Groups[2].Value.Trim();
                switch (key)
                {
                    case "ti":     title  = value; break;
                    case "ar":     title ??= null; artist = value; break;
                    case "al":     album  = value; break;
                    case "offset": _ = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out offsetMs); break;
                }
                continue;
            }

            var stamps = new List<TimeSpan>();
            var lastEnd = 0;
            foreach (Match m in TimestampRegex.Matches(line))
            {
                if (m.Index != lastEnd) break; // stamps must be contiguous at the start
                var minutes = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                var seconds = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                var fracStr = m.Groups[3].Success ? m.Groups[3].Value : "0";
                // LRC fractional part is hundredths or milliseconds — normalise to ms.
                var frac = int.Parse(fracStr, CultureInfo.InvariantCulture);
                if (fracStr.Length == 2) frac *= 10;        // hundredths → ms
                else if (fracStr.Length == 1) frac *= 100;  // tenths → ms
                stamps.Add(new TimeSpan(0, 0, minutes, seconds, frac));
                lastEnd = m.Index + m.Length;
            }
            if (stamps.Count == 0) continue;

            var lyric = line[lastEnd..].Trim();
            foreach (var ts in stamps)
            {
                var adjusted = ts.Add(TimeSpan.FromMilliseconds(offsetMs));
                if (adjusted < TimeSpan.Zero) adjusted = TimeSpan.Zero;
                lines.Add(new LyricLine(adjusted, lyric));
            }
        }

        lines.Sort((a, b) => a.Offset.CompareTo(b.Offset));
        return new LrcFile(title, artist, album, lines);
    }

    /// <summary>Parse from a stream.</summary>
    public async Task<LrcFile> ParseAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var text = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
        return Parse(text);
    }
}
