// SPDX-License-Identifier: MIT

using System.Text;
using AetherMedia.LocalLibrary.Audio.Visualization;
using AetherMedia.LocalLibrary.Audio.Visualization.Avs;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Visualization;

public class AvsGeissTests
{
    [Fact]
    public async Task AvsParser_ReadsHeader_AndCollectsEffectBlobs()
    {
        var bytes = BuildMinimalAvs(effectTypeCodes: new[] { 1, 2 });
        using var ms = new MemoryStream(bytes);
        var preset = await new AvsPresetParser().ParseAsync(ms);
        Assert.False(preset.ClearEveryFrame);
        Assert.Equal(2, preset.EffectBlobs.Count);
        Assert.Equal(1, preset.EffectBlobs[0].TypeCode);
    }

    [Fact]
    public void AvsRenderer_PaintsNonEmptyFrame_WithSpectrumInput()
    {
        var preset = new AvsPreset("0.2", ClearEveryFrame: true, EffectBlobs: Array.Empty<AvsEffectBlob>());
        var renderer = new AvsRenderer(preset);
        var frame = new RgbaFrame(48, 32);
        var mags = new float[24];
        for (var i = 0; i < mags.Length; i++) mags[i] = 0.6f;

        renderer.Render(new VisualizationInputs(
            TimeDomainSamples: new float[64],
            Spectrum: new SpectrumFrame(mags, 1.0f, 44100),
            SampleRateHz: 44100,
            Channels: 1), frame);

        Assert.Contains(frame.Pixels, p => p > 0);
    }

    [Fact]
    public void GeissRenderer_ProducesColouredOutput_OverFrames()
    {
        var renderer = new GeissRenderer();
        var frame = new RgbaFrame(32, 32);
        var mags = new float[16];
        for (var i = 0; i < mags.Length; i++) mags[i] = 0.7f;

        for (var f = 0; f < 5; f++)
            renderer.Render(
                new VisualizationInputs(new float[32], new SpectrumFrame(mags, 1.0f, 44100), 44100, 1),
                frame);

        // At least one pixel must be coloured.
        Assert.Contains(frame.Pixels, p => p > 0);
    }

    private static byte[] BuildMinimalAvs(int[] effectTypeCodes)
    {
        // "Nullsoft AVS Preset 0.2\x1A" + 1 clear byte + repeated (typecode, length=0).
        var header = Encoding.ASCII.GetBytes("Nullsoft AVS Preset 0.2\x1A");
        var body = new List<byte>(header) { 0 };
        foreach (var code in effectTypeCodes)
        {
            body.AddRange(BitConverter.GetBytes(code));
            body.AddRange(BitConverter.GetBytes(0)); // length 0
        }
        return body.ToArray();
    }
}
