// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Effects;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Effects;

public class PreampCrossfeedKaraokeTests
{
    [Fact]
    public void Preamp_AttenuatesEvery_Sample_ByConfiguredGain()
    {
        var fx = new PreampEffect { GainDb = -6.0206 }; // ~0.5x
        var samples = new float[] { 1.0f, -0.5f, 0.25f, -0.125f };
        fx.Process(samples, 44100, 1);
        Assert.Equal(0.5f, samples[0], 3);
        Assert.Equal(-0.25f, samples[1], 3);
    }

    [Fact]
    public void Preamp_Disabled_PassesThroughUnchanged()
    {
        var fx = new PreampEffect { GainDb = 12, IsEnabled = false };
        var samples = new float[] { 0.1f, 0.2f };
        fx.Process(samples, 44100, 1);
        Assert.Equal(0.1f, samples[0]);
        Assert.Equal(0.2f, samples[1]);
    }

    [Fact]
    public void Crossfeed_BleedsLowFrequencies_BetweenChannels()
    {
        var fx = new BauerCrossfeedEffect { FeedAmount = 0.5 };
        // Stereo: L = 1.0, R = -1.0 — extreme stereo separation.
        var samples = new float[200];
        for (var i = 0; i < samples.Length; i += 2)
        {
            samples[i] = 1.0f;
            samples[i + 1] = -1.0f;
        }
        fx.Process(samples, 44100, 2);

        // After the low-pass + bleed, the absolute value of the magnitude must
        // decrease — both channels should move toward each other.
        Assert.True(Math.Abs(samples[^2]) < 1.0f);
        Assert.True(Math.Abs(samples[^1]) < 1.0f);
    }

    [Fact]
    public void Karaoke_CancelsCentre_AboveBypassCutoff()
    {
        var fx = new KaraokeEffect { IsEnabled = true, LowBypassHz = 150.0 };
        // Identical L/R at 1 kHz = centre signal well above the low-bypass
        // cutoff. Should be largely cancelled after the low-pass has settled
        // away from the high-frequency path.
        const int sampleRate = 44100;
        var frames = sampleRate / 2; // 0.5 s of audio
        var samples = new float[frames * 2];
        for (var i = 0; i < frames; i++)
        {
            var s = (float)Math.Sin(2.0 * Math.PI * 1000.0 * i / sampleRate);
            samples[i * 2]     = s;
            samples[i * 2 + 1] = s;
        }
        fx.Process(samples, sampleRate, 2);

        // Late in the buffer the magnitude of the output must be well under
        // the original 1.0 — the centre has been cancelled.
        var lateRms = 0.0;
        for (var i = samples.Length - 2000; i < samples.Length; i++)
            lateRms += samples[i] * samples[i];
        lateRms = Math.Sqrt(lateRms / 2000);
        Assert.True(lateRms < 0.2, $"late-buffer RMS should be near zero; was {lateRms}");
    }

    [Fact]
    public void Karaoke_Disabled_PassesThroughUnchanged()
    {
        var fx = new KaraokeEffect { IsEnabled = false };
        var samples = new float[] { 0.5f, 0.5f, -0.25f, -0.25f };
        fx.Process(samples, 44100, 2);
        Assert.Equal(0.5f, samples[0]);
        Assert.Equal(-0.25f, samples[3]);
    }
}
