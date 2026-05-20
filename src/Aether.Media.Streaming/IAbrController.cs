// SPDX-License-Identifier: MIT

namespace Aether.Media.Streaming;

/// <summary>
/// Adaptive Bitrate controller for a live stream consumer.
///
/// <para>
/// The controller maintains an exponential moving average (EMA) of observed
/// bandwidth (α = 0.3) and selects the highest bitrate rung that fits within
/// 80% of the EMA (20% headroom for protocol overhead).
/// </para>
/// <para>
/// Standard rung table (Kbps): 200, 400, 800, 1 200, 2 500, 5 000.
/// </para>
/// </summary>
public interface IAbrController
{
    /// <summary>The bitrate (Kbps) of the rung currently selected for this controller.</summary>
    int CurrentBitrateKbps { get; }

    /// <summary>True while the controller is tracking bandwidth and selecting rungs.</summary>
    bool IsAdapting { get; }

    /// <summary>
    /// Notify the controller of a new bandwidth measurement for <paramref name="streamId"/>.
    /// Updates the EMA and calls <c>IStreamingService.UpdateBandwidthEstimate</c>.
    /// </summary>
    Task NotifyBandwidthAsync(Guid streamId, long bandwidthKbps, CancellationToken ct = default);

    /// <summary>
    /// Ask the publisher to send a keyframe for <paramref name="streamId"/>
    /// (e.g. after packet loss or a bitrate rung downgrade).
    /// </summary>
    Task RequestKeyframeAsync(Guid streamId, CancellationToken ct = default);

    /// <summary>
    /// Pick the optimal bitrate rung (Kbps) given <paramref name="availableBandwidthKbps"/>.
    /// Uses the 80%-headroom rule: returns the highest rung whose value ≤ 0.8 × available.
    /// Returns the floor rung (200 Kbps) when no rung fits within headroom.
    /// </summary>
    Task<int> SelectBitrateRungAsync(Guid streamId, long availableBandwidthKbps, CancellationToken ct = default);
}
