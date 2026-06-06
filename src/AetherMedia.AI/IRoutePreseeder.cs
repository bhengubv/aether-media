// SPDX-License-Identifier: MIT

using AetherMedia.Core.Models;

namespace AetherMedia.AI;

/// <summary>
/// Pre-warms AODV routing-table entries for the creators most likely to be
/// accessed next, so that content requests can resolve routes from cache
/// instead of waiting for a full AODV flood.
///
/// <para>
/// When <see cref="CircleAI"/> is available, creator candidates are ranked by
/// the AI confidence returned from
/// <see cref="AetherNet.Extensibility.IAetherNetAiProvider.SuggestRoutesAsync"/>.
/// When the AI is unavailable the top-<c>N</c> creators (by feed position) are
/// pre-warmed directly — the AI enhances, never blocks.
/// </para>
///
/// <para>
/// All methods are fire-and-forget in spirit: failures are swallowed internally
/// and never surface to the caller.  A missed preseed is a latency miss, not an
/// error.
/// </para>
/// </summary>
public interface IRoutePreseeder
{
    /// <summary>
    /// Initiates background route pre-warming for the distinct creators that
    /// appear in <paramref name="items"/>.
    ///
    /// <para>
    /// Up to <c>MaxPreseedCount</c> (10) routes are warmed per call, prioritised
    /// by AI confidence when CircleAI is available. An empty list is a no-op.
    /// </para>
    /// </summary>
    /// <param name="items">Ranked feed items whose creators should be pre-warmed.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PreseedFeedRoutesAsync(
        IReadOnlyList<MediaFeedItem> items,
        CancellationToken ct = default);
}
