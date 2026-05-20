// SPDX-License-Identifier: MIT

using Aether.Extensibility;
using Aether.Media.Core.Models;

namespace Aether.Media.AI;

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
}
