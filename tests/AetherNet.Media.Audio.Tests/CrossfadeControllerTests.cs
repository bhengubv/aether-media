// SPDX-License-Identifier: MIT

using AetherNet.Media.Audio.Crossfade;
using Xunit;

namespace AetherNet.Media.Audio.Tests;

public class CrossfadeControllerTests
{
    private const int SampleRate = 48000;

    [Fact]
    public void Off_Mode_FadingOut_GainsAreZero()
    {
        var fade = new CrossfadeController { Mode = CrossfadeMode.Off };
        var gains = new float[256];
        fade.ComputeGainRamp(positionMs: 0, sampleCount: 256, SampleRate, fadingOut: true, gains);
        Assert.All(gains, g => Assert.Equal(0f, g));
    }

    [Fact]
    public void Off_Mode_FadingIn_GainsAreOne()
    {
        var fade = new CrossfadeController { Mode = CrossfadeMode.Off };
        var gains = new float[256];
        fade.ComputeGainRamp(0, 256, SampleRate, fadingOut: false, gains);
        Assert.All(gains, g => Assert.Equal(1f, g));
    }

    [Fact]
    public void Crossfade_Mode_EqualPowerSumStaysNearOne()
    {
        // The equal-power S-curve property: sin²(t·π/2) + cos²(t·π/2) = 1.
        var fade = new CrossfadeController
        {
            Mode = CrossfadeMode.Crossfade,
            FadeDurationMs = 4000,
        };
        var outgoing = new float[1024];
        var incoming = new float[1024];

        // Sample the middle of the fade window
        fade.ComputeGainRamp(positionMs: 2000, sampleCount: 1024, SampleRate, fadingOut: true,  outgoing);
        fade.ComputeGainRamp(positionMs: 2000, sampleCount: 1024, SampleRate, fadingOut: false, incoming);

        for (var i = 0; i < outgoing.Length; i++)
        {
            var energy = outgoing[i] * outgoing[i] + incoming[i] * incoming[i];
            Assert.InRange(energy, 0.99f, 1.01f);
        }
    }

    [Fact]
    public void Crossfade_Mode_OutgoingStartsAtOneEndsAtZero()
    {
        var fade = new CrossfadeController
        {
            Mode = CrossfadeMode.Crossfade,
            FadeDurationMs = 4000,
        };
        var gains = new float[2];

        // Beginning of fade
        fade.ComputeGainRamp(positionMs: 0, sampleCount: 2, SampleRate, fadingOut: true, gains);
        Assert.InRange(gains[0], 0.99f, 1.01f);

        // End of fade
        fade.ComputeGainRamp(positionMs: 4000, sampleCount: 2, SampleRate, fadingOut: true, gains);
        Assert.InRange(gains[0], -0.01f, 0.01f);
    }

    [Fact]
    public void DefaultMode_IsGapless()
    {
        var fade = new CrossfadeController();
        Assert.Equal(CrossfadeMode.Gapless, fade.Mode);
    }
}
