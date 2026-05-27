// SPDX-License-Identifier: MIT

/** Describes a file stored in the Aether Vault. */
export interface VaultManifest {
  /** Unique identifier for this vault entry (UUID). */
  fileId: string;
  /** SHA-256 hex digest of the plaintext content before encryption. */
  contentHash: string;
  /** Base64-encoded random salt used to derive the symmetric encryption key. */
  encryptionSalt: string;
  /** SHA-256 hex digests of each encrypted shard (length == k + m). */
  shardHashes: string[];
  /** Number of data shards required to reconstruct the file (default 10). */
  k: number;
  /** Number of parity shards added for redundancy (default 4). */
  m: number;
  /** UTC timestamp when this vault entry was created (ISO-8601 string). */
  createdAtUtc: string;
  /** Original plaintext size in bytes. */
  sizeBytes: number;
  /** Human-readable label for this vault entry. */
  label: string;
}

/** Represents a single encrypted shard of a vault file. */
export interface VaultShard {
  /** SHA-256 hex digest of the encrypted shard bytes. */
  shardHash: string;
  /** Zero-based index of this shard within the erasure-coded set. */
  shardIndex: number;
  /** Base64-encoded encrypted file identifier used to locate the shard on a node. */
  encFileId: string;
}

/**
 * Describes the health of a vault file by reporting how many of its shards
 * are currently reachable on the mesh.
 */
export interface VaultHealth {
  /** Total number of shards (k + m) expected for this file. */
  totalShards: number;
  /** Number of shards currently reachable on the mesh. */
  reachableShards: number;
  /** True when reachableShards >= k (reconstruction is possible). */
  isRecoverable: boolean;
  /**
   * Ratio of reachable shards to total shards.
   * 0.0 = none reachable, 1.0 = all reachable.
   */
  redundancyScore: number;
}

/** Represents an incoming shard-retrieval request from another mesh node. */
export interface VaultShardRequest {
  /** SHA-256 hex digest identifying the requested shard. */
  shardHash: string;
  /** Universal host ID of the node requesting the shard. */
  requesterUhid: string;
  /** UTC timestamp when the request was received (ISO-8601 string). */
  requestedAtUtc: string;
}
