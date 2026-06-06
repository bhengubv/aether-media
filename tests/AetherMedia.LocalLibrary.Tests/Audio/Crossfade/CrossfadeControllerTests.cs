// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Crossfade;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Crossfade;

public class CrossfadeControllerTests
{
    private const int SampleRate = 48000;

    [Fact]
    public void Off_Mode_FadingOut_GainsAreZero()
    {
        var fade = new CrossfadeController { Mode = CrossfadeMode.Off };
        var gains = new float[256];
        fade.ComputeGainRamp(0, 256, SampleRate, true, gains);
        Assert.All(gains, g => Assert.Equal(0f, g));
    }

    [Fact]
    public void Off_Mode_FadingIn_GainsAreOne()
    {
        var fade = new CrossfadeController { Mode = CrossfadeMode.Off };
        var gains = new float[256];
        fade.ComputeGainRamp(0, 256, SampleRate, false, gains);
        Assert.All(gains, g => Assert.Equal(1f, g));
    }

    [Fact]
    public void Crossfade_Mode_EqualPowerSumStaysNearOne()
    {
        var fade = new CrossfadeController
        {
            Mode = CrossfadeMode.Crossfade,
            FadeDurationMs = 4000,
        };
        var outgoing = new float[1024];
        var incoming = new float[1024];

        fade.ComputeGainRamp(2000, 1024, SampleRate, true,  outgoing);
        fade.ComputeGainRamp(2000, 1024, SampleRate, false, incoming);

        for (var i = 0; i < outgoing.Length; i++)
        {
            var energy = outgoing[i] * outgoing[i] + incoming[i] * incoming[i];
            Assert.InRange(energy, 0.99f, 1.01f);
        }
    }

    [Fact]
    public void Crossfade_StartAndEnd()
    {
        var fade = new CrossfadeController
        {
            Mode = CrossfadeMode.Crossfade,
            FadeDurationMs = 4000,
        };
        var gains = new float[2];

        fade.ComputeGainRamp(0,    2, SampleRate, true, gains);
        Assert.InRange(gains[0], 0.99f, 1.01f);

        fade.ComputeGainRamp(4000, 2, SampleRate, true, gains);
        Assert.InRange(gains[0], -0.01f, 0.01f);
    }

    [Fact]
    public void DefaultMode_IsGapless()
    {
        Assert.Equal(CrossfadeMode.Gapless, new CrossfadeController().Mode);
    }
}
