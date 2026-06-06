// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Skin;
using AetherMedia.LocalLibrary.Audio.Visualization;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Skin;

public class WinampMainWindowRendererTests
{
    [Fact]
    public void Render_PaintsBackground_AndButtonsFromAtlas()
    {
        // Build a skin atlas with a "main" sprite (red) plus "cbuttons" / "titlebar"
        // sprites (green) and verify both sources flow through into the target.
        var redMain = SolidColourBmp(width: 275, height: 116, r: 0xFF, g: 0, b: 0);
        var greenCb = SolidColourBmp(width: 116, height: 36,  r: 0,    g: 0xFF, b: 0);
        var blueTb  = SolidColourBmp(width: 275, height: 29,  r: 0,    g: 0,    b: 0xFF);

        var skin = new WinampClassicSkin(
            Name: "demo",
            Sprites: new Dictionary<string, byte[]>
            {
                ["main"]     = redMain,
                ["cbuttons"] = greenCb,
                ["titlebar"] = blueTb,
            },
            ConfigFiles: new Dictionary<string, string>(),
            RegionDefinition: null,
            VisualizerColors: Array.Empty<(byte, byte, byte)>());

        var atlas = WinampSpriteAtlas.FromSkin(skin);
        var state = new WinampPlayerState(
            Status: WinampPlaybackStatus.Playing,
            WindowActive: true,
            CurrentTrack: "demo",
            PositionFraction: 0.5,
            VolumeFraction: 0.8);

        var renderer = new WinampMainWindowRenderer(atlas, () => state);
        var frame = new RgbaFrame(275, 116);
        renderer.Render(new VisualizationInputs(ReadOnlyMemory<float>.Empty, null, 44100, 2), frame);

        // (200, 50) is inside main background only — must be red.
        AssertPixel(frame, 200, 50, 0xFF, 0, 0);
        // (16, 88) is the buttons origin — must be green.
        AssertPixel(frame, 16 + 1, 88 + 1, 0, 0xFF, 0);
        // (10, 5) is inside the active title bar — must be blue.
        AssertPixel(frame, 10, 5, 0, 0, 0xFF);
    }

    private static byte[] SolidColourBmp(int width, int height, byte r, byte g, byte b)
    {
        var rowBytes = ((24 * width + 31) / 32) * 4;
        var pixelData = new byte[rowBytes * height];
        for (var row = 0; row < height; row++)
        {
            var rowOffset = row * rowBytes;
            for (var col = 0; col < width; col++)
            {
                var o = rowOffset + col * 3;
                pixelData[o]     = b;
                pixelData[o + 1] = g;
                pixelData[o + 2] = r;
            }
        }

        const int FileHeaderSize = 14;
        const int InfoHeaderSize = 40;
        var pixelOffset = FileHeaderSize + InfoHeaderSize;
        var totalSize = pixelOffset + pixelData.Length;
        var bmp = new byte[totalSize];

        bmp[0] = (byte)'B'; bmp[1] = (byte)'M';
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(2,  4), totalSize);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(10, 4), pixelOffset);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(14, 4), InfoHeaderSize);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(18, 4), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bmp.AsSpan(22, 4), height);
        System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(26, 2), 1);
        System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(bmp.AsSpan(28, 2), 24);
        Buffer.BlockCopy(pixelData, 0, bmp, pixelOffset, pixelData.Length);
        return bmp;
    }

    private static void AssertPixel(RgbaFrame f, int x, int y, byte r, byte g, byte b)
    {
        var i = (y * f.Width + x) * 4;
        Assert.Equal(r, f.Pixels[i]);
        Assert.Equal(g, f.Pixels[i + 1]);
        Assert.Equal(b, f.Pixels[i + 2]);
    }
}
