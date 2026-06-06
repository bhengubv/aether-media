// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using AetherMedia.LocalLibrary.Audio.Skin;
using AetherMedia.LocalLibrary.Audio.Visualization;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Skin;

public class WinampSpriteAtlasTests
{
    [Fact]
    public void FromSkin_DecodesBmps_AndBlitSlicesPixels()
    {
        // Build a 2×2 BMP that the decoder can read: all red.
        var redBmp = MakeRedBmp(2, 2);
        var skin = new WinampClassicSkin(
            Name: "test",
            Sprites: new Dictionary<string, byte[]> { ["main"] = redBmp },
            ConfigFiles: new Dictionary<string, string>(),
            RegionDefinition: null,
            VisualizerColors: Array.Empty<(byte, byte, byte)>());

        var atlas = WinampSpriteAtlas.FromSkin(skin);
        Assert.NotNull(atlas.TryGetSprite("main"));

        // Blit a 1×1 slice at the top-left into a 4×4 frame.
        var target = new RgbaFrame(4, 4);
        atlas.Blit(new WinampSpriteSlice("main", 0, 0, 1, 1), target, destX: 1, destY: 1);

        // The painted pixel must be red.
        var i = (1 * target.Width + 1) * 4;
        Assert.Equal((byte)0xFF, target.Pixels[i]);
        Assert.Equal((byte)0x00, target.Pixels[i + 1]);
        Assert.Equal((byte)0x00, target.Pixels[i + 2]);
        // Untouched pixels stay at the default RGBA(0,0,0,0).
        Assert.Equal((byte)0, target.Pixels[0]);
    }

    [Fact]
    public void Blit_MissingSprite_IsNoop()
    {
        var skin = new WinampClassicSkin(
            Name: "empty",
            Sprites: new Dictionary<string, byte[]>(),
            ConfigFiles: new Dictionary<string, string>(),
            RegionDefinition: null,
            VisualizerColors: Array.Empty<(byte, byte, byte)>());

        var atlas = WinampSpriteAtlas.FromSkin(skin);
        var target = new RgbaFrame(2, 2);
        atlas.Blit(WinampMainWindowLayout.Background, target, 0, 0);

        // Frame stays at its default zeroed state.
        Assert.All(target.Pixels, p => Assert.Equal(0, p));
    }

    private static byte[] MakeRedBmp(int width, int height)
    {
        // BMP rows are padded to 4-byte boundaries: stride = ceil(24*w/8 / 4) * 4.
        var rowBytes = ((24 * width + 31) / 32) * 4;
        var pixelData = new byte[rowBytes * height];
        for (var row = 0; row < height; row++)
        {
            var rowOffset = row * rowBytes;
            for (var col = 0; col < width; col++)
            {
                var o = rowOffset + col * 3;
                pixelData[o]     = 0x00; // B
                pixelData[o + 1] = 0x00; // G
                pixelData[o + 2] = 0xFF; // R
            }
        }

        const int FileHeaderSize = 14;
        const int InfoHeaderSize = 40;
        var pixelOffset = FileHeaderSize + InfoHeaderSize;
        var totalSize = pixelOffset + pixelData.Length;
        var bmp = new byte[totalSize];

        bmp[0] = (byte)'B'; bmp[1] = (byte)'M';
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(2, 4),  totalSize);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(10, 4), pixelOffset);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14, 4), InfoHeaderSize);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18, 4), width);
        BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22, 4), height);
        BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(26, 2), 1);
        BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(28, 2), 24);
        Buffer.BlockCopy(pixelData, 0, bmp, pixelOffset, pixelData.Length);
        return bmp;
    }
}
