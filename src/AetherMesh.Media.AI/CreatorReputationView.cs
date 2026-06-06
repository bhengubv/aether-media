// SPDX-License-Identifier: MIT

using AetherMesh.Reputation;

namespace AetherMesh.Media.AI;

/// <summary>
/// Read-only projection of <see cref="INodeReputationService"/> that exposes
/// creator reputation scores to the AI ranking layer without allowing the
/// ranking code to write new signals.
/// </summary>
public sealed class CreatorReputationView : ICreatorReputationView
{
    private readonly INodeReputationService _reputation;

    public CreatorReputationView(INodeReputationService reputation)
    {
        _reputation = reputation ?? throw new ArgumentNullException(nameof(reputation));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Delegates directly to <see cref="INodeReputationService.GetReputationScoreAsync"/>.
    /// Unknown creators return 1.0 (benefit of the doubt) as defined by the
    /// underlying service contract.
    /// </remarks>
    public Task<double> GetCreatorScoreAsync(string creatorUhid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(creatorUhid))
            return Task.FromResult(1.0); // Unknown → benefit of the doubt

        return _reputation.GetReputationScoreAsync(creatorUhid, ct);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Fetches the full score dictionary from the service, sorts descending,
    /// and returns the top <paramref name="limit"/> entries as (Uhid, Score) tuples.
    /// </remarks>
    public async Task<IReadOnlyList<(string Uhid, double Score)>> GetTopCreatorsAsync(
        int limit = 10,
        CancellationToken ct = default)
    {
        if (limit <= 0)
            return Array.Empty<(string, double)>();

        var allScores = await _reputation.GetAllScoresAsync(ct).ConfigureAwait(false);

        IReadOnlyList<(string Uhid, double Score)> result = allScores
            .OrderByDescending(kvp => kvp.Value)
            .Take(limit)
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToList()
            .AsReadOnly();

        return result;
    }
}
