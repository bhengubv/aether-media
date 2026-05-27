// SPDX-License-Identifier: MIT
package market

import "context"

// IPoVService defines operations for issuing, accepting, verifying, and
// scoring Proof-of-Vicinity tokens on the Aether mesh.
type IPoVService interface {
	// IssueToken initiates a PoV handshake with subjectAetherTag and returns
	// a signed PoVToken on successful proximity confirmation.
	IssueToken(ctx context.Context, subjectAetherTag string) (*PoVToken, error)

	// AcceptToken accepts an inbound PoVToken, verifies both signatures, and
	// incorporates it into the local PoV score.
	AcceptToken(ctx context.Context, token *PoVToken) error

	// GetScore returns the current decay-adjusted PoVScore for the node
	// identified by uhid.
	GetScore(ctx context.Context, uhid string) (*PoVScore, error)

	// VerifyToken verifies the cryptographic signatures on token and returns
	// true when both the witness and subject signatures are valid.
	VerifyToken(ctx context.Context, token *PoVToken) (bool, error)

	// ReportDefection reports a node that defected from a confirmed trade.
	// evidence should be a serialised TradeEscrow or other signed artefact.
	ReportDefection(ctx context.Context, uhid string, evidence string) error
}

// IMarketService defines operations for publishing, browsing, and trading on
// the Aether Market decentralised peer-to-peer marketplace.
type IMarketService interface {
	// CreateListing creates and broadcasts a new MarketListing derived from item.
	// When item.DocumentPath is set, the document is encrypted and stored in
	// Aether Vault before the listing is broadcast.
	CreateListing(ctx context.Context, item *MarketItem) (*MarketListing, error)

	// BrowseNearby returns active listings within radiusCells geohash cells
	// of centerGeoHash.
	BrowseNearby(ctx context.Context, centerGeoHash string, radiusCells int) ([]*MarketListing, error)

	// Search searches active listings by free-text query and optional category filter.
	// Pass nil for category to search all categories.
	Search(ctx context.Context, query string, category *MarketCategory) ([]*MarketListing, error)

	// InitiateTrade initiates a trade against listing by creating a TradeEscrow
	// in TradeStateInitiated.
	InitiateTrade(ctx context.Context, listing *MarketListing) (*TradeEscrow, error)

	// ConfirmTrade records confirmation from role and advances the escrow state machine.
	ConfirmTrade(ctx context.Context, escrow *TradeEscrow, role TradeRole) (*TradeEscrow, error)

	// ReleaseDocument releases the escrow document to the buyer once the trade
	// reaches TradeStateSellerConfirmed.
	ReleaseDocument(ctx context.Context, escrow *TradeEscrow) error

	// Dispute opens a dispute on escrow, transitioning it to TradeStateDisputed
	// and broadcasting reason to mesh mediator nodes.
	Dispute(ctx context.Context, escrow *TradeEscrow, reason string) (*TradeEscrow, error)
}
