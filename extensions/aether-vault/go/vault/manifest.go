// SPDX-License-Identifier: MIT
package vault

import "time"

// VaultManifest describes a file stored in the Aether Vault.
// It contains all metadata required to locate, reassemble, and decrypt the
// file shards scattered across mesh nodes.
type VaultManifest struct {
	// FileId is the unique identifier for this vault entry (UUID string).
	FileId string `json:"file_id"`
	// ContentHash is the SHA-256 hex digest of the plaintext content before encryption.
	ContentHash string `json:"content_hash"`
	// EncryptionSalt is the random salt (base64-encoded) used to derive the symmetric key.
	EncryptionSalt []byte `json:"encryption_salt"`
	// ShardHashes contains the SHA-256 hex digests of each encrypted shard (length == K + M).
	ShardHashes []string `json:"shard_hashes"`
	// K is the number of data shards required to reconstruct the file (default 10).
	K int `json:"k"`
	// M is the number of parity shards added for redundancy (default 4).
	M int `json:"m"`
	// CreatedAtUtc is the UTC timestamp when this vault entry was created.
	CreatedAtUtc time.Time `json:"created_at_utc"`
	// SizeBytes is the original plaintext size in bytes.
	SizeBytes int64 `json:"size_bytes"`
	// Label is a human-readable label for this vault entry.
	Label string `json:"label"`
}

// VaultShard represents a single encrypted shard of a vault file.
type VaultShard struct {
	// ShardHash is the SHA-256 hex digest of the encrypted shard bytes.
	ShardHash string `json:"shard_hash"`
	// ShardIndex is the zero-based index of this shard within the erasure-coded set.
	ShardIndex int `json:"shard_index"`
	// EncFileId is the encrypted file identifier used to locate the shard on a mesh node.
	EncFileId []byte `json:"enc_file_id"`
}

// VaultHealth describes the health of a vault file by reporting how many of
// its shards are currently reachable on the mesh.
type VaultHealth struct {
	// TotalShards is the total number of shards (K + M) expected for this file.
	TotalShards int `json:"total_shards"`
	// ReachableShards is the number of shards currently reachable on the mesh.
	ReachableShards int `json:"reachable_shards"`
	// IsRecoverable is true when ReachableShards >= K (reconstruction is possible).
	IsRecoverable bool `json:"is_recoverable"`
	// RedundancyScore is the ratio of reachable shards to total shards (0.0–1.0).
	RedundancyScore float64 `json:"redundancy_score"`
}
