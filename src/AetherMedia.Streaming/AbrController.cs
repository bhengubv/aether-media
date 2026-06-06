// SPDX-License-Identifier: MIT

using AetherNet.Extensibility;
using AetherNet.Streaming;

namespace AetherMedia.Streaming;

/// <summary>
/// Adaptive Bitrate controller with EMA bandwidth tracking, a fixed bitrate
/// rung table, and optional CircleAI transport-bias integration.
///
/// <para>
/// <b>EMA update rule</b> (α = 0.3): <c>ema ← α × sample + (1 − α) × ema</c>.
/// When CircleAI is available the raw bandwidth sample is multiplied by the
/// AI transport-bias signal before entering the EMA.  This lets the AI
/// proactively steer the controller toward more reliable transports before
/// the EMA has caught up to a degrading link.
/// </para>
///
/// <para>
/// <b>AI bias integration:</b>
/// Transport biases are fetched from <see cref="IAetherNetAiProvider.GetTransportBiasesAsync"/>
/// at most once every <see cref="BiasRefreshInterval"/> (5 s) to avoid per-segment
/// overhead.  The average multiplier across all active transports is clamped to
/// [0.5, 1.5] so a misbehaving AI provider cannot force the rung to an extreme.
/// When the AI provider is unavailable the bias defaults to 1.0 (neutral).
/// </para>
///
/// <para>
/// <b>Rung table</b> (Kbps): 200 · 400 · 800 · 1 200 · 2 500 · 5 000.
/// </para>
///
/// <para>
/// <b>Rung selection:</b> pick the highest rung whose value is ≤ 0.8 × EMA
/// (20 % headroom for protocol overhead).
/// </para>
/// </summary>
public sealed class AbrController : IAbrController
{
    // ── Rung table ─────────────────────────────────────────────────────────
    private static readonly int[] Rungs = [200, 400, 800, 1_200, 2_500, 5_000];

    // ── EMA / rung parameters ──────────────────────────────────────────────
    private const double Alpha         = 0.3;
    private const double HeadroomFactor = 0.8;

    // ── AI bias parameters ────────────────────────────────────────────────
    /// <summary>
    /// How often transport biases are refreshed from CircleAI.
    /// Balances AI responsiveness against per-segment call overhead.
    /// </summary>
    private static readonly TimeSpan BiasRefreshInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Minimum and maximum values for the clamped AI bias multiplier.
    /// Prevents a misbehaving AI model from forcing rungs to extremes.
    /// </summary>
    private const double BiasMin = 0.5;
    private const double BiasMax = 1.5;

    // ── State ──────────────────────────────────────────────────────────────
    private double         _emaBandwidthKbps;
    private int            _currentBitrateKbps;
    private double         _cachedBiasMultiplier = 1.0;   // 1.0 = neutral
    private DateTimeOffset _biasLastRefreshed    = DateTimeOffset.MinValue;

    public int  CurrentBitrateKbps => _currentBitrateKbps;
    public bool IsAdapting         => true;

    // ── Dependencies ───────────────────────────────────────────────────────
    private readonly IStreamingService   _streaming;
    private readonly IAetherNetAiProvider?  _ai;

    /// <param name="streaming">
    /// Streaming service used to propagate bandwidth estimates and issue
    /// keyframe requests.
    /// </param>
    /// <param name="initialBandwidthKbps">
    /// Seed value for the EMA. Defaults to 10 000 Kbps (broadband assumption).
    /// </param>
    /// <param name="ai">
    /// Optional CircleAI provider. When <see langword="null"/> or
    /// <see cref="IAetherNetAiProvider.IsAvailable"/> is <c>false</c>,
    /// transport biases are not applied and the controller behaves as a
    /// pure-EMA ABR controller. This preserves the "magic potion" contract:
    /// AI enhances but never blocks.
    /// </param>
    public AbrController(
        IStreamingService  streaming,
        long               initialBandwidthKbps = 10_000,
        IAetherNetAiProvider? ai                   = null)
    {
        _streaming          = streaming ?? throw new ArgumentNullException(nameof(streaming));
        _ai                 = ai;
        _emaBandwidthKbps   = initialBandwidthKbps > 0 ? (double)initialBandwidthKbps : 10_000;
        _currentBitrateKbps = SelectRung(_emaBandwidthKbps);
    }

    // ── IAbrController ─────────────────────────────────────────────────────

