// SPDX-License-Identifier: MIT

using AetherNet.Reputation;

namespace AetherMedia.AI.Tests.Helpers;

/// <summary>
/// Controllable INodeReputationService stub.
/// Pre-populate <see cref="Scores"/> to control what <see cref="GetReputationScoreAsync"/> returns.
/// Unknown UHIDs return 1.0 (benefit of the doubt — same contract as the real service).
/// </summary>
internal sealed class FakeReputationService : INodeReputationService
{
    /// <summary>UHID → score override. Absent entries return 1.0.</summary>
    public Dictionary<string, double> Scores { get; } = new(StringComparer.Ordinal);

    public Task<double> GetReputationScoreAsync(string uhid, CancellationToken ct = default)
        => Task.FromResult(Scores.TryGetValue(uhid, out var s) ? s : 1.0);

    /// <summary>Gossip weight tracks the node's own reputation (same controllable source, default 1.0).</summary>
    public Task<double> GetGossipWeightAsync(string uhid, CancellationToken ct = default)
        => Task.FromResult(Scores.TryGetValue(uhid, out var s) ? s : 1.0);

    public Task<IReadOnlyDictionary<string, double>> GetAllScoresAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<string, double>>(
            new Dictionary<string, double>(Scores, StringComparer.Ordinal));

    // ── Write methods — no-ops for test purposes ──────────────────────────

    public Task RecordRreqFloodAttemptAsync(string uhid, CancellationToken ct = default)
        => Task.CompletedTask;
    public Task RecordReplayAttemptAsync(string uhid, CancellationToken ct = default)
        => Task.CompletedTask;
    public Task RecordSignatureFailureAsync(string uhid, CancellationToken ct = default)
        => Task.CompletedTask;
    public Task RecordCustodyRefusalAsync(string uhid, CancellationToken ct = default)
        => Task.CompletedTask;
    public Task RecordDeliverySuccessAsync(string uhid, int roundTripMs, CancellationToken ct = default)
        => Task.CompletedTask;
    public Task RecordDeliveryFailureAsync(string uhid, CancellationToken ct = default)
        => Task.CompletedTask;
    public Task ApplyWeightedDeltaAsync(string uhid, double weightedDelta, CancellationToken ct = default)
        => Task.CompletedTask;
}
