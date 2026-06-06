// SPDX-License-Identifier: MIT

using System.IO.Compression;
using System.Text;
using AetherMedia.LocalLibrary.Audio.Skin;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Skin;

public class WinampSkinLoaderTests
{
    [Fact]
    public async Task LoadsSpritesAndConfigFromZip()
    {
        var bytes = BuildSkin(
            ("MAIN.BMP", new byte[] { 0x42, 0x4D, 0x00 }),     // pretend BMP
            ("CBUTTONS.BMP", new byte[] { 0x42, 0x4D, 0xFF }), // another sprite
            ("region.txt", "[Normal]"u8.ToArray()),
            ("viscolor.txt", "0,0,0\n255,255,255\n128,128,128"u8.ToArray()));

        using var ms = new MemoryStream(bytes);
        var skin = await new WinampSkinLoader().LoadAsync(ms, "demo");

        Assert.Equal("demo", skin.Name);
        Assert.Equal(2, skin.Sprites.Count);
        Assert.NotNull(skin.TryGetSprite("main"));
        Assert.NotNull(skin.TryGetSprite("MAIN"));
        Assert.NotNull(skin.RegionDefinition);
        Assert.True(skin.VisualizerColors.Count >= 3);
        Assert.Equal((byte)255, skin.VisualizerColors[1].R);
    }

    [Fact]
    public async Task EmptyZip_ReturnsEmptySkin()
    {
        var bytes = BuildSkin(); // no entries
        using var ms = new MemoryStream(bytes);
        var skin = await new WinampSkinLoader().LoadAsync(ms, "empty");
        Assert.Empty(skin.Sprites);
        Assert.Null(skin.RegionDefinition);
    }

    [Fact]
    public void ParsesVisColorIgnoringCommentsAndBlankLines()
    {
        const string text = "// header comment\n12,34,56 // r,g,b\n\n78,90,11\n";
        var parsed = WinampSkinLoader.ParseVisColor(text).ToList();
        Assert.Equal(2, parsed.Count);
        Assert.Equal((12, 34, 56), (parsed[0].R, parsed[0].G, parsed[0].B));
        Assert.Equal((78, 90, 11), (parsed[1].R, parsed[1].G, parsed[1].B));
    }

    private static byte[] BuildSkin(params (string Name, byte[] Bytes)[] entries)
    {
        using var ms = new MemoryStream();
        using (var z = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, data) in entries)
            {
                var e = z.CreateEntry(name);
                using var s = e.Open();
                s.Write(data, 0, data.Length);
            }
        }
        return ms.ToArray();
    }
}
