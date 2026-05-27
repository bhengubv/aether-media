// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;

namespace Aether.Vault.Core;

/// <summary>
/// Represents an incoming shard-retrieval request received from another mesh node.
/// </summary>
/// <param name="ShardHash">SHA-256 hex digest identifying the requested shard.</param>
/// <param name="RequesterUhid">Universal host ID of the node requesting the shard.</param>
/// <param name="RequestedAtUtc">UTC timestamp when the request was received.</param>
public sealed record VaultShardRequest(
    [property: JsonPropertyName("shard_hash")]        string   ShardHash,
    [property: JsonPropertyName("requester_uhid")]    string   RequesterUhid,
    [property: JsonPropertyName("requested_at_utc")]  DateTime RequestedAtUtc);
