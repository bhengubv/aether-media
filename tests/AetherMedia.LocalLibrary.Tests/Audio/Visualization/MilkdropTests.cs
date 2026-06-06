// SPDX-License-Identifier: MIT

using System.Text;
using AetherMedia.LocalLibrary.Audio.Visualization;
using AetherMedia.LocalLibrary.Audio.Visualization.Milkdrop;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Visualization;

public class MilkdropTests
{
    private const string SamplePreset = """
        [preset00]
        fRating=3.000000
        fDecay=0.980000
        zoom=1.000000
        rot=0.000000
        cx=0.500000
        cy=0.500000
        fWaveAlpha=0.800000
        fWaveScale=2.330000
        wave_r=1.000000
        wave_g=0.500000
        wave_b=0.250000
        wave_x=0.500000
        wave_y=0.500000
        bAdditiveWaves=0
        per_frame_1=zoom = 1.05;
        per_frame_2=rot = rot + 0.10;
        per_frame_3=q1 = bass * 2;
        warp_1=`shader_body { ret = 1; }
        """;

    [Fact]
    public async Task Parser_ReadsParametersAndEquations()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(SamplePreset));
        var preset = await new MilkdropPresetParser().ParseAsync(ms);

        Assert.Equal("preset00", preset.SectionName);
        Assert.Equal(0.98, preset.Parameters["fDecay"], 4);
        Assert.Equal(3, preset.PerFrameEquations.Count);
        Assert.Equal("zoom = 1.05;", preset.PerFrameEquations[0]);
        Assert.Empty(preset.PerPixelEquations);
    }

    [Fact]
    public async Task Evaluator_AppliesPerFrameEquations_UpdatingState()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(SamplePreset));
        var preset = await new MilkdropPresetParser().ParseAsync(ms);
        var ev = new MilkdropEvaluator(preset);

        ev.EvaluateFrame(timeSeconds: 0,    frameIndex: 1, fps: 60, bass: 0.5, mid: 0.0, treb: 0.0);
        Assert.Equal(1.05, ev.State.Zoom, 4);
        Assert.Equal(0.10, ev.State.Rot,  4);
        Assert.Equal(1.0,  ev.GetQ(1),    4); // q1 = bass * 2 = 0.5 * 2

        ev.EvaluateFrame(timeSeconds: 0.02, frameIndex: 2, fps: 60, bass: 0.0, mid: 0.0, treb: 0.0);
        // rot accumulates per frame; zoom resets to 1.05 each frame.
        Assert.Equal(1.05, ev.State.Zoom, 4);
        Assert.Equal(0.20, ev.State.Rot,  4);
    }

    [Fact]
    public async Task Renderer_PaintsWaveformAndCarriesPreviousFrame()
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(SamplePreset));
        var preset = await new MilkdropPresetParser().ParseAsync(ms);
        var renderer = new MilkdropRenderer(preset);
        var frame = new RgbaFrame(96, 64);

        var samples = new float[256];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = (float)Math.Sin(2.0 * Math.PI * i / 16.0);

        renderer.Render(
            new VisualizationInputs(samples, new SpectrumFrame(new float[32], 1.0f, 44100), 44100, 1),
            frame);

        // Must paint at least some non-black pixels for the waveform overlay.
        var hasOverlay = false;
        for (var i = 0; i < frame.Pixels.Length; i += 4)
            if (frame.Pixels[i] > 100 && frame.Pixels[i + 1] > 30)
            { hasOverlay = true; break; }
        Assert.True(hasOverlay, "renderer should paint the waveform overlay in the configured colour");
    }
}
