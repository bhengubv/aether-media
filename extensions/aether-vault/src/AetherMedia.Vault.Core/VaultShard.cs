// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;

namespace AetherMedia.Vault.Core;

/// <summary>
/// Represents a single encrypted shard of a file stored in the Aether Vault.
/// </summary>
/// <param name="ShardHash">SHA-256 hex digest of the encrypted shard bytes.</param>
/// <param name="ShardIndex">Zero-based index of this shard within the erasure-coded set.</param>
/// <param name="EncFileId">
/// Encrypted file identifier used to locate the shard on a mesh node.
/// This is the file's logical ID encrypted with the node's public key.
/// </param>
public sealed record VaultShard(
    [property: JsonPropertyName("shard_hash")]  string ShardHash,
    [property: JsonPropertyName("shard_index")] int    ShardIndex,
    [property: JsonPropertyName("enc_file_id")] byte[] EncFileId);
