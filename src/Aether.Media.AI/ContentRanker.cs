// SPDX-License-Identifier: MIT

using Aether.Extensibility;
using Aether.Media.Core.Models;
using Aether.Reputation;

namespace Aether.Media.AI;

/// <summary>
/// Ranks a feed of <see cref="MediaFeedItem"/> values using a four-signal
/// composite score:
/// <list type="bullet">
///   <item><description>Reputation (35 %) — creator's node reputation score [0,1].</description></item>
///   <item><description>AI bias    (25 %) — AI transport-bias signal; neutral 0.5 when AI unavailable.</description></item>
///   <item><description>Recency    (25 %) — exponential decay over 48 hours.</description></item>
///   <item><description>Engagement (15 %) — weighted reaction counts, clamped to [0,1].</description></item>
/// </list>
/// Items whose creator is assessed as <see cref="AiThreatLevel.High"/> or above
/// are unconditionally assigned a composite score of 0 and sorted to the bottom.
/// </summary>
public sealed class ContentRanker : IContentRanker
{
    // ── Score weights (must sum to 1.0) ────────────────────────────────────
    private const double ReputationWeight  = 0.35;
    private const double AiWeight          = 0.25;
    private const double RecencyWeight     = 0.25;
    private const double EngagementWeight  = 0.15;

    // ── Recency half-life ──────────────────────────────────────────────────
    /// <summary>
    /// Time constant for the recency exponential decay.
    /// At 48 hours the score reaches e^-1 ≈ 0.368; beyond that it approaches 0.
    /// </summary>
    private const double RecencyDecayHours = 48.0;

    // ── Dependencies ───────────────────────────────────────────────────────
    private readonly INodeReputationService _reputation;
    private readonly IAetherAiProvider      _ai;
    private readonly IContentModerator      _moderator;

    public ContentRanker(
        INodeReputationService reputation,
        IAetherAiProvider      ai,
        IContentModerator      moderator)
    {
        _reputation = reputation ?? throw new ArgumentNullException(nameof(reputation));
        _ai         = ai         ?? throw new ArgumentNullException(nameof(ai));
        _moderator  = moderator  ?? throw new ArgumentNullException(nameof(moderator));
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

        // Pre-fetch AI transport biases once for the whole batch (payload-size
        // agnostic here — we pass 0 as a neutral probe).
        IReadOnlyDictionary<string, double> transportBiases =
            _ai.IsAvailable
                ? await _ai.GetTransportBiasesAsync(0, ct).ConfigureAwait(false)
                : new Dictionary<string, double>();

        // Derive a single AI signal from the average of all transport biases
        // (already in (0, ∞)); normalise to [0, 1] by treating ≥2.0 as max.
        double aiSignal = ComputeAiSignal(transportBiases);

        // Score every item concurrently.
        var scored = await ScoreAllAsync(items, aiSignal, ct).ConfigureAwait(false);

        // Sort descending: safe items first (natural score), then zero-score threats last.
        return scored
            .OrderByDescending(pair => pair.Score)
            .Select(pair => pair.Item)
            .ToList()
            .AsReadOnly();
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task<List<(MediaFeedItem Item, double Score)>> ScoreAllAsync(
        IReadOnlyList<MediaFeedItem> items,
        double aiSignal,
        CancellationToken ct)
    {
        var result = new List<(MediaFeedItem, double)>(items.Count);

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();

            // Threat gate: creators assessed High or Critical get score = 0.
            var threatLevel = await _moderator
                .AssessSourceAsync(item.Content.CreatorUhid, ct)
                .ConfigureAwait(false);

            if (threatLevel >= AiThreatLevel.High)
            {
                result.Add((item, 0.0));
                continue;
            }

            // Reputation signal [0, 1]
            double reputationScore = await _reputation
                .GetReputationScoreAsync(item.Content.CreatorUhid, ct)
                .ConfigureAwait(false);

            // Recency signal: exp(-hours / 48)
            double hoursSince   = (DateTime.UtcNow - item.PublishedAt.ToUniversalTime()).TotalHours;
            double recencyScore = Math.Exp(-Math.Max(0.0, hoursSince) / RecencyDecayHours);

            // Engagement signal, clamped to [0, 1]
            double rawEngagement =
                item.LikeCount    * 2.0 +
                item.ShareCount   * 3.0 +
                item.CommentCount * 1.0 +
                item.WatchCount   * 0.5;
            double engagementScore = Math.Min(rawEngagement / 1000.0, 1.0);

            double composite =
                reputationScore  * ReputationWeight +
                aiSignal         * AiWeight         +
                recencyScore     * RecencyWeight     +
                engagementScore  * EngagementWeight;

            result.Add((item, composite));
        }

        return result;
    }

    /// <summary>
    /// Converts a dictionary of transport multipliers into a single normalised signal
    /// in [0, 1]. A neutral provider (empty dict) returns 0.5. Multipliers are
    /// averaged and then linearly mapped so that an average of 0.0 → 0.0,
    /// 1.0 (neutral) → 0.5, and 2.0+ → 1.0.
    /// </summary>
    private static double ComputeAiSignal(IReadOnlyDictionary<string, double> biases)
    {
        if (biases.Count == 0)
            return 0.5;

        double avg = biases.Values.Average();
        // Map [0, 2] → [0, 1]; clamp outside that range.
        return Math.Clamp(avg / 2.0, 0.0, 1.0);
    }
}
