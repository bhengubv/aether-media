// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Text;

namespace AetherMedia.LocalLibrary.Audio.Visualization.Avs;

/// <summary>
/// Helper that reads typed fields out of an AVS effect's binary parameter
/// payload. AVS stores effect config as little-endian 32-bit ints plus the
/// occasional length-prefixed string for scripted effects.
/// </summary>
public ref struct AvsPayloadReader
{
    private readonly ReadOnlySpan<byte> _payload;
    private int _pos;

    public AvsPayloadReader(ReadOnlySpan<byte> payload)
    {
        _payload = payload;
        _pos = 0;
    }

    public int Remaining => _payload.Length - _pos;

    public bool TryReadInt32(out int value)
    {
        if (Remaining < 4) { value = 0; return false; }
        value = BinaryPrimitives.ReadInt32LittleEndian(_payload[_pos..]);
        _pos += 4;
        return true;
    }

    public int ReadInt32(int fallback = 0) => TryReadInt32(out var v) ? v : fallback;

    /// <summary>Read a length-prefixed string (length is uint32, body is ASCII).</summary>
    public bool TryReadLengthPrefixedString(out string value)
    {
        if (!TryReadInt32(out var len)) { value = ""; return false; }
        if (len < 0 || len > Remaining) { value = ""; return false; }
        value = Encoding.ASCII.GetString(_payload.Slice(_pos, len)).TrimEnd('\0');
        _pos += len;
        return true;
    }

    public string ReadLengthPrefixedString() => TryReadLengthPrefixedString(out var v) ? v : "";

    /// <summary>Read <paramref name="count"/> raw bytes.</summary>
    public ReadOnlySpan<byte> ReadBytes(int count)
    {
        var got = Math.Min(count, Remaining);
        var s = _payload.Slice(_pos, got);
        _pos += got;
        return s;
    }
}
