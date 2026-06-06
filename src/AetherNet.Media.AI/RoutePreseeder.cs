// SPDX-License-Identifier: MIT

using AetherNet.Extensibility;
using AetherNet.Media.Core.Models;
using AetherNet.Routing;

namespace AetherNet.Media.AI;

/// <summary>
/// Uses <see cref="IAetherNetAiProvider.SuggestRoutesAsync"/> to identify the
/// most-confident routes and then calls
/// <see cref="IRoutingService.FindRouteAsync"/> to pre-populate the AODV
/// routing cache before the user requests the content.
///
/// <para>
/// <b>Algorithm:</b>
/// <list type="number">
///   <item>Deduplicate creator UHIDs from the feed batch.</item>
///   <item>
///     When AI is available, call <c>SuggestRoutesAsync</c> per creator and
///     collect the top-confidence score for each.  Creators above
///     <see cref="MinConfidenceThreshold"/> (0.6) are selected, sorted by
///     confidence descending.  If no creator clears the threshold, fall back to
///     the feed-order list (same as the no-AI path).
///   </item>
///   <item>
///     When AI is unavailable, use all distinct creators in feed order.
///   </item>
///   <item>
///     Cap the final candidate list at <see cref="MaxPreseedCount"/> (10) and
///     fire <c>FindRouteAsync</c> for each in parallel.  Every routing call is
///     best-effort — failures are swallowed.
///   </item>
/// </list>
/// </para>
///
/// <para>
/// <b>Magic-potion rule:</b> if <see cref="IRoutingService"/> was not injected
/// (e.g. in a test or desktop-only context), every call is a silent no-op.
/// </para>
/// </summary>
public sealed class RoutePreseeder : IRoutePreseeder
{
    // ── Configuration ──────────────────────────────────────────────────────

    /// <summary>
    /// Minimum AI confidence score for a creator to be included in the
    /// AI-prioritised candidate list.  Creators below this threshold are skipped
    /// unless the AI produces no qualifying candidates at all.
    /// </summary>
    private const double MinConfidenceThreshold = 0.6;

    /// <summary>
    /// Maximum number of routes pre-warmed per <see cref="PreseedFeedRoutesAsync"/>
    /// call, preventing excessive AODV traffic on constrained mesh links.
    /// </summary>
    private const int MaxPreseedCount = 10;

    /// <summary>
    /// Payload hint passed to <see cref="IAetherNetAiProvider.SuggestRoutesAsync"/>
    /// during feed-level probing.  1 KiB represents a lightweight presence probe —
    /// not the actual media payload — so the AI considers general path availability
    /// rather than high-bandwidth capacity.
    /// </summary>
    private const int ProbePayloadBytes = 1_024;

    // ── Dependencies ───────────────────────────────────────────────────────
    private readonly IAetherNetAiProvider _ai;
    private readonly IRoutingService?  _routing;   // null = no-op (magic-potion)

    public RoutePreseeder(IAetherNetAiProvider ai, IRoutingService? routing = null)
    {
        _ai      = ai      ?? throw new ArgumentNullException(nameof(ai));
        _routing = routing;
    }

    // ── IRoutePreseeder ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task PreseedFeedRoutesAsync(
        IReadOnlyList<MediaFeedItem> items,
        CancellationToken ct = default)
    {
        if (_routing is null || items.Count == 0)
            return;

        // 1. Deduplicate creator UHIDs, preserving feed order.
        var creators = items
            .Select(i => i.Content.CreatorUhid)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (creators.Count == 0)
            return;

        // 2. Determine candidates based on AI availability.
        IReadOnlyList<string> candidates = await SelectCandidatesAsync(creators, ct)
            .ConfigureAwait(false);

        // 3. Warm the route cache in parallel (best-effort).
        await Task.WhenAll(
            candidates.Select(uhid => WarmRouteAsync(uhid, ct))
        ).ConfigureAwait(false);
    }

    // ── Private ────────────────────────────────────────────────────────────

    /// <summary>
    /// Selects up to <see cref="MaxPreseedCount"/> creator UHIDs to warm.
    /// Uses AI confidence when available; falls back to feed order otherwise.
    /// </summary>
    private async Task<IReadOnlyList<string>> SelectCandidatesAsync(
        List<string> creators,
        CancellationToken ct)
    {
        if (!_ai.IsAvailable)
            return creators.Take(MaxPreseedCount).ToList();

        var scored = new List<(string Uhid, double TopConfidence)>(creators.Count);

        foreach (var uhid in creators)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var suggestions = await _ai
                    .SuggestRoutesAsync(uhid, ProbePayloadBytes, ct)
                    .ConfigureAwait(false);

                double top = suggestions.Count > 0
                    ? suggestions.Max(s => s.Confidence)
                    : 0.0;

                if (top >= MinConfidenceThreshold)
                    scored.Add((uhid, top));
            }
            catch
            {
                // Best-effort — skip this creator; others still processed.
            }
        }

        // If no creator cleared the threshold (all exceptions or all low confidence),
        // fall back to feed order so pre-warming still happens.
        if (scored.Count == 0)
            return creators.Take(MaxPreseedCount).ToList();

        return scored
            .OrderByDescending(s => s.TopConfidence)
            .Take(MaxPreseedCount)
            .Select(s => s.Uhid)
            .ToList();
    }

    /// <summary>
    /// Triggers AODV discovery for a single creator UHID and returns when the
    /// routing table entry is cached.  All exceptions are swallowed — a missed
    /// pre-warm is a latency miss, not a failure.
    /// </summary>
    private async Task WarmRouteAsync(string uhid, CancellationToken ct)
    {
        try
        {
            await _routing!.FindRouteAsync(uhid, ct).ConfigureAwait(false);
        }
        catch
        {
            // Intentionally swallowed — pre-warming is best-effort.
        }
    }
}
