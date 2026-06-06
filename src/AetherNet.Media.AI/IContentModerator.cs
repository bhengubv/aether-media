// SPDX-License-Identifier: MIT

using AetherNet.Extensibility;
using AetherNet.Media.Core.Models;
using AetherNet.Protocol;

namespace AetherNet.Media.AI;

/// <summary>
/// Guards the feed by assessing content safety and evaluating the threat level
/// of the creator who published it.
/// </summary>
public interface IContentModerator
{
    /// <summary>
    /// Returns <c>true</c> when the <paramref name="content"/> is safe to display.
    /// A piece of content is considered safe when its creator's assessed threat
    /// level is <see cref="AiThreatLevel.None"/> or <see cref="AiThreatLevel.Low"/>.
    /// </summary>
    Task<bool> IsContentSafeAsync(MediaContent content, CancellationToken ct = default);

    /// <summary>
    /// Returns the AI-assessed threat level for the creator identified by
    /// <paramref name="creatorUhid"/>. When the AI provider is unavailable the
    /// method returns <see cref="AiThreatLevel.None"/> as a permissive fallback
    /// so that content is not incorrectly suppressed.
    /// </summary>
    Task<AiThreatLevel> AssessSourceAsync(string creatorUhid, CancellationToken ct = default);

    /// <summary>
    /// Assesses the threat level of an incoming social <see cref="MeshPacket"/>
    /// (e.g. Follow, ContentAnnounce, WatchReaction, ProfileSync).
    ///
    /// <para>
    /// The assessment combines two independent signals and returns the higher
    /// of the two:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///       <b>AI signal</b> — <see cref="IAetherNetAiProvider.AssessThreatAsync"/>
    ///       called directly on the live social packet; returns
    ///       <see cref="AiThreatLevel.None"/> when AI is unavailable.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       <b>Velocity signal</b> — a sliding-window burst detector that
    ///       returns <see cref="AiThreatLevel.Medium"/> when the source UHID
    ///       emits social packets above a type-specific rate threshold, even
    ///       when the AI provider is unavailable.
    ///     </description>
    ///   </item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// This method never throws; it returns <see cref="AiThreatLevel.None"/>
    /// on any internal error to stay permissive.
    /// </para>
    /// </summary>
    /// <param name="packet">The incoming social mesh packet to assess.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<AiThreatLevel> AssessSocialPacketAsync(
        MeshPacket packet,
        CancellationToken ct = default);
}
