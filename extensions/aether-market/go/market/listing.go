// SPDX-License-Identifier: MIT
package market

import "time"

// MarketCategory classifies what is being offered in a MarketListing.
type MarketCategory int

const (
	// MarketCategoryGoods represents physical or digital goods.
	MarketCategoryGoods MarketCategory = 0
	// MarketCategoryServices represents services offered by a person or business.
	MarketCategoryServices MarketCategory = 1
	// MarketCategoryLabour represents time-and-skills engagements (gig work, day labour).
	MarketCategoryLabour MarketCategory = 2
	// MarketCategoryLand represents real-estate or agricultural land listings.
	MarketCategoryLand MarketCategory = 3
	// MarketCategoryDocuments represents title deeds, certificates, contracts and other formal documents.
	// Listings in this category must include an EscrowManifestJSON.
	MarketCategoryDocuments MarketCategory = 4
)

// TradeState represents the state machine for a TradeEscrow.
type TradeState int

const (
	// TradeStateInitiated means the trade has been initiated by the buyer.
	TradeStateInitiated TradeState = 0
	// TradeStateBuyerConfirmed means the buyer has confirmed receipt intent and locked funds.
	TradeStateBuyerConfirmed TradeState = 1
	// TradeStateSellerConfirmed means the seller has confirmed delivery.
	TradeStateSellerConfirmed TradeState = 2
	// TradeStateComplete means both parties confirmed — trade is done.
	TradeStateComplete TradeState = 3
	// TradeStateDisputed means the trade is in dispute.
	TradeStateDisputed TradeState = 4
)

// TradeRole is the role a participant plays in a TradeEscrow.
type TradeRole int

const (
	// TradeRoleBuyer is the party purchasing the item or service.
	TradeRoleBuyer TradeRole = 0
	// TradeRoleSeller is the party offering the item or service for sale.
	TradeRoleSeller TradeRole = 1
)

// MarketListing is an immutable record describing a single item available for
// trade on the Aether Market peer-to-peer marketplace.
type MarketListing struct {
	// ListingId is the unique identifier for this listing (UUID string).
	ListingId string `json:"listing_id"`
	// SellerUhid is the universal host ID of the seller node.
	SellerUhid string `json:"seller_uhid"`
	// SellerPoVScore is the current Proof-of-Vicinity reputation score of the seller.
	SellerPoVScore PoVScore `json:"seller_pov_score"`
	// Title is the short title for this listing.
	Title string `json:"title"`
	// Description is the full description of the item or service.
	Description string `json:"description"`
	// PriceZAR is the asking price in South African Rand.
	PriceZAR float64 `json:"price_zar"`
	// GeoHash is the geohash cell where the seller is physically located.
	GeoHash string `json:"geo_hash"`
	// Category classifies the type of offering.
	Category MarketCategory `json:"category"`
	// EscrowManifestJSON is the JSON-serialised VaultManifest for the escrow
	// document. Empty string when no escrow document is attached.
	EscrowManifestJSON string `json:"escrow_manifest_json,omitempty"`
	// CreatedAtUtc is the UTC timestamp when the listing was created.
	CreatedAtUtc time.Time `json:"created_at_utc"`
	// ExpiresAtUtc is the UTC timestamp after which the listing is inactive.
	ExpiresAtUtc time.Time `json:"expires_at_utc"`
}

// TradeEscrow tracks the lifecycle of an escrow agreement between a buyer and
// seller for a specific MarketListing.
type TradeEscrow struct {
	// EscrowId is the unique identifier for this escrow agreement (UUID string).
	EscrowId string `json:"escrow_id"`
	// ListingId is the listing this escrow is associated with.
	ListingId string `json:"listing_id"`
	// BuyerUhid is the universal host ID of the buyer.
	BuyerUhid string `json:"buyer_uhid"`
	// State is the current state of the trade state machine.
	State TradeState `json:"state"`
	// VaultManifestJSON is the JSON-serialised VaultManifest for the escrow asset.
	VaultManifestJSON string `json:"vault_manifest_json"`
}

// MarketItem is the input model used to create a new MarketListing.
type MarketItem struct {
	// Title is the short title for the listing.
	Title string `json:"title"`
	// Description is the full description of the item or service.
	Description string `json:"description"`
	// PriceZAR is the asking price in South African Rand (must be >= 0).
	PriceZAR float64 `json:"price_zar"`
	// Category classifies the type of offering.
	Category MarketCategory `json:"category"`
	// DocumentPath is the optional local file-system path to a document for escrow.
	// Required when Category is MarketCategoryLand or MarketCategoryDocuments.
	DocumentPath string `json:"document_path,omitempty"`
}
