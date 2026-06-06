// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Effects;
using AetherMedia.LocalLibrary.Audio.Equalizer;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Equalizer;

public class BiquadEqualizerTests
{
    private const int SampleRate = 48000;

    [Fact]
    public void Flat_Preset_LeavesSignalEssentiallyUnchanged()
    {
        var input = new float[SampleRate];
        var omega = 2.0 * Math.PI * 1000 / SampleRate;
        for (var i = 0; i < input.Length; i++)
            input[i] = 0.5f * (float)Math.Sin(omega * i);

        var working = (float[])input.Clone();
        var eq = new BiquadEqualizer();
        eq.ApplyPreset(EqualizerPresets.Flat);
        eq.Process(working, SampleRate, channels: 1);

        var startSkip = SampleRate / 100;
        for (var i = startSkip; i < input.Length; i++)
            Assert.InRange(working[i] - input[i], -0.01f, 0.01f);
    }

    [Fact]
    public void BassBoost_Preset_IncreasesEnergyAtLowFrequency()
    {
        var input = new float[SampleRate];
        var omega = 2.0 * Math.PI * 60 / SampleRate;
        for (var i = 0; i < input.Length; i++)
            input[i] = 0.3f * (float)Math.Sin(omega * i);

        var working = (float[])input.Clone();
        var eq = new BiquadEqualizer();
        eq.ApplyPreset(EqualizerPresets.BassBoost);
        eq.Process(working, SampleRate, channels: 1);

        var startSkip = SampleRate / 10;
        var rmsIn  = Rms(input,   startSkip);
        var rmsOut = Rms(working, startSkip);
        Assert.True(rmsOut > rmsIn);
    }

    [Fact]
    public void Disabled_Equalizer_DoesNotMutateSamples_WhenInChain()
    {
        var input = new float[1024];
        for (var i = 0; i < input.Length; i++) input[i] = 0.5f;

        var working = (float[])input.Clone();
        var eq = new BiquadEqualizer { IsEnabled = false };
        eq.ApplyPreset(EqualizerPresets.BassBoost);

        var chain = new DspChain();
        chain.Add(eq);
        chain.Process(working, SampleRate, channels: 1);

        Assert.Equal(input, working);
    }

    [Fact]
    public void AvailablePresets_IncludesFlatAndBassBoost()
    {
        var eq = new BiquadEqualizer();
        Assert.Contains(EqualizerPresets.Flat, eq.AvailablePresets);
        Assert.Contains(EqualizerPresets.BassBoost, eq.AvailablePresets);
        Assert.Equal(8, eq.AvailablePresets.Count);
    }

    private static double Rms(float[] s, int startIndex)
    {
        double sumSq = 0;
        for (var i = startIndex; i < s.Length; i++) sumSq += s[i] * s[i];
        return Math.Sqrt(sumSq / (s.Length - startIndex));
    }
}
