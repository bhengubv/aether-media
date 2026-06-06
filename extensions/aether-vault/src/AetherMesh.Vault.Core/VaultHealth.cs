// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;

namespace AetherMesh.Vault.Core;

/// <summary>
/// Describes the health of a vault file by reporting how many of its shards
/// are currently reachable on the mesh.
/// </summary>
/// <param name="TotalShards">Total number of shards (K + M) expected for this file.</param>
/// <param name="ReachableShards">Number of shards currently reachable on the mesh.</param>
/// <param name="IsRecoverable">
/// <see langword="true"/> when <see cref="ReachableShards"/> &gt;= K (the data-shard
/// threshold required for reconstruction).
/// </param>
/// <param name="RedundancyScore">
/// Ratio of reachable shards to total shards (0.0 = none reachable, 1.0 = all reachable).
/// </param>
public sealed record VaultHealth(
    [property: JsonPropertyName("total_shards")]     int    TotalShards,
    [property: JsonPropertyName("reachable_shards")] int    ReachableShards,
    [property: JsonPropertyName("is_recoverable")]   bool   IsRecoverable,
    [property: JsonPropertyName("redundancy_score")] double RedundancyScore);
