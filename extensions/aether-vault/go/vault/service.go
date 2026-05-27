// SPDX-License-Identifier: MIT
package vault

import (
	"context"
	"io"
)

// IVaultService defines operations for storing, recovering, and managing files
// in the Aether Vault distributed encrypted storage layer.
type IVaultService interface {
	// Store encrypts and erasure-codes the content stream, distributes the
	// resulting shards across mesh nodes, and returns the VaultManifest
	// required to recover the file later.
	Store(ctx context.Context, content io.Reader, label string) (*VaultManifest, error)

	// Recover locates and reassembles the shards described by manifest,
	// decrypts them, and returns the plaintext content stream.
	// Requires at least manifest.K reachable shards.
	Recover(ctx context.Context, manifest *VaultManifest) (io.ReadCloser, error)

	// CheckHealth probes the mesh for shards described by manifest and
	// returns a VaultHealth snapshot.
	CheckHealth(ctx context.Context, manifest *VaultManifest) (*VaultHealth, error)

	// Replicate ensures that at least targetReplicas copies of each shard
	// described by manifest are held across distinct mesh nodes.
	Replicate(ctx context.Context, manifest *VaultManifest, targetReplicas int) error
}

// ShardRequestedFunc is a callback invoked when a shard request arrives from
// another mesh node.
type ShardRequestedFunc func(req *VaultShardRequest)

// VaultShardRequest represents an incoming shard-retrieval request from another
// mesh node.
type VaultShardRequest struct {
	// ShardHash is the SHA-256 hex digest identifying the requested shard.
	ShardHash string `json:"shard_hash"`
	// RequesterUhid is the universal host ID of the requesting node.
	RequesterUhid string `json:"requester_uhid"`
	// RequestedAtUtc is the UTC timestamp when the request was received.
	RequestedAtUtc string `json:"requested_at_utc"`
}

// VaultService is the default implementation of IVaultService.
// Embed this struct and override methods as needed for different transport
// backends.
type VaultService struct {
	// OnShardRequested is called whenever a shard request arrives from the
	// mesh. May be nil.
	OnShardRequested ShardRequestedFunc
}

// Store implements IVaultService.
func (s *VaultService) Store(ctx context.Context, content io.Reader, label string) (*VaultManifest, error) {
	return nil, errNotImplemented("Store")
}

// Recover implements IVaultService.
func (s *VaultService) Recover(ctx context.Context, manifest *VaultManifest) (io.ReadCloser, error) {
	return nil, errNotImplemented("Recover")
}

// CheckHealth implements IVaultService.
func (s *VaultService) CheckHealth(ctx context.Context, manifest *VaultManifest) (*VaultHealth, error) {
	return nil, errNotImplemented("CheckHealth")
}

// Replicate implements IVaultService.
func (s *VaultService) Replicate(ctx context.Context, manifest *VaultManifest, targetReplicas int) error {
	return errNotImplemented("Replicate")
}

type notImplementedError struct{ method string }

func (e *notImplementedError) Error() string {
	return "VaultService." + e.method + ": requires a transport backend implementation"
}

func errNotImplemented(method string) error { return &notImplementedError{method} }
