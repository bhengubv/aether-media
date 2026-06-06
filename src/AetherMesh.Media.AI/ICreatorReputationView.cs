// SPDX-License-Identifier: MIT

namespace AetherMesh.Media.AI;

/// <summary>
/// Read-only projection of creator reputation scores sourced from the underlying
/// <c>INodeReputationService</c>. Callers in the AI layer use this view rather
/// than the mutable service directly so that ranking logic cannot accidentally
/// write reputation signals.
/// </summary>
public interface ICreatorReputationView
{
    /// <summary>
    /// Returns the current reputation score for <paramref name="creatorUhid"/> in [0, 1].
    /// Returns 1.0 for unknown creators (benefit of the doubt).
    /// </summary>
    Task<double> GetCreatorScoreAsync(string creatorUhid, CancellationToken ct = default);

    /// <summary>
    /// Returns the top <paramref name="limit"/> creators by reputation score, sorted
    /// descending. Each tuple contains the creator UHID and their score in [0, 1].
    /// </summary>
    Task<IReadOnlyList<(string Uhid, double Score)>> GetTopCreatorsAsync(
        int limit = 10,
        CancellationToken ct = default);
}
