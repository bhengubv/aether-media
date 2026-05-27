// SPDX-License-Identifier: MIT
package space

import "time"

// BreadcrumbType classifies the purpose of a SpaceBreadcrumb.
type BreadcrumbType int

const (
	// BreadcrumbTypeNotice is a general-purpose notice or announcement.
	BreadcrumbTypeNotice BreadcrumbType = 0
	// BreadcrumbTypeEmergency is an emergency alert that bypasses the radius
	// filter and is flooded to all reachable peers.
	BreadcrumbTypeEmergency BreadcrumbType = 1
	// BreadcrumbTypeCommerce is a commercial listing anchored to a location.
	BreadcrumbTypeCommerce BreadcrumbType = 2
	// BreadcrumbTypeEvent is a scheduled or live event.
	BreadcrumbTypeEvent BreadcrumbType = 3
	// BreadcrumbTypeJobPosting is a job posting anchored to a physical location.
	BreadcrumbTypeJobPosting BreadcrumbType = 4
)

// GeoHash is a strongly-typed geohash cell identifier.
type GeoHash string

// SpaceBreadcrumb is an immutable geo-anchored content record on the Aether
// Space layer.
type SpaceBreadcrumb struct {
	// ContentHash is the SHA-256 hex digest of the payload bytes.
	ContentHash string `json:"content_hash"`
	// GeoHash is the geohash cell in which the breadcrumb was dropped.
	GeoHash GeoHash `json:"geo_hash"`
	// AnchorUhid is the universal host ID of the originating node.
	AnchorUhid string `json:"anchor_uhid"`
	// CreatedAtUtc is the UTC creation timestamp.
	CreatedAtUtc time.Time `json:"created_at_utc"`
	// TtlHours is the time-to-live in hours.
	TtlHours int `json:"ttl_hours"`
	// Type is the semantic classification of the breadcrumb.
	Type BreadcrumbType `json:"type"`
	// Signature is the Ed25519 signature produced by the anchor node.
	Signature []byte `json:"signature"`
}

// IsExpired reports whether the breadcrumb's TTL has elapsed relative to utcNow.
func (b *SpaceBreadcrumb) IsExpired(utcNow time.Time) bool {
	return !utcNow.Before(b.CreatedAtUtc.Add(time.Duration(b.TtlHours) * time.Hour))
}
