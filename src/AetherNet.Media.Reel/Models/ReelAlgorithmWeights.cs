// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Reel;

/// <summary>
/// User-tunable weights for the on-device For You ranking algorithm.
///
/// All weights are stored locally and never shared with any external service. Users
/// can inspect and adjust them directly ("algorithm transparency") or reset to the
/// research-backed defaults below.
///
/// The ranker normalises these weights internally so they do not need to sum to 1.
///
/// Design rationale:
/// <list type="bullet">
///   <item>
///     <see cref="WatchTimeRatio"/> is the dominant signal — consistent with both
///     TikTok research and Twitter's open-source ranker, completion rate is the
///     strongest proxy for genuine interest.
///   </item>
///   <item>
///     <see cref="SkipPenalty"/> is a positive value applied negatively — mirrors
///     Twitter's heavy downweighting of fast-swipe signals.
///   </item>
///   <item>
///     <see cref="NoveltyBonus"/> is the key differentiator from TikTok and Twitter:
///     it explicitly rewards content from hashtag clusters the user has NOT recently
///     engaged with, preventing algorithmic echo chambers.
///   </item>
///   <item>
///     <see cref="MeshPopularity"/> is intentionally the smallest weight — it
///     reflects gossipped peer aggregates (not personal data) and must not dominate
///     personal signals, otherwise popular content crowds out niche content.
///   </item>
/// </list>
/// </summary>
public sealed record ReelAlgorithmWeights
{
    /// <summary>Watch-time completion ratio weight (default 0.35).</summary>
    public float WatchTimeRatio  { get; init; } = 0.35f;

    /// <summary>Replay / rewatch count weight (default 0.20).</summary>
    public float ReplayBonus     { get; init; } = 0.20f;

    /// <summary>Like signal weight (default 0.15).</summary>
    public float LikeSignal      { get; init; } = 0.15f;

    /// <summary>Share signal weight (default 0.10).</summary>
    public float ShareSignal     { get; init; } = 0.10f;

    /// <summary>
    /// Fast-skip penalty weight (default 0.25). Applied as a negative.
    /// </summary>
    public float SkipPenalty     { get; init; } = 0.25f;

    /// <summary>Hashtag affinity weight (default 0.10).</summary>
    public float HashtagAffinity { get; init; } = 0.10f;

    /// <summary>Creator affinity weight (default 0.08).</summary>
    public float CreatorAffinity { get; init; } = 0.08f;

    /// <summary>
    /// Novelty bonus weight (default 0.10). Rewards content from clusters the
    /// user has NOT recently engaged with — anti-echo-chamber mechanism.
    /// </summary>
    public float NoveltyBonus    { get; init; } = 0.10f;

    /// <summary>Content recency weight (default 0.08).</summary>
    public float RecencyBonus    { get; init; } = 0.08f;

    /// <summary>
    /// Gossipped mesh popularity weight (default 0.05). Intentionally small —
    /// peer aggregate counts should inform but never dominate personal signals.
    /// </summary>
    public float MeshPopularity  { get; init; } = 0.05f;

    /// <summary>Returns a new instance with all weights set to their defaults.</summary>
    public static ReelAlgorithmWeights Default => new();
}
