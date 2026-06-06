// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Visualization;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Visualization;

public class FftAnalyzerTests
{
    private const int SampleRate = 48000;

    [Fact]
    public void Sine_At_1kHz_PeaksAtBinFor_1kHz()
    {
        var fft = new FftAnalyzer(2048);
        var samples = new float[2048];
        var freq = 1000.0;
        var omega = 2.0 * Math.PI * freq / SampleRate;
        for (var i = 0; i < samples.Length; i++)
            samples[i] = 0.7f * (float)Math.Sin(omega * i);

        var frame = fft.Analyse(samples, SampleRate, channels: 1);
        Assert.NotNull(frame);

        // Find the peak bin
        var peakBin = 0;
        var peakMag = 0f;
        for (var k = 1; k < frame!.Magnitudes.Length; k++)
        {
            if (frame.Magnitudes[k] > peakMag) { peakMag = frame.Magnitudes[k]; peakBin = k; }
        }
        var peakFreq = frame.BinFrequencyHz(peakBin);

        // Bin resolution at 48 kHz / 2048 = 23.4 Hz; tolerate ± 2 bins
        Assert.InRange(peakFreq, freq - 50, freq + 50);
    }

    [Fact]
    public void Returns_Null_WhenBufferTooSmall()
    {
        var fft = new FftAnalyzer(2048);
        var samples = new float[100];
        Assert.Null(fft.Analyse(samples, SampleRate, channels: 1));
    }

    [Fact]
    public void Throws_OnNonPowerOfTwo()
    {
        Assert.Throws<ArgumentException>(() => new FftAnalyzer(1500));
    }
}
