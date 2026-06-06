// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using AetherMedia.LocalLibrary.Audio.Skin;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Skin;

public class BmpDecoderTests
{
    [Fact]
    public void Decodes_24BitBmp_PreservingBgrChannelOrder()
    {
        // 2×2 image: top row = red,green; bottom row = blue,white. BMP stores
        // bottom-up, BGR per pixel, rows padded to 4-byte boundary.
        var pixels = new byte[]
        {
            // Row 0 (bottom, drawn last): blue, white  + 2 bytes pad
            0xFF, 0x00, 0x00,   0xFF, 0xFF, 0xFF,   0x00, 0x00,
            // Row 1 (top): red, green + 2 bytes pad
            0x00, 0x00, 0xFF,   0x00, 0xFF, 0x00,   0x00, 0x00,
        };
        var bmp = BuildBmp(width: 2, height: 2, bitsPerPixel: 24, pixelData: pixels);

        var frame = BmpDecoder.Decode(bmp);

        Assert.Equal(2, frame.Width);
        Assert.Equal(2, frame.Height);
        // Top-left = red
        AssertPixel(frame, 0, 0, 0xFF, 0, 0);
        // Top-right = green
        AssertPixel(frame, 1, 0, 0, 0xFF, 0);
        // Bottom-left = blue
        AssertPixel(frame, 0, 1, 0, 0, 0xFF);
        // Bottom-right = white
        AssertPixel(frame, 1, 1, 0xFF, 0xFF, 0xFF);
    }

    [Fact]
    public void Rejects_CompressedBmp()
    {
        var bmp = BuildBmp(width: 2, height: 2, bitsPerPixel: 24,
            pixelData: new byte[16], compression: 1 /* BI_RLE8 */);
        Assert.Throws<FormatException>(() => BmpDecoder.Decode(bmp));
    }

    [Fact]
    public void Rejects_MissingMagic()
    {
        var bytes = new byte[64];
        bytes[0] = (byte)'X'; bytes[1] = (byte)'X';
        Assert.Throws<FormatException>(() => BmpDecoder.Decode(bytes));
    }

    /// <summary>Build a minimal BITMAPFILEHEADER + BITMAPINFOHEADER + pixel buffer.</summary>
    private static byte[] BuildBmp(int width, int height, int bitsPerPixel, byte[] pixelData, int compression = 0)
    {
        const int FileHeaderSize = 14;
        const int InfoHeaderSize = 40;
        var paletteSize = bitsPerPixel <= 8 ? (1 << bitsPerPixel) * 4 : 0;
        var pixelOffset = FileHeaderSize + InfoHeaderSize + paletteSize;
        var totalSize   = pixelOffset + pixelData.Length;

        var b = new byte[totalSize];
        b[0] = (byte)'B'; b[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(2,  4), totalSize);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(10, 4), pixelOffset);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(14, 4), InfoHeaderSize);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(18, 4), width);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(22, 4), height);
        BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(26, 2), 1);
        BinaryPrimitives.WriteInt16LittleEndian(b.AsSpan(28, 2), (short)bitsPerPixel);
        BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(30, 4), compression);

        Buffer.BlockCopy(pixelData, 0, b, pixelOffset, pixelData.Length);
        return b;
    }

    private static void AssertPixel(AetherMedia.LocalLibrary.Audio.Visualization.RgbaFrame f, int x, int y, byte r, byte g, byte b)
    {
        var i = (y * f.Width + x) * 4;
        Assert.Equal(r, f.Pixels[i]);
        Assert.Equal(g, f.Pixels[i + 1]);
        Assert.Equal(b, f.Pixels[i + 2]);
        Assert.Equal((byte)0xFF, f.Pixels[i + 3]);
    }
}
