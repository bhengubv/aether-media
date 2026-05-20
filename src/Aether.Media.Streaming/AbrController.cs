// SPDX-License-Identifier: MIT

using Aether.Streaming;

namespace Aether.Media.Streaming;

/// <summary>
/// Adaptive Bitrate controller with an EMA (exponential moving average) bandwidth
/// tracker and a fixed bitrate rung table.
///
/// <para>
/// EMA update rule (α = 0.3): <c>ema ← α × sample + (1 − α) × ema</c>.
/// Rung selection: pick the highest rung whose value is ≤ 0.8 × current EMA
/// (20% headroom).
/// </para>
///
/// <para>
/// Rung table (Kbps): 200 · 400 · 800 · 1 200 · 2 500 · 5 000.
/// </para>
/// </summary>
public sealed class AbrController : IAbrController
{
    // ── Constants ──────────────────────────────────────────────────────────

    // EMA smoothing factor
    private const double Alpha = 0.3;

    // Headroom factor: only pick a rung whose total ≤ this fraction of EMA
    private const double HeadroomFactor = 0.8;

    // Standard rung table in Kbps (ascending)
    private static readonly int[] Rungs = [200, 400, 800, 1_200, 2_500, 5_000];

    // ── State ──────────────────────────────────────────────────────────────
    private double _emaBandwidthKbps;
    private int _currentBitrateKbps;

    public int CurrentBitrateKbps => _currentBitrateKbps;
    public bool IsAdapting => true;

    // ── Dependencies ───────────────────────────────────────────────────────
    private readonly IStreamingService _streaming;

    /// <param name="streaming">The streaming service used to propagate bandwidth estimates and keyframe requests.</param>
    /// <param name="initialBandwidthKbps">Seed value for the EMA. Defaults to 10 000 Kbps (broadband assumption).</param>
    public AbrController(IStreamingService streaming, long initialBandwidthKbps = 10_000)
    {
        _streaming = streaming ?? throw new ArgumentNullException(nameof(streaming));

        _emaBandwidthKbps = initialBandwidthKbps > 0 ? (double)initialBandwidthKbps : 10_000;
        _currentBitrateKbps = SelectRung(_emaBandwidthKbps);
    }

    // ── IAbrController ─────────────────────────────────────────────────────

    public Task NotifyBandwidthAsync(Guid streamId, long bandwidthKbps, CancellationToken ct = default)
    {
        if (bandwidthKbps <= 0)
            return Task.CompletedTask;

        // EMA update
        _emaBandwidthKbps = Alpha * bandwidthKbps + (1.0 - Alpha) * _emaBandwidthKbps;
        _currentBitrateKbps = SelectRung(_emaBandwidthKbps);

        // Propagate estimate to the streaming layer (best-effort; returns bool, not Task)
        _streaming.UpdateBandwidthEstimate(streamId, (long)_emaBandwidthKbps);

        return Task.CompletedTask;
    }

    public Task RequestKeyframeAsync(Guid streamId, CancellationToken ct = default)
    {
        // IStreamingService does not expose a direct keyframe request on the publisher side;
        // we subscribe to the stream and unsubscribe/re-subscribe to force a keyframe delivery.
        // This is the standard ABR keyframe recovery mechanism for server-push streams.
        return RequestKeyframeViaCycleAsync(streamId, ct);
    }

    public Task<int> SelectBitrateRungAsync(
        Guid streamId,
        long availableBandwidthKbps,
        CancellationToken ct = default)
    {
        var rung = SelectRung(availableBandwidthKbps);
        return Task.FromResult(rung);
    }

    // ── Private ────────────────────────────────────────────────────────────

    /// <summary>
    /// Walk the rung table from highest to lowest; return the first rung whose
    /// value fits within <paramref name="bandwidthKbps"/> × <see cref="HeadroomFactor"/>.
    /// Falls back to the floor rung (200 Kbps) if none fit.
    /// </summary>
    private static int SelectRung(double bandwidthKbps)
    {
        var effective = bandwidthKbps * HeadroomFactor;

        for (var i = Rungs.Length - 1; i >= 0; i--)
        {
            if (Rungs[i] <= effective)
                return Rungs[i];
        }

        return Rungs[0]; // floor
    }

    /// <summary>
    /// Request a keyframe by cycling the subscription (unsubscribe then immediately
    /// re-subscribe).  This signals to the publisher that the next segment should
    /// begin at a keyframe.
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
            // Best-effort — keyframe recovery is a hint; drop silently if stream state is invalid
        }
    }
}
