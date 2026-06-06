// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using AetherNet.Extensibility;
using AetherNet.Media.Core.Models;
using AetherNet.Protocol;

namespace AetherNet.Media.AI;

/// <summary>
/// Moderates content and social packets by combining two independent signals:
/// <list type="bullet">
///   <item>
///     <description>
///       <b>AI assessment</b> via <see cref="IAetherNetAiProvider.AssessThreatAsync"/>.
///       When the provider is unavailable the signal returns
///       <see cref="AiThreatLevel.None"/> so content is never incorrectly hidden.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Velocity burst detection</b> — a per-source sliding-window counter
///       that fires <see cref="AiThreatLevel.Medium"/> when a single UHID emits
///       social packets above a type-specific rate threshold. This operates even
///       when the AI provider is unavailable, providing a safety net against
///       follow-bots and reaction spam.
///     </description>
///   </item>
/// </list>
/// </summary>
public sealed class ContentModerator : IContentModerator
{
    // ── Velocity-detection parameters ──────────────────────────────────────

    /// <summary>
    /// Window width for <see cref="PacketType.WatchReaction"/> burst detection.
    /// </summary>
    private const long ReactionWindowMs = 30_000L;   // 30 s

    /// <summary>
    /// Maximum reactions per source UHID within <see cref="ReactionWindowMs"/>
    /// before the source is flagged as <see cref="AiThreatLevel.Medium"/>.
    /// </summary>
    private const int  ReactionBurstThreshold = 20;

    /// <summary>
    /// Window width for all other social packet types (Follow, ContentAnnounce,
    /// ProfileSync, etc.).
    /// </summary>
    private const long SocialWindowMs = 60_000L;     // 60 s

    /// <summary>
    /// Maximum non-reaction social packets per source UHID within
    /// <see cref="SocialWindowMs"/> before the source is flagged as
    /// <see cref="AiThreatLevel.Medium"/>.
    /// </summary>
    private const int  SocialBurstThreshold = 5;

    // ── State ──────────────────────────────────────────────────────────────
    // source UHID → queue of arrival timestamps (ms since epoch)
    private readonly ConcurrentDictionary<string, Queue<long>> _velocityWindows = new();

    // ── Dependencies ───────────────────────────────────────────────────────
    private readonly IAetherNetAiProvider _ai;

    public ContentModerator(IAetherNetAiProvider ai)
    {
        _ai = ai ?? throw new ArgumentNullException(nameof(ai));
    }

    // ── IContentModerator ──────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// A piece of content is safe when its creator's assessed threat level is
    /// <see cref="AiThreatLevel.None"/> or <see cref="AiThreatLevel.Low"/>.
    /// </remarks>
    public async Task<bool> IsContentSafeAsync(
        MediaContent content,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var threat = await AssessSourceAsync(content.CreatorUhid, ct).ConfigureAwait(false);
        return threat <= AiThreatLevel.Low;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Creates a synthetic <see cref="MeshPacket"/> with the creator's UHID as
    /// <see cref="MeshPacket.SourceUhid"/> and calls
    /// <see cref="IAetherNetAiProvider.AssessThreatAsync"/>. Re-uses the existing
    /// per-packet anomaly-detection surface to evaluate a creator identity rather
    /// than a raw packet — no additional AI API surface is required.
    /// </remarks>
    public async Task<AiThreatLevel> AssessSourceAsync(
        string creatorUhid,
        CancellationToken ct = default)
    {
        if (!_ai.IsAvailable)
            return AiThreatLevel.None;

        if (string.IsNullOrWhiteSpace(creatorUhid))
            return AiThreatLevel.None;

        var probe = new MeshPacket
        {
            Type            = PacketType.PresenceBeacon,
            SourceUhid      = creatorUhid,
            DestinationUhid = string.Empty,
            Ttl             = 0,
            Priority        = 0,
            Payload         = Array.Empty<byte>(),
        };

        return await _ai.AssessThreatAsync(probe, ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<AiThreatLevel> AssessSocialPacketAsync(
        MeshPacket packet,
        CancellationToken ct = default)
    {
        if (packet is null)
            return AiThreatLevel.None;

        try
        {
            // ── AI signal ──────────────────────────────────────────────────
            AiThreatLevel aiThreat = _ai.IsAvailable
                ? await _ai.AssessThreatAsync(packet, ct).ConfigureAwait(false)
                : AiThreatLevel.None;

            // ── Velocity signal ────────────────────────────────────────────
            AiThreatLevel velocityThreat = CheckVelocity(packet.SourceUhid, packet.Type);

            // Return the higher of the two signals: either can flag a threat
            // independently of the other.
            return (AiThreatLevel)Math.Max((byte)aiThreat, (byte)velocityThreat);
        }
        catch
        {
            // Stay permissive on any internal failure — never block on error.
            return AiThreatLevel.None;
        }
    }

    // ── Velocity burst detection ────────────────────────────────────────────

    /// <summary>
    /// Records this packet's arrival and returns <see cref="AiThreatLevel.Medium"/>
    /// when the source UHID has exceeded the type-specific burst threshold within
    /// its sliding window.
    /// </summary>
    private AiThreatLevel CheckVelocity(string sourceUhid, PacketType packetType)
    {
        if (string.IsNullOrWhiteSpace(sourceUhid))
            return AiThreatLevel.None;

        bool isReaction = packetType == PacketType.WatchReaction;
        long windowMs   = isReaction ? ReactionWindowMs  : SocialWindowMs;
        int  threshold  = isReaction ? ReactionBurstThreshold : SocialBurstThreshold;

        long now         = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long windowStart = now - windowMs;

        var queue = _velocityWindows.GetOrAdd(sourceUhid, _ => new Queue<long>());

        lock (queue)
        {
            // Evict timestamps that have aged out of the window.
            while (queue.Count > 0 && queue.Peek() < windowStart)
                queue.Dequeue();

            queue.Enqueue(now);

            return queue.Count > threshold
                ? AiThreatLevel.Medium
                : AiThreatLevel.None;
        }
    }
}
