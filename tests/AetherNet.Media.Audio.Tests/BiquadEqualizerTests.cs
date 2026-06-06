// SPDX-License-Identifier: MIT

using AetherNet.Media.Audio.Equalizer;
using Xunit;

namespace AetherNet.Media.Audio.Tests;

public class BiquadEqualizerTests
{
    private const int SampleRate = 48000;

    [Fact]
    public void Flat_Preset_LeavesSignalEssentiallyUnchanged()
    {
        // Flat = 0 dB on every band → output should match input within numerical precision.
        var input = new float[SampleRate]; // 1 s of audio
        var omega = 2.0 * Math.PI * 1000 / SampleRate;
        for (var i = 0; i < input.Length; i++)
            input[i] = 0.5f * (float)Math.Sin(omega * i);

        var working = (float[])input.Clone();
        var eq = new BiquadEqualizer();
        eq.ApplyPreset(EqualizerPresets.Flat);
        eq.Process(working, SampleRate, channels: 1);

        // Allow small numerical noise from the cascaded filters.
        // Skip the first 10 ms to let filter state settle.
        var startSkip = SampleRate / 100;
        for (var i = startSkip; i < input.Length; i++)
            Assert.InRange(working[i] - input[i], -0.01f, 0.01f);
    }

    [Fact]
    public void BassBoost_Preset_IncreasesEnergyAtLowFrequency()
    {
        // 60 Hz tone should get louder with bass boost.
        var input = new float[SampleRate];
        var omega = 2.0 * Math.PI * 60 / SampleRate;
        for (var i = 0; i < input.Length; i++)
            input[i] = 0.3f * (float)Math.Sin(omega * i);

        var working = (float[])input.Clone();
        var eq = new BiquadEqualizer();
        eq.ApplyPreset(EqualizerPresets.BassBoost);
        eq.Process(working, SampleRate, channels: 1);

        // RMS after filter > RMS before filter (skip startup).
        var startSkip = SampleRate / 10;
        var rmsIn  = Rms(input,   startSkip);
        var rmsOut = Rms(working, startSkip);
        Assert.True(rmsOut > rmsIn,
            $"Bass boost did not increase 60 Hz energy: rmsIn={rmsIn}, rmsOut={rmsOut}");
    }

    [Fact]
    public void Disabled_Equalizer_DoesNotMutateSamples()
    {
        var input = new float[1024];
        for (var i = 0; i < input.Length; i++) input[i] = 0.5f;

        var working = (float[])input.Clone();
        var eq = new BiquadEqualizer { IsEnabled = false };
        eq.ApplyPreset(EqualizerPresets.BassBoost);

        // Note: BiquadEqualizer.Process doesn't itself check IsEnabled; the
        // DspChain does. Here we just confirm the API contract: passing
        // through a chain with a disabled effect should leave samples alone.
        var chain = new AetherNet.Media.Audio.Effects.DspChain();
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
