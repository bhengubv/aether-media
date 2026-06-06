// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;

namespace AetherNet.Vault.Core;

/// <summary>
/// Describes a file stored in the Aether Vault.  The manifest contains all
/// metadata required to locate, reassemble, and decrypt the file shards
/// scattered across mesh nodes.
/// </summary>
/// <param name="FileId">Unique identifier for this vault entry.</param>
/// <param name="ContentHash">SHA-256 hex digest of the plaintext content before encryption.</param>
/// <param name="EncryptionSalt">Random salt used to derive the symmetric encryption key.</param>
/// <param name="ShardHashes">SHA-256 hex digests of each encrypted shard (length == K + M).</param>
/// <param name="K">Number of data shards required to reconstruct the file (default 10).</param>
/// <param name="M">Number of parity shards added for redundancy (default 4).</param>
/// <param name="CreatedAtUtc">UTC timestamp when this vault entry was created.</param>
/// <param name="SizeBytes">Original plaintext size in bytes.</param>
/// <param name="Label">Human-readable label for this vault entry.</param>
public sealed record VaultManifest(
    [property: JsonPropertyName("file_id")]          Guid     FileId,
    [property: JsonPropertyName("content_hash")]     string   ContentHash,
    [property: JsonPropertyName("encryption_salt")]  byte[]   EncryptionSalt,
    [property: JsonPropertyName("shard_hashes")]     string[] ShardHashes,
    [property: JsonPropertyName("k")]                int      K          = 10,
    [property: JsonPropertyName("m")]                int      M          = 4,
    [property: JsonPropertyName("created_at_utc")]   DateTime CreatedAtUtc = default,
    [property: JsonPropertyName("size_bytes")]       long     SizeBytes  = 0,
    [property: JsonPropertyName("label")]            string   Label      = "");
