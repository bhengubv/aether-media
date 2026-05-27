// SPDX-License-Identifier: MIT
package space

import (
	"context"
	"io"
)

// ISpaceService defines operations for dropping, scanning, pinning, and
// deleting geo-anchored SpaceBreadcrumb entries on the Aether mesh.
type ISpaceService interface {
	// Drop creates and broadcasts a new breadcrumb at geoHash using the
	// payload from content.
	Drop(ctx context.Context, geoHash GeoHash, content io.Reader, btype BreadcrumbType, ttlHours int) (*SpaceBreadcrumb, error)

	// Scan returns all live (non-expired) breadcrumbs within radiusCells
	// geohash cells of geoHash.
	Scan(ctx context.Context, geoHash GeoHash, radiusCells int) ([]*SpaceBreadcrumb, error)

	// Pin caches a pre-existing breadcrumb on the local node without
	// re-broadcasting it.
	Pin(ctx context.Context, breadcrumb *SpaceBreadcrumb) error

	// Delete removes a breadcrumb from the local cache and broadcasts a
	// retract message to neighbouring nodes.
	Delete(ctx context.Context, breadcrumb *SpaceBreadcrumb) error
}

// BreadcrumbReceivedFunc is a callback invoked when a breadcrumb is received
// from the mesh.
type BreadcrumbReceivedFunc func(breadcrumb *SpaceBreadcrumb)

// SpaceService is the default implementation of ISpaceService.
// Embed this struct and override methods as needed for different transport
// backends.
type SpaceService struct {
	// OnBreadcrumbReceived is called whenever a breadcrumb arrives from the
	// mesh. May be nil.
	OnBreadcrumbReceived BreadcrumbReceivedFunc
}

// Drop implements ISpaceService.
func (s *SpaceService) Drop(
	ctx context.Context,
	geoHash GeoHash,
	content io.Reader,
	btype BreadcrumbType,
	ttlHours int,
) (*SpaceBreadcrumb, error) {
	// Production implementations would write content to the Aether mesh,
	// obtain the content hash, sign the breadcrumb, and broadcast it.
	// This stub returns an error indicating the method requires a real
	// transport backend.
	return nil, errNotImplemented("Drop")
}

// Scan implements ISpaceService.
func (s *SpaceService) Scan(
	ctx context.Context,
	geoHash GeoHash,
	radiusCells int,
) ([]*SpaceBreadcrumb, error) {
	return nil, errNotImplemented("Scan")
}

// Pin implements ISpaceService.
func (s *SpaceService) Pin(ctx context.Context, breadcrumb *SpaceBreadcrumb) error {
	return errNotImplemented("Pin")
}

// Delete implements ISpaceService.
func (s *SpaceService) Delete(ctx context.Context, breadcrumb *SpaceBreadcrumb) error {
	return errNotImplemented("Delete")
}

type notImplementedError struct{ method string }

func (e *notImplementedError) Error() string {
	return "SpaceService." + e.method + ": requires a transport backend implementation"
}

func errNotImplemented(method string) error { return &notImplementedError{method} }
