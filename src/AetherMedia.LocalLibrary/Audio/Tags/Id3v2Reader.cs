// SPDX-License-Identifier: MIT

using System.Text;

namespace AetherMedia.LocalLibrary.Audio.Tags;

/// <summary>
/// Minimal but correct ID3v2 (versions 2.3 and 2.4) tag reader. Extracts the
/// fields the player cares about (title / artist / album / year / track /
/// genre) plus ReplayGain values from <c>TXXX</c> user-defined frames.
///
/// <para>
/// Why not pull TagLib# from NuGet? Because the only fields we need are a
/// handful of well-defined frames; a 250-line parser stays inside the no-
/// new-deps discipline. If we ever need Vorbis / MP4 / APE, those can each
/// be added as a new reader (or TagLib# can be considered then).
/// </para>
/// </summary>
public sealed class Id3v2Reader : IAudioTagReader
{
    /// <inheritdoc/>
    public async Task<AudioTags?> ReadAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        await using var fs = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);
        return await ReadAsync(fs, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<AudioTags?> ReadAsync(Stream stream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var header = new byte[10];
        var read = await stream.ReadAsync(header.AsMemory(0, 10), ct).ConfigureAwait(false);
        if (read != 10) return null;
        if (header[0] != (byte)'I' || header[1] != (byte)'D' || header[2] != (byte)'3')
            return null;

        var majorVersion = header[3];
        if (majorVersion is not (3 or 4)) return null;

        var unsync = (header[5] & 0x80) != 0;
        if (unsync) return null; // rare; not implemented

        var tagSize = SynchsafeInt(header, 6);
        var tagBytes = new byte[tagSize];
        read = await stream.ReadAsync(tagBytes.AsMemory(0, tagSize), ct).ConfigureAwait(false);
        if (read != tagSize) return null;

        return Parse(tagBytes, majorVersion);
    }

    private static AudioTags Parse(byte[] tag, byte majorVersion)
    {
        string? title = null, artist = null, album = null, genre = null;
        int? year = null, trackNum = null;
        double? rgGainDb = null, rgPeakDbfs = null;

        var p = 0;
        while (p + 10 <= tag.Length)
        {
            // Frame header: 4-byte ID, 4-byte size, 2-byte flags
            var idBytes = tag.AsSpan(p, 4);
            if (idBytes[0] == 0) break; // padding

            var frameId = Encoding.ASCII.GetString(idBytes);
            var frameSize = majorVersion == 4
                ? SynchsafeInt(tag, p + 4)
                : (int)((uint)tag[p + 4] << 24 | (uint)tag[p + 5] << 16 |
                        (uint)tag[p + 6] << 8 | tag[p + 7]);
            p += 10;
            if (frameSize < 0 || p + frameSize > tag.Length) break;

            var frame = tag.AsSpan(p, frameSize);

            switch (frameId)
            {
                case "TIT2": title  = DecodeText(frame); break;
                case "TPE1": artist = DecodeText(frame); break;
                case "TALB": album  = DecodeText(frame); break;
                case "TCON": genre  = StripGenreParens(DecodeText(frame)); break;
                case "TYER":
                case "TDRC": if (int.TryParse(DecodeText(frame)?.Split('-')[0], out var y)) year = y; break;
                case "TRCK":
                    var trk = DecodeText(frame)?.Split('/')[0];
                    if (int.TryParse(trk, out var n)) trackNum = n;
                    break;
                case "TXXX":
                    (var desc, var val) = DecodeTxxx(frame);
                    if (desc != null && val != null)
                    {
                        if (desc.Equals("REPLAYGAIN_TRACK_GAIN", StringComparison.OrdinalIgnoreCase))
                            rgGainDb = ParseGainDb(val);
                        else if (desc.Equals("REPLAYGAIN_TRACK_PEAK", StringComparison.OrdinalIgnoreCase))
                            rgPeakDbfs = ParsePeak(val);
                    }
                    break;
            }
            p += frameSize;
        }

        return new AudioTags(title, artist, album, year, trackNum, genre, rgGainDb, rgPeakDbfs);
    }

    private static int SynchsafeInt(byte[] data, int offset)
    {
        return ((data[offset] & 0x7F) << 21)
             | ((data[offset + 1] & 0x7F) << 14)
             | ((data[offset + 2] & 0x7F) << 7)
             | (data[offset + 3] & 0x7F);
    }

    private static string? DecodeText(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 1) return null;
        var encoding = frame[0];
        var payload = frame[1..];
        // Strip trailing nulls
        while (payload.Length > 0 && payload[^1] == 0) payload = payload[..^1];
        return DecodeBytes(payload, encoding);
    }

    private static (string? Description, string? Value) DecodeTxxx(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 1) return (null, null);
        var encoding = frame[0];
        var payload = frame[1..];

        // Description is null-terminated, then value follows.
        var splitIndex = FindNullTerminator(payload, encoding);
        if (splitIndex < 0)
            return (DecodeBytes(payload, encoding), null);

        var step = encoding is 1 or 2 ? 2 : 1;
        var desc = payload[..splitIndex];
        var rest = splitIndex + step < payload.Length
            ? payload[(splitIndex + step)..]
            : ReadOnlySpan<byte>.Empty;
        return (DecodeBytes(desc, encoding), DecodeBytes(rest, encoding));
    }

    /// <summary>
    /// Find the index of the first null-terminator in <paramref name="bytes"/>
    /// (single byte for ISO/UTF-8, two bytes for UTF-16). Returns −1 if none.
    /// </summary>
    private static int FindNullTerminator(ReadOnlySpan<byte> bytes, byte encoding)
    {
        var step = encoding is 1 or 2 ? 2 : 1;
        for (var i = 0; i + step <= bytes.Length; i += step)
        {
            var allZero = true;
            for (var k = 0; k < step; k++) if (bytes[i + k] != 0) { allZero = false; break; }
            if (allZero) return i;
        }
        return -1;
    }

    private static string? DecodeBytes(ReadOnlySpan<byte> bytes, byte encoding)
    {
        if (bytes.Length == 0) return null;
        return encoding switch
        {
            0 => Encoding.GetEncoding("ISO-8859-1").GetString(bytes),
            1 => DecodeUtf16WithBom(bytes),
            2 => Encoding.BigEndianUnicode.GetString(bytes),
            3 => Encoding.UTF8.GetString(bytes),
            _ => null,
        };
    }

    private static string DecodeUtf16WithBom(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes[2..]);
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes[2..]);
        return Encoding.Unicode.GetString(bytes);
    }

    private static string? StripGenreParens(string? s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        // ID3v1 numeric genre references like "(17)" -> strip
        if (s.StartsWith('(') && s.IndexOf(')') > 0)
            return s[(s.IndexOf(')') + 1)..].Trim() is var rest && rest.Length > 0 ? rest : null;
        return s;
    }

    private static double? ParseGainDb(string value)
    {
        // e.g. "-6.50 dB", "+1.23 dB"
        var s = value.Trim();
        var space = s.IndexOf(' ');
        if (space > 0) s = s[..space];
        return double.TryParse(s, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static double? ParsePeak(string value)
    {
        // REPLAYGAIN_TRACK_PEAK is stored as a linear amplitude (0.0..1.0+).
        // Convert to dBFS for consistency with LoudnessMeasurement.TruePeakDbfs.
        if (!double.TryParse(value.Trim(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var lin)) return null;
        return lin > 0 ? 20.0 * Math.Log10(lin) : null;
    }
}
