// SPDX-License-Identifier: MIT

using Aether.Extensibility;
using Aether.Media.Core.Models;
using Aether.Protocol;

namespace Aether.Media.AI;

/// <summary>
/// Moderates content by assessing the threat level of its creator via
/// <see cref="IAetherAiProvider.AssessThreatAsync"/>. When the AI provider is
/// unavailable (<see cref="IAetherAiProvider.IsAvailable"/> is <c>false</c>),
/// the moderator adopts a permissive stance and returns
/// <see cref="AiThreatLevel.None"/> so that content is not incorrectly hidden.
/// </summary>
public sealed class ContentModerator : IContentModerator
{
    private readonly IAetherAiProvider _ai;

    public ContentModerator(IAetherAiProvider ai)
    {
        _ai = ai ?? throw new ArgumentNullException(nameof(ai));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A piece of content is safe when its creator's assessed threat level is
    /// <see cref="AiThreatLevel.None"/> or <see cref="AiThreatLevel.Low"/>.
    /// Medium, High, and Critical threat levels are treated as unsafe.
    /// </remarks>
    public async Task<bool> IsContentSafeAsync(MediaContent content, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var threat = await AssessSourceAsync(content.CreatorUhid, ct).ConfigureAwait(false);
        return threat <= AiThreatLevel.Low;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Creates a synthetic <see cref="MeshPacket"/> with the creator's UHID as
    /// <see cref="MeshPacket.SourceUhid"/> and calls
    /// <see cref="IAetherAiProvider.AssessThreatAsync"/>. This re-uses the
    /// existing per-packet anomaly-detection surface to evaluate a creator rather
    /// than a raw packet, so no additional AI API surface is required.
    ///
    /// When the provider is not available the method returns
    /// <see cref="AiThreatLevel.None"/> immediately without calling the provider.
    /// </remarks>
    public async Task<AiThreatLevel> AssessSourceAsync(string creatorUhid, CancellationToken ct = default)
    {
        if (!_ai.IsAvailable)
            return AiThreatLevel.None;

        if (string.IsNullOrWhiteSpace(creatorUhid))
            return AiThreatLevel.None;

        // Synthesise a minimal probe packet that carries the creator's identity.
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
}
