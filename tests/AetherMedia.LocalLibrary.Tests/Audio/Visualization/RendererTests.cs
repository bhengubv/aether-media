// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Visualization;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Visualization;

public class RendererTests
{
    [Fact]
    public void Oscilloscope_DrawsLineOnBlackBackground()
    {
        var frame = new RgbaFrame(64, 32);
        var samples = new float[256];
        for (var i = 0; i < samples.Length; i++)
            samples[i] = (float)Math.Sin(2.0 * Math.PI * i / samples.Length);

        var sut = new OscilloscopeRenderer();
        var inputs = new VisualizationInputs(samples.AsMemory(), Spectrum: null, SampleRateHz: 44100, Channels: 1);
        sut.Render(in inputs, frame);

        // Background must contain black pixels…
        Assert.Contains(frame.Pixels, p => p == 0);
        // …and the waveform must light up at least one green pixel.
        var hasGreen = false;
        for (var i = 0; i < frame.Pixels.Length; i += 4)
            if (frame.Pixels[i + 1] > 0 && frame.Pixels[i + 1] == 0xFF && frame.Pixels[i] == 0)
            { hasGreen = true; break; }
        Assert.True(hasGreen);
    }

    [Fact]
    public void SpectrumBars_FullScaleBin_PaintsToTopRow()
    {
        var frame = new RgbaFrame(64, 32);
        var mags = new float[16];
        // Strong magnitude across the whole band → tall bars.
        for (var i = 0; i < mags.Length; i++) mags[i] = 1.0f;
        var spectrum = new SpectrumFrame(mags, PeakAmplitude: 1.0f, SampleRateHz: 44100);

        var sut = new SpectrumBarsRenderer { BarCount = 8 };
        sut.Render(new VisualizationInputs(ReadOnlyMemory<float>.Empty, spectrum, 44100, 1), frame);

        // At least one pixel in the very top row must be coloured.
        var hasTopPixel = false;
        for (var x = 0; x < frame.Width; x++)
        {
            var i = (0 * frame.Width + x) * 4;
            if (frame.Pixels[i + 3] == 0xFF && (frame.Pixels[i] | frame.Pixels[i + 1] | frame.Pixels[i + 2]) != 0)
            { hasTopPixel = true; break; }
        }
        Assert.True(hasTopPixel);
    }

    [Fact]
    public void ClassicWinampBars_NoSpectrum_LeavesBackgroundOnly()
    {
        var frame = new RgbaFrame(40, 20);
        var sut = new ClassicWinampBarsRenderer();
        sut.Render(new VisualizationInputs(ReadOnlyMemory<float>.Empty, Spectrum: null, 44100, 1), frame);

        var bg = ClassicWinampBarsRenderer.DefaultPalette[0];
        // Every pixel must match the background colour.
        for (var i = 0; i < frame.Pixels.Length; i += 4)
        {
            Assert.Equal(bg.R, frame.Pixels[i]);
            Assert.Equal(bg.G, frame.Pixels[i + 1]);
            Assert.Equal(bg.B, frame.Pixels[i + 2]);
            Assert.Equal((byte)0xFF, frame.Pixels[i + 3]);
        }
    }
}
