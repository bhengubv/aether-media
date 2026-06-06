// SPDX-License-Identifier: MIT

using AetherNet.Media.Streaming.Tests.Helpers;

namespace AetherNet.Media.Streaming.Tests;

/// <summary>
/// Unit tests for <see cref="AbrController"/>.
///
/// <para>
/// Rung table (Kbps): 200 · 400 · 800 · 1 200 · 2 500 · 5 000.
/// Headroom: pick highest rung ≤ 0.8 × EMA.
/// EMA rule: α=0.3 — ema ← 0.3 × sample + 0.7 × ema.
/// </para>
/// </summary>
public sealed class AbrControllerTests
{
    // ── Factory ────────────────────────────────────────────────────────────

    private static (AbrController Abr, FakeStreamingService Streaming) Make(long initialKbps = 10_000)
    {
        var streaming = new FakeStreamingService();
        var abr       = new AbrController(streaming, initialKbps);
        return (abr, streaming);
    }

    // ── Constructor ────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_HighInitialBandwidth_PicksHighestRung()
    {
        var (abr, _) = Make(initialKbps: 10_000);
        // 0.8 × 10_000 = 8_000 ≥ 5_000 → rung 5_000
        Assert.Equal(5_000, abr.CurrentBitrateKbps);
    }

    [Fact]
    public void Constructor_LowInitialBandwidth_PicksLowestRung()
    {
        var (abr, _) = Make(initialKbps: 100);
        // 0.8 × 100 = 80 < 200 → floor rung 200
        Assert.Equal(200, abr.CurrentBitrateKbps);
    }

    [Fact]
    public void Constructor_ZeroInitialBandwidth_DefaultsTo10Gbps()
    {
        var (abr, _) = Make(initialKbps: 0);
        // Falls back to 10_000 seed → picks rung 5_000
        Assert.Equal(5_000, abr.CurrentBitrateKbps);
    }

    [Fact]
    public void IsAdapting_AlwaysTrue()
    {
        var (abr, _) = Make();
        Assert.True(abr.IsAdapting);
    }

    // ── SelectBitrateRungAsync ─────────────────────────────────────────────

    [Theory]
    [InlineData(100,    200)]   // floor
    [InlineData(250,    200)]   // 0.8×250=200 → 200
    [InlineData(501,    400)]   // 0.8×501=400.8 → 400
    [InlineData(1_001,  800)]   // 0.8×1001=800.8 → 800
    [InlineData(1_501, 1_200)]  // 0.8×1501=1200.8 → 1200
    [InlineData(3_126, 2_500)]  // 0.8×3126=2500.8 → 2500
    [InlineData(6_251, 5_000)]  // 0.8×6251=5000.8 → 5000
    public async Task SelectBitrateRungAsync_VariousBandwidths_PicksCorrectRung(long bwKbps, int expectedRung)
    {
        var (abr, _) = Make();
        var rung = await abr.SelectBitrateRungAsync(Guid.NewGuid(), bwKbps);
        Assert.Equal(expectedRung, rung);
    }

    // ── NotifyBandwidthAsync — rung changes ────────────────────────────────

    [Fact]
    public async Task NotifyBandwidth_HighSample_EventuallyRaisesRung()
    {
        // Start on the floor (very low bandwidth seed)
        var (abr, _) = Make(initialKbps: 1);

        var streamId    = Guid.NewGuid();
        var initialRung = abr.CurrentBitrateKbps;

        // Feed repeated high-bandwidth samples; EMA should climb
        for (var i = 0; i < 20; i++)
            await abr.NotifyBandwidthAsync(streamId, 10_000);

        Assert.True(abr.CurrentBitrateKbps > initialRung,
            $"Expected rung to rise above {initialRung} but stayed at {abr.CurrentBitrateKbps}");
    }

    [Fact]
    public async Task NotifyBandwidth_LowSample_EventuallyLowersRung()
    {
        var (abr, _) = Make(initialKbps: 10_000); // starts at 5_000
        var initialRung = abr.CurrentBitrateKbps;

        var streamId = Guid.NewGuid();

        // Feed repeated very-low samples; EMA should drop
        for (var i = 0; i < 20; i++)
            await abr.NotifyBandwidthAsync(streamId, 50);

        Assert.True(abr.CurrentBitrateKbps < initialRung,
            $"Expected rung to drop below {initialRung} but stayed at {abr.CurrentBitrateKbps}");
    }

    [Fact]
    public async Task NotifyBandwidth_ThreeOrMoreRungChanges_OccurDuringDegradation()
    {
        // Start at broadband, drive bandwidth down; plan verification item 4:
        // "ABR adjusts within 3 rung changes"
        var (abr, _) = Make(initialKbps: 10_000);
        var streamId = Guid.NewGuid();

        var rungChanges = 0;
        var lastRung    = abr.CurrentBitrateKbps;

        // Push 60 very-low bandwidth samples (simulates sustained congestion)
        for (var i = 0; i < 60; i++)
        {
            await abr.NotifyBandwidthAsync(streamId, 80);
            if (abr.CurrentBitrateKbps != lastRung)
            {
                rungChanges++;
                lastRung = abr.CurrentBitrateKbps;
            }
        }

        Assert.True(rungChanges >= 3,
            $"Expected ≥ 3 rung changes during degradation but observed {rungChanges}");
    }

    [Fact]
    public async Task NotifyBandwidth_IgnoresZeroAndNegative()
    {
        var (abr, _) = Make(initialKbps: 1_000);
        var rungBefore = abr.CurrentBitrateKbps;

        await abr.NotifyBandwidthAsync(Guid.NewGuid(), 0);
        await abr.NotifyBandwidthAsync(Guid.NewGuid(), -100);

        // Rung must not change
        Assert.Equal(rungBefore, abr.CurrentBitrateKbps);
    }

    // ── NotifyBandwidthAsync — bandwidth propagation ───────────────────────

    [Fact]
    public async Task NotifyBandwidth_PropagatesEstimateToStreamingService()
    {
        var (abr, streaming) = Make();
        var streamId = Guid.NewGuid();

        await abr.NotifyBandwidthAsync(streamId, 2_000);

        Assert.NotEmpty(streaming.BandwidthEstimates);
        Assert.All(streaming.BandwidthEstimates, e => Assert.Equal(streamId, e.StreamId));
    }

    // ── RequestKeyframeAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RequestKeyframe_CyclesSubscription()
    {
        var (abr, streaming) = Make();
        var streamId = Guid.NewGuid();

        await abr.RequestKeyframeAsync(streamId);

        Assert.Contains(streamId, streaming.UnsubscribeCalls);
        Assert.Contains(streamId, streaming.SubscribeCalls);
    }
}
