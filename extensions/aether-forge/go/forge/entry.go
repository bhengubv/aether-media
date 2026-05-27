// SPDX-License-Identifier: MIT
package forge

import "time"

// ForgeEntry represents a single package cached in the Aether Forge
// distributed package-cache layer.
type ForgeEntry struct {
	// ContentHash is the SHA-256 hex digest of the cached package bytes.
	ContentHash string `json:"content_hash"`
	// PackageId is the fully-qualified package identifier in the form
	// "ecosystem:name@version", e.g. "npm:react@18.2.0".
	PackageId string `json:"package_id"`
	// FetchedAtUtc is the UTC timestamp of when the package was first cached.
	FetchedAtUtc time.Time `json:"fetched_at_utc"`
	// SizeBytes is the byte length of the cached package payload.
	SizeBytes int64 `json:"size_bytes"`
	// DownloadCount is the number of times this entry has been served from
	// the local Forge cache.
	DownloadCount int `json:"download_count"`
}

// ForgeStats holds aggregate statistics for the local Aether Forge cache node.
type ForgeStats struct {
	// TotalBytesSaved is the total bytes served from the local cache
	// (bandwidth saved vs. internet fetches).
	TotalBytesSaved int64 `json:"total_bytes_saved"`
	// TotalPeersServed is the number of distinct peer nodes that downloaded
	// at least one package from this node.
	TotalPeersServed int `json:"total_peers_served"`
	// CatalogueSize is the number of unique package entries in the local cache.
	CatalogueSize int `json:"catalogue_size"`
	// TopPackages lists the most-downloaded packages ordered by DownloadCount
	// descending.
	TopPackages []ForgeEntry `json:"top_packages"`
}
