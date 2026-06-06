// SPDX-License-Identifier: MIT

using AetherNet.Extensibility;
using AetherNet.Media.Core.Models;
using AetherNet.Reputation;

namespace AetherNet.Media.AI;

/// <summary>
/// Ranks a feed of <see cref="MediaFeedItem"/> values using a five-signal
/// composite score:
/// <list type="bullet">
///   <item><description>Reputation    (30 %) — creator's node reputation score [0, 1].</description></item>
///   <item><description>AI bias       (20 %) — AI transport-bias signal; neutral 0.5 when AI unavailable.</description></item>
///   <item><description>Recency       (20 %) — exponential decay over 48 hours.</description></item>
///   <item><description>Engagement    (15 %) — weighted reaction counts, clamped to [0, 1].</description></item>
///   <item><description>Watch history (15 %) — EWMA completion rate from <see cref="IWatchHistoryStore"/>;
///         neutral 0.5 when no history exists for the viewer/content pair.</description></item>
/// </list>
///
/// <para>
/// Items whose creator is assessed as <see cref="AiThreatLevel.High"/> or above
/// are unconditionally assigned a composite score of 0 and sorted to the bottom.
/// </para>
///
/// <para>
/// The watch-history signal personalises ranking for a specific viewer.
/// Content the viewer has previously watched to completion scores higher;
/// content the viewer skipped immediately scores lower. Content the viewer
/// has never seen is treated as neutral (0.5) so it is not disadvantaged
/// against historically strong items.
/// </para>
/// </summary>
public sealed class ContentRanker : IContentRanker
{
    // ── Score weights (must sum to 1.0) ────────────────────────────────────
    private const double ReputationWeight    = 0.30;
    private const double AiWeight            = 0.20;
    private const double RecencyWeight       = 0.20;
    private const double EngagementWeight    = 0.15;
    private const double WatchHistoryWeight  = 0.15;

    // ── Recency half-life ──────────────────────────────────────────────────
    /// <summary>
    /// Time constant for the recency exponential decay.
    /// At 48 h the score reaches e^−1 ≈ 0.368; beyond that it approaches 0.
    /// </summary>
    private const double RecencyDecayHours = 48.0;

    // ── Dependencies ───────────────────────────────────────────────────────
    private readonly INodeReputationService _reputation;
    private readonly IAetherNetAiProvider      _ai;
    private readonly IContentModerator      _moderator;
    private readonly IWatchHistoryStore     _history;

    public ContentRanker(
        INodeReputationService reputation,
        IAetherNetAiProvider      ai,
        IContentModerator      moderator,
        IWatchHistoryStore     history)
    {
        _reputation = reputation ?? throw new ArgumentNullException(nameof(reputation));
        _ai         = ai         ?? throw new ArgumentNullException(nameof(ai));
        _moderator  = moderator  ?? throw new ArgumentNullException(nameof(moderator));
        _history    = history    ?? throw new ArgumentNullException(nameof(history));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MediaFeedItem>> RankFeedAsync(
        IReadOnlyList<MediaFeedItem> items,
        string viewerUhid,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
            return Array.Empty<MediaFeedItem>();

        // Pre-fetch AI transport biases once for the whole batch.
        // Pass 0 as a neutral payload-size probe — we want the overall
        // AI transport disposition, not a per-content-size estimate.
        IReadOnlyDictionary<string, double> transportBiases =
            _ai.IsAvailable
                ? await _ai.GetTransportBiasesAsync(0, ct).ConfigureAwait(false)
                : new Dictionary<string, double>();

        double aiSignal = ComputeAiSignal(transportBiases);

        var scored = await ScoreAllAsync(items, viewerUhid, aiSignal, ct).ConfigureAwait(false);

        return scored
            .OrderByDescending(pair => pair.Score)
            .Select(pair => pair.Item)
            .ToList()
            .AsReadOnly();
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task<List<(MediaFeedItem Item, double Score)>> ScoreAllAsync(
        IReadOnlyList<MediaFeedItem> items,
        string                       viewerUhid,
        double                       aiSignal,
        CancellationToken            ct)
    {
        var result = new List<(MediaFeedItem, double)>(items.Count);

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();

            // Threat gate: creators assessed High or above get score = 0.
            var threatLevel = await _moderator
                .AssessSourceAsync(item.Content.CreatorUhid, ct)
                .ConfigureAwait(false);

            if (threatLevel >= AiThreatLevel.High)
            {
                result.Add((item, 0.0));
                continue;
            }

            // 1. Reputation signal [0, 1]
            double reputationScore = await _reputation
                .GetReputationScoreAsync(item.Content.CreatorUhid, ct)
                .ConfigureAwait(false);

            // 2. Recency signal: exp(−hours / 48)
            double hoursSince   = (DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeMilliseconds(item.PublishedAtMs)).TotalHours;
            double recencyScore = Math.Exp(-Math.Max(0.0, hoursSince) / RecencyDecayHours);

            // 3. Engagement signal, clamped to [0, 1]
            double rawEngagement =
                item.LikeCount    * 2.0 +
                item.ShareCount   * 3.0 +
                item.CommentCount * 1.0 +
                item.WatchCount   * 0.5;
            double engagementScore = Math.Min(rawEngagement / 1_000.0, 1.0);

            // 4. Watch-history signal [0, 1]; neutral 0.5 when no history.
            //    Completion = 1.0 → boost; instant skip = 0.0 → suppress.
            double? completionRate = await _history
                .GetCompletionRateAsync(viewerUhid, item.Content.ContentHash, ct)
                .ConfigureAwait(false);
            double watchHistoryScore = completionRate ?? 0.5;

            double composite =
                reputationScore   * ReputationWeight   +
                aiSignal          * AiWeight           +
                recencyScore      * RecencyWeight      +
                engagementScore   * EngagementWeight   +
                watchHistoryScore * WatchHistoryWeight;

            result.Add((item, composite));
        }

        return result;
    }

    /// <summary>
    /// Converts a dictionary of transport multipliers into a single normalised
    /// signal in [0, 1]. An empty dictionary (neutral / AI unavailable) returns
    /// 0.5. Multipliers are averaged and linearly mapped: 0.0 → 0.0,
    /// 1.0 (neutral) → 0.5, 2.0+ → 1.0.
    /// </summary>
    private static double ComputeAiSignal(IReadOnlyDictionary<string, double> biases)
    {
        if (biases.Count == 0)
            return 0.5;

        double avg = biases.Values.Average();
        return Math.Clamp(avg / 2.0, 0.0, 1.0);
    }
}
