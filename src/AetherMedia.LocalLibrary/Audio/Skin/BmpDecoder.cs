// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using AetherMedia.LocalLibrary.Audio.Visualization;

namespace AetherMedia.LocalLibrary.Audio.Skin;

/// <summary>
/// Minimal pure-managed BMP decoder. Handles the subset of the format that
/// Winamp classic skin BMPs actually use: BITMAPINFOHEADER + uncompressed
/// 8-bit indexed and 24-bit BGR. Output is RGBA8888 in
/// <see cref="RgbaFrame"/> form so the renderer side has no BMP knowledge.
///
/// <para>
/// Why not <see cref="System.Drawing"/> / SkiaSharp? <see cref="System.Drawing"/>
/// is Windows-only post .NET 6 and SkiaSharp is a heavy native dependency
/// the audio library doesn't otherwise need. A 200-line BMP reader covers
/// every skin in the Winamp Skin Museum without either.
/// </para>
/// </summary>
public static class BmpDecoder
{
    /// <summary>Decode a BMP byte buffer into an RGBA frame.</summary>
    public static RgbaFrame Decode(ReadOnlySpan<byte> bmp)
    {
        if (bmp.Length < 54)
            throw new FormatException("BMP buffer too small.");
        if (bmp[0] != (byte)'B' || bmp[1] != (byte)'M')
            throw new FormatException("Not a BMP file (missing BM signature).");

        var pixelOffset = BinaryPrimitives.ReadInt32LittleEndian(bmp.Slice(10, 4));
        var dibHeaderSize = BinaryPrimitives.ReadInt32LittleEndian(bmp.Slice(14, 4));
        if (dibHeaderSize < 40)
            throw new FormatException($"Unsupported DIB header size {dibHeaderSize} (need BITMAPINFOHEADER).");

        var width = BinaryPrimitives.ReadInt32LittleEndian(bmp.Slice(18, 4));
        var rawHeight = BinaryPrimitives.ReadInt32LittleEndian(bmp.Slice(22, 4));
        var height = Math.Abs(rawHeight);
        var topDown = rawHeight < 0;

        var bitsPerPixel = BinaryPrimitives.ReadInt16LittleEndian(bmp.Slice(28, 2));
        var compression  = BinaryPrimitives.ReadInt32LittleEndian(bmp.Slice(30, 4));
        if (compression != 0)
            throw new FormatException($"Unsupported BMP compression {compression} (need BI_RGB).");

        var paletteEntries = BinaryPrimitives.ReadInt32LittleEndian(bmp.Slice(46, 4));
        if (paletteEntries == 0 && bitsPerPixel <= 8) paletteEntries = 1 << bitsPerPixel;

        var palette = bitsPerPixel <= 8 ? new (byte R, byte G, byte B)[paletteEntries] : Array.Empty<(byte, byte, byte)>();
        if (palette.Length > 0)
        {
            var paletteStart = 14 + dibHeaderSize;
            for (var i = 0; i < palette.Length; i++)
            {
                var o = paletteStart + i * 4;
                // BMP palette entries are stored BGRA (the A byte is reserved).
                palette[i] = (bmp[o + 2], bmp[o + 1], bmp[o]);
            }
        }

        var stride = ((bitsPerPixel * width + 31) / 32) * 4; // padded to 4-byte boundary
        var frame = new RgbaFrame(width, height);
        var src = bmp[pixelOffset..];

        for (var row = 0; row < height; row++)
        {
            var srcRow = topDown ? row : height - 1 - row;
            var srcLine = src.Slice(srcRow * stride, stride);
            switch (bitsPerPixel)
            {
                case 8:
                    for (var x = 0; x < width; x++)
                    {
                        var pi = srcLine[x];
                        var (r, g, b) = palette[pi];
                        frame.SetPixel(x, row, r, g, b, 0xFF);
                    }
                    break;
                case 24:
                    for (var x = 0; x < width; x++)
                    {
                        var o = x * 3;
                        // BMP rows are BGR.
                        frame.SetPixel(x, row, srcLine[o + 2], srcLine[o + 1], srcLine[o], 0xFF);
                    }
                    break;
                case 32:
                    for (var x = 0; x < width; x++)
                    {
                        var o = x * 4;
                        // BGRA — alpha is the 4th byte in the file.
                        frame.SetPixel(x, row, srcLine[o + 2], srcLine[o + 1], srcLine[o], srcLine[o + 3]);
                    }
                    break;
                default:
                    throw new FormatException($"Unsupported BMP bit depth {bitsPerPixel}.");
            }
        }

        return frame;
    }
}
