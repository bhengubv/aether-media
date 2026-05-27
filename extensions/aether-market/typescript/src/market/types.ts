// SPDX-License-Identifier: MIT

/** Short-range transport protocols valid for Proof-of-Vicinity attestation. */
export enum PoVTransport {
  /** Bluetooth Low Energy — ~10 m range. */
  BLE      = 0,
  /** Near Field Communication — ~4 cm range. */
  NFC      = 1,
  /** Huawei NearLink (SparkLink) — ~10 m range. */
  NearLink = 2,
}

/**
 * A cryptographically signed token proving that two devices were physically
 * co-located via a short-range transport at a specific moment in time.
 */
export interface PoVToken {
  /** Universal host ID of the witnessing device. */
  witnessUhid: string;
  /** Universal host ID of the subject device. */
  subjectUhid: string;
  /** UTC timestamp of the proximity event (ISO-8601 string). */
  timestampUtc: string;
  /** Short-range transport protocol used for the handshake. */
  transportUsed: PoVTransport;
  /** Ed25519 signature of the canonical payload by the witness (Base64). */
  witnessSignature: string;
  /** Ed25519 signature of the canonical payload by the subject (Base64). */
  subjectSignature: string;
}

/**
 * Aggregated Proof-of-Vicinity reputation score for a mesh node.
 * Scores decay over time via a 6-month half-life applied at query time.
 */
export interface PoVScore {
  /** Universal host ID of the node this score applies to. */
  uhid: string;
  /** Number of distinct UHIDs that have witnessed proximity with this node. */
  uniqueWitnesses: number;
  /** Decay-adjusted composite score. */
  weightedScore: number;
  /** UTC timestamp of the most recent PoV token factored into this score (ISO-8601 string). */
  lastUpdated: string;
}

/** Classifies what is being offered in a {@link MarketListing}. */
export enum MarketCategory {
  Goods     = 0,
  Services  = 1,
  Labour    = 2,
  Land      = 3,
  Documents = 4,
}

/** An immutable record describing a single item available for trade. */
export interface MarketListing {
  listingId: string;
  sellerUhid: string;
  sellerPoVScore: PoVScore;
  title: string;
  description: string;
  priceZAR: number;
  geoHash: string;
  category: MarketCategory;
  /** Present for Land/Documents categories; null otherwise. */
  escrowManifestJson: string | null;
  createdAtUtc: string;
  expiresAtUtc: string;
}

/** Trade lifecycle state machine values. */
export enum TradeState {
  Initiated       = 0,
  BuyerConfirmed  = 1,
  SellerConfirmed = 2,
  Complete        = 3,
  Disputed        = 4,
}

/** Participant role in a trade. */
export enum TradeRole {
  Buyer  = 0,
  Seller = 1,
}

/** Tracks the lifecycle of an escrow agreement between buyer and seller. */
export interface TradeEscrow {
  escrowId: string;
  listingId: string;
  buyerUhid: string;
  state: TradeState;
  vaultManifestJson: string;
}
