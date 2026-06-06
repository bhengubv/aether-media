// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Effects;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Effects;

public class DspChainTests
{
    private sealed class CountingEffect : IDspEffect
    {
        public int Calls;
        public string Id { get; set; } = "count";
        public string DisplayName => "Counting Effect";
        public bool IsEnabled { get; set; } = true;
        public void Process(Span<float> _, int __, int ___) => Calls++;
    }

    [Fact]
    public void Add_Then_Process_RunsInOrder()
    {
        var chain = new DspChain();
        var a = new CountingEffect { Id = "a" };
        var b = new CountingEffect { Id = "b" };
        chain.Add(a); chain.Add(b);
        var samples = new float[4];
        chain.Process(samples, 48000, 1);
        Assert.Equal(1, a.Calls);
        Assert.Equal(1, b.Calls);
    }

    [Fact]
    public void Disabled_Effect_NotInvoked()
    {
        var chain = new DspChain();
        var a = new CountingEffect { Id = "a", IsEnabled = false };
        chain.Add(a);
        chain.Process(new float[2], 48000, 1);
        Assert.Equal(0, a.Calls);
    }

    [Fact]
    public void Remove_ById_Works()
    {
        var chain = new DspChain();
        chain.Add(new CountingEffect { Id = "a" });
        chain.Add(new CountingEffect { Id = "b" });
        Assert.True(chain.Remove("a"));
        Assert.Single(chain.Effects);
        Assert.Equal("b", chain.Effects[0].Id);
    }

    [Fact]
    public void Reorder_MovesEffect()
    {
        var chain = new DspChain();
        chain.Add(new CountingEffect { Id = "a" });
        chain.Add(new CountingEffect { Id = "b" });
        chain.Add(new CountingEffect { Id = "c" });
        chain.Reorder("c", 0);
        Assert.Equal(["c", "a", "b"], chain.Effects.Select(e => e.Id));
    }

    [Fact]
    public void NormalizationEffect_AppliesGain()
    {
        var samples = new float[] { 0.5f, -0.25f, 1.0f };
        new NormalizationEffect { Gain = 0.5 }.Process(samples, 48000, 1);
        Assert.Equal(0.25f, samples[0], 5);
        Assert.Equal(-0.125f, samples[1], 5);
        Assert.Equal(0.5f, samples[2], 5);
    }

    [Fact]
    public void SafeClipEffect_NeverExceedsCeiling()
    {
        var clip = new SafeClipEffect { CeilingDbfs = -0.3 };
        var ceiling = (float)Math.Pow(10.0, -0.3 / 20.0);
        var samples = new float[100];
        for (var i = 0; i < samples.Length; i++) samples[i] = (i % 2 == 0) ? 2.0f : -2.0f;
        clip.Process(samples, 48000, 1);
        foreach (var s in samples)
            Assert.InRange(s, -ceiling, ceiling);
    }
}
