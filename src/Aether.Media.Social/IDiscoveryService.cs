// SPDX-License-Identifier: MIT

using Aether.Media.Core.Models;

namespace Aether.Media.Social;

/// <summary>
/// Discovers nearby content creators and live streams by listening to mesh
/// handshake events.  A peer is classified as a potential creator when its
/// negotiated <c>NodeCapabilities</c> includes the <c>Streaming</c> flag.
/// </summary>
public interface IDiscoveryService
{
    /// <summary>Raised when a streaming-capable peer is discovered and its profile resolved.</summary>
    event EventHandler<MediaProfile>? CreatorDiscovered;

    /// <summary>Returns all creator profiles discovered since <see cref="StartAsync"/> was called.</summary>
    Task<IReadOnlyList<MediaProfile>> GetNearbyCreatorsAsync(CancellationToken ct = default);

    /// <summary>Returns all live streams currently active on the mesh (from any creator, not just followed).</summary>
    Task<IReadOnlyList<LiveStream>> GetActiveStreamsAsync(CancellationToken ct = default);

    /// <summary>Subscribe to handshake events and start building the discovered-creator set.</summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>Detach event handlers and stop discovery.</summary>
    Task StopAsync(CancellationToken ct = default);
}
