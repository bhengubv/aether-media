// SPDX-License-Identifier: MIT

using Aether.Extensibility;
using Aether.Media.Core.Models;

namespace Aether.Media.AI.Tests.Helpers;

/// <summary>
/// Controllable IContentModerator stub for ContentRanker tests.
/// By default all sources return <see cref="AiThreatLevel.None"/> (safe).
/// Override <see cref="ThreatLevels"/> to control per-UHID results.
/// </summary>
internal sealed class FakeContentModerator : IContentModerator
{
    /// <summary>UHID → threat level. UHIDs absent from this map return <see cref="AiThreatLevel.None"/>.</summary>
    public Dictionary<string, AiThreatLevel> ThreatLevels { get; } = new(StringComparer.Ordinal);

    public Task<AiThreatLevel> AssessSourceAsync(string creatorUhid, CancellationToken ct = default)
        => Task.FromResult(ThreatLevels.TryGetValue(creatorUhid, out var level)
            ? level
            : AiThreatLevel.None);

    public async Task<bool> IsContentSafeAsync(MediaContent content, CancellationToken ct = default)
    {
        var threat = await AssessSourceAsync(content.CreatorUhid, ct).ConfigureAwait(false);
        return threat <= AiThreatLevel.Low;
    }
}
