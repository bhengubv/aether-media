// SPDX-License-Identifier: MIT
package forge

import (
	"context"
	"io"
)

// IForgeService defines operations for querying, caching, and fetching
// packages through the Aether Forge distributed package-cache layer.
type IForgeService interface {
	// Query looks up a package in the local Forge cache by its
	// fully-qualified packageId (e.g. "npm:react@18.2.0").
	// Returns nil, nil when the package is not cached.
	Query(ctx context.Context, packageId string) (*ForgeEntry, error)

	// Cache stores a package payload in the local Forge cache and announces
	// the new entry to neighbouring mesh nodes.
	Cache(ctx context.Context, packageId string, content io.Reader, contentHash string) (*ForgeEntry, error)

	// Fetch returns the raw byte stream of a cached package by its
	// contentHash. Returns nil, nil when the content is not found locally.
	Fetch(ctx context.Context, contentHash string) (io.ReadCloser, error)

	// GetStats returns aggregate statistics for the local Forge cache node.
	GetStats(ctx context.Context) (*ForgeStats, error)
}

// ForgeService is the default implementation of IForgeService.
// Embed this struct and override methods for different storage backends.
type ForgeService struct{}

// Query implements IForgeService.
func (s *ForgeService) Query(ctx context.Context, packageId string) (*ForgeEntry, error) {
	return nil, errNotImplemented("Query")
}

// Cache implements IForgeService.
func (s *ForgeService) Cache(
	ctx context.Context,
	packageId string,
	content io.Reader,
	contentHash string,
) (*ForgeEntry, error) {
	return nil, errNotImplemented("Cache")
}

// Fetch implements IForgeService.
func (s *ForgeService) Fetch(ctx context.Context, contentHash string) (io.ReadCloser, error) {
	return nil, errNotImplemented("Fetch")
}

// GetStats implements IForgeService.
func (s *ForgeService) GetStats(ctx context.Context) (*ForgeStats, error) {
	return nil, errNotImplemented("GetStats")
}

type notImplementedError struct{ method string }

func (e *notImplementedError) Error() string {
	return "ForgeService." + e.method + ": requires a storage backend implementation"
}

func errNotImplemented(method string) error { return &notImplementedError{method} }
