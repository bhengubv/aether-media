// SPDX-License-Identifier: MIT

using Aether.Media.Streaming.Tests.Helpers;

namespace Aether.Media.Streaming.Tests;

/// <summary>
/// Unit tests for the AI transport-bias integration in <see cref="AbrController"/>.
///
/// <para>
/// The AI bias multiplier is applied to the raw bandwidth sample before the EMA
/// update: <c>biasedSample = bandwidth × bias</c>.  This means:
/// <list type="bullet">
///   <item>bias > 1.0 → inflated sample → EMA climbs faster → higher rung</item>
///   <item>bias &lt; 1.0 → deflated sample → EMA drops faster → lower rung</item>
///   <item>bias = 1.0 (neutral / AI unavailable) → pure-EMA behaviour</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Test arithmetic setup</b> (Kbps, headroom = 0.8, α = 0.3):
/// <list type="number">
///   <item>Start at initialBandwidth = 1000 → EMA = 1000 → 0.8×1000 = 800 → rung = 800.</item>
///   <item>Send sample = 2000 with bias 1.5:
///         biasedSample = 3000; EMA = 0.3×3000 + 0.7×1000 = 1600 → 0.8×1600 = 1280 → rung = 1200 (↑).</item>
///   <item>Send sample = 1000 with bias 0.5:
///         biasedSample = 500; EMA = 0.3×500 + 0.7×1000 = 850 → 0.8×850 = 680 → rung = 400 (↓).</item>
/// </list>
/// </para>
/// </summary>
public sealed class AbrControllerAiTests
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private static (AbrController Abr, FakeStreamingService Streaming, FakeAiProvider Ai)
        Make(long initialKbps = 1_000)
    {
        var streaming = new FakeStreamingService();
        var ai        = new FakeAiProvider();
        var abr       = new AbrController(streaming, initialKbps, ai);
        return (abr, streaming, ai);
    }

    // ── No AI — pure EMA ──────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithoutAi_InitialisesNormally()
    {
        var streaming = new FakeStreamingService();
        var abr       = new AbrController(streaming, initialBandwidthKbps: 1_000);

        Assert.Equal(800, abr.CurrentBitrateKbps); // 0.8 × 1000 = 800 ≥ 800
    }

    [Fact]
    public async Task NoAi_NormalEma_RungBehavesAsExpected()
    {
        var streaming = new FakeStreamingService();
        var abr       = new AbrController(streaming, initialBandwidthKbps: 1_000);
        var streamId  = Guid.NewGuid();

        // No bias → EMA = 0.3×2000 + 0.7×1000 = 1300 → 0.8×1300 = 1040 → rung stays 800
        await abr.NotifyBandwidthAsync(streamId, 2_000);

        Assert.Equal(800, abr.CurrentBitrateKbps);
    }

    // ── AI unavailable → neutral (bias = 1.0) ─────────────────────────────

    [Fact]
    public async Task AiUnavailable_BehavesLikeNoAi()
    {
        var (abr, _, ai) = Make(initialKbps: 1_000);
        ai.Available     = false;
        var streamId     = Guid.NewGuid();

        // Same as no-AI: EMA = 1300 → rung = 800
        await abr.NotifyBandwidthAsync(streamId, 2_000);

        Assert.Equal(800, abr.CurrentBitrateKbps);
    }

    // ── High bias elevates the rung ───────────────────────────────────────

    [Fact]
    public async Task HighBias_ElevatesRungAboveNoBiasResult()
    {
        // bias = 1.5 → biasedSample = 3000
        // EMA = 0.3×3000 + 0.7×1000 = 1600 → 0.8×1600 = 1280 → rung = 1200
        var (abr, _, ai) = Make(initialKbps: 1_000);
        ai.TransportBiases["BLE"] = 1.5;

        await abr.NotifyBandwidthAsync(Guid.NewGuid(), 2_000);

        Assert.Equal(1_200, abr.CurrentBitrateKbps);
    }

    // ── Low bias depresses the rung ────────────────────────────────────────

    [Fact]
    public async Task LowBias_DepressesRungBelowNoBiasResult()
    {
        // bias = 0.5 → biasedSample = 500
        // EMA = 0.3×500 + 0.7×1000 = 850 → 0.8×850 = 680 → rung = 400
        var (abr, _, ai) = Make(initialKbps: 1_000);
        ai.TransportBiases["BLE"] = 0.5;

        await abr.NotifyBandwidthAsync(Guid.NewGuid(), 1_000);

        Assert.Equal(400, abr.CurrentBitrateKbps);
    }

    // ── Bias is clamped to [0.5, 1.5] ─────────────────────────────────────

    [Fact]
    public async Task ExtremeHighBias_ClampedToOnePointFive()
    {
        // bias dictionary average = 10.0, clamped to 1.5
        // biasedSample = 2000 × 1.5 = 3000
        // EMA = 0.3×3000 + 0.7×1000 = 1600 → rung = 1200
        var (abr, _, ai) = Make(initialKbps: 1_000);
        ai.TransportBiases["BLE"] = 10.0;

        await abr.NotifyBandwidthAsync(Guid.NewGuid(), 2_000);

        // Same result as bias = 1.5: rung = 1200
        Assert.Equal(1_200, abr.CurrentBitrateKbps);
    }

    [Fact]
    public async Task ExtremeLowBias_ClampedToZeroPointFive()
    {
        // bias dictionary average = 0.01, clamped to 0.5
        // biasedSample = 1000 × 0.5 = 500
        // EMA = 0.3×500 + 0.7×1000 = 850 → rung = 400
        var (abr, _, ai) = Make(initialKbps: 1_000);
        ai.TransportBiases["BLE"] = 0.01;

        await abr.NotifyBandwidthAsync(Guid.NewGuid(), 1_000);

        Assert.Equal(400, abr.CurrentBitrateKbps);
    }

    // ── Empty bias dictionary → neutral (1.0) ─────────────────────────────

    [Fact]
    public async Task EmptyBiasDictionary_TreatedAsNeutral()
    {
        // TransportBiases is empty → bias = 1.0 (neutral)
        // EMA = 0.3×2000 + 0.7×1000 = 1300 → rung = 800
        var (abr, _, _) = Make(initialKbps: 1_000);
        // No entries added to TransportBiases → neutral

        await abr.NotifyBandwidthAsync(Guid.NewGuid(), 2_000);

        Assert.Equal(800, abr.CurrentBitrateKbps);
    }

    // ── Multiple transport biases are averaged ─────────────────────────────

    [Fact]
    public async Task MultipleTransports_AveragesBeforeClamping()
    {
        // avg([1.4, 1.6]) = 1.5 → clamp(1.5, 0.5, 1.5) = 1.5
        // Same result as single bias of 1.5: rung = 1200
        var (abr, _, ai) = Make(initialKbps: 1_000);
        ai.TransportBiases["BLE"]  = 1.4;
        ai.TransportBiases["WiFi"] = 1.6;

        await abr.NotifyBandwidthAsync(Guid.NewGuid(), 2_000);

        Assert.Equal(1_200, abr.CurrentBitrateKbps);
    }

    // ── Zero / negative bandwidth ignored ─────────────────────────────────

    [Fact]
    public async Task WithAi_ZeroBandwidth_Ignored()
    {
        var (abr, _, ai) = Make(initialKbps: 1_000);
        ai.TransportBiases["BLE"] = 1.5;
        var rungBefore = abr.CurrentBitrateKbps;

        await abr.NotifyBandwidthAsync(Guid.NewGuid(), 0);
        await abr.NotifyBandwidthAsync(Guid.NewGuid(), -500);

        Assert.Equal(rungBefore, abr.CurrentBitrateKbps);
    }

    // ── AI exception falls back to neutral ───────────────────────────────
    // (Tested indirectly: even if GetTransportBiasesAsync throws, the
    //  AbrController must complete NotifyBandwidthAsync without propagating.)

    [Fact]
    public async Task AiException_FallsBackToNeutral_DoesNotThrow()
    {
        var streaming = new FakeStreamingService();
        var ai        = new ThrowingAiProvider();
        var abr       = new AbrController(streaming, initialBandwidthKbps: 1_000, ai: ai);

        // Should not throw even though the AI throws
        await abr.NotifyBandwidthAsync(Guid.NewGuid(), 2_000);

        // Neutral bias: EMA = 1300 → rung = 800
        Assert.Equal(800, abr.CurrentBitrateKbps);
    }
}