    public async Task NotifyBandwidthAsync(
        Guid              streamId,
        long              bandwidthKbps,
        CancellationToken ct = default)
    {
        if (bandwidthKbps <= 0)
            return;

        // Refresh AI transport biases if the cache has expired.
        await RefreshBiasCacheIfStaleAsync(ct).ConfigureAwait(false);

        // Apply the AI bias to the raw sample before updating the EMA.
        // A bias > 1.0 means AI prefers the current transport → amplify the
        // sample; bias < 1.0 means AI is discouraging it → reduce the sample.
        double biasedSample = bandwidthKbps * _cachedBiasMultiplier;

        // EMA update with the (possibly AI-adjusted) sample.
        _emaBandwidthKbps   = Alpha * biasedSample + (1.0 - Alpha) * _emaBandwidthKbps;
        _currentBitrateKbps = SelectRung(_emaBandwidthKbps);

        _streaming.UpdateBandwidthEstimate(streamId, (long)_emaBandwidthKbps);
    }

    public Task RequestKeyframeAsync(Guid streamId, CancellationToken ct = default)
        => RequestKeyframeViaCycleAsync(streamId, ct);

    public Task<int> SelectBitrateRungAsync(
        Guid              streamId,
        long              availableBandwidthKbps,
        CancellationToken ct = default)
        => Task.FromResult(SelectRung(availableBandwidthKbps));

    // ── Private ────────────────────────────────────────────────────────────

    /// <summary>
    /// Refreshes <see cref="_cachedBiasMultiplier"/> from CircleAI when the
    /// cache is stale. No-op when AI is unavailable or not injected.
    /// </summary>
    private async Task RefreshBiasCacheIfStaleAsync(CancellationToken ct)
    {
        if (_ai is null || !_ai.IsAvailable)
        {
            _cachedBiasMultiplier = 1.0;
            return;
        }

        if (DateTimeOffset.UtcNow - _biasLastRefreshed <= BiasRefreshInterval)
            return;

        try
        {
            // Estimate segment size from the current rung: standard 2-second
            // HLS/DASH segment. The AI uses payload size to decide which
            // transports can serve this segment in time.
            int segmentBytes = EstimateSegmentBytes(_currentBitrateKbps);

            var biases = await _ai
                .GetTransportBiasesAsync(segmentBytes, ct)
                .ConfigureAwait(false);

            _cachedBiasMultiplier = ComputeBiasMultiplier(biases);
            _biasLastRefreshed    = DateTimeOffset.UtcNow;
        }
        catch
        {
            // Best-effort: fall back to neutral on any failure.
            _cachedBiasMultiplier = 1.0;
        }
    }

    /// <summary>
    /// Estimates the byte size of a 2-second segment at the given bitrate.
    /// Used as the <c>payloadBytes</c> hint for <see cref="IAetherNetAiProvider.GetTransportBiasesAsync"/>.
    /// </summary>
    private static int EstimateSegmentBytes(int bitrateKbps)
        // kbps × 1000 bits/kbit ÷ 8 bits/byte × 2 s/segment
        => (int)Math.Min((long)bitrateKbps * 1_000L / 8L * 2L, int.MaxValue);

    /// <summary>
    /// Reduces a transport-bias dictionary to a single multiplier clamped to
    /// [<see cref="BiasMin"/>, <see cref="BiasMax"/>].
    /// An empty dictionary (or neutral biases averaging to 1.0) returns 1.0.
    /// </summary>
    private static double ComputeBiasMultiplier(IReadOnlyDictionary<string, double> biases)
    {
        if (biases.Count == 0)
            return 1.0;

        double avg = biases.Values.Average();
        return Math.Clamp(avg, BiasMin, BiasMax);
    }

    /// <summary>
    /// Walk the rung table highest-to-lowest; return the first rung whose
    /// value fits within <paramref name="bandwidthKbps"/> × <see cref="HeadroomFactor"/>.
    /// Falls back to the floor rung (200 Kbps) when none fit.
    /// </summary>
    private static int SelectRung(double bandwidthKbps)
    {
        double effective = bandwidthKbps * HeadroomFactor;

        for (int i = Rungs.Length - 1; i >= 0; i--)
        {
            if (Rungs[i] <= effective)
                return Rungs[i];
        }

        return Rungs[0]; // floor
    }

    /// <summary>
    /// Request a keyframe by cycling the subscription (unsubscribe then
    /// immediately re-subscribe). Signals to the publisher that the next
    /// segment must begin at a keyframe.
    /// </summary>
    private async Task RequestKeyframeViaCycleAsync(Guid streamId, CancellationToken ct)
    {
        try
        {
            await _streaming.UnsubscribeAsync(streamId, ct).ConfigureAwait(false);
            await _streaming.SubscribeAsync(streamId, ct).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort — keyframe recovery is a hint; drop silently if
            // stream state is invalid.
        }
    }
}
