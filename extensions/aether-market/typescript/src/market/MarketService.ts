// SPDX-License-Identifier: MIT
import { MarketListing, TradeEscrow, TradeRole, MarketCategory } from "./types.js";

type ListingCallback = (listing: MarketListing) => void;

/** Input shape for {@link MarketService.createListing}. */
export interface MarketItem {
  title: string;
  description: string;
  priceZAR: number;
  category: MarketCategory;
  /** Optional path/URL to a document for escrow (required for Land/Documents). */
  documentPath?: string;
}

/**
 * Client-side facade for the Aether Market decentralised marketplace.
 *
 * Call {@link createListing} to publish a listing, {@link browseNearby} or
 * {@link search} to discover listings, {@link initiateTrade} to start a
 * trade, {@link confirmTrade} to advance the escrow state, and
 * {@link releaseDocument}/{@link dispute} for post-confirmation actions.
 */
export class MarketService {
  /** Invoked whenever a listing arrives from a nearby mesh node. */
  onListingReceived: ListingCallback | null = null;

  private readonly _baseUrl: string;

  constructor(baseUrl: string = "http://localhost:5290") {
    this._baseUrl = baseUrl.replace(/\/+$/, "");
  }

  /**
   * Creates and broadcasts a new listing.  If {@link item.documentPath} is
   * provided it is encrypted and stored in Aether Vault before broadcast.
   *
   * @param item  Input model describing the listing.
   */
  async createListing(item: MarketItem): Promise<MarketListing> {
    const res = await fetch(`${this._baseUrl}/market/listings`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(item),
    });

    if (!res.ok) {
      throw new Error(`MarketService.createListing failed: ${res.status} ${res.statusText}`);
    }

    return res.json() as Promise<MarketListing>;
  }

  /**
   * Returns active listings within {@link radiusCells} geohash cells of
   * {@link centerGeoHash}.
   *
   * @param centerGeoHash  Centre geohash cell to search from.
   * @param radiusCells    Number of cells to expand outward.
   */
  async browseNearby(centerGeoHash: string, radiusCells: number): Promise<MarketListing[]> {
    const params = new URLSearchParams({
      geo_hash:     centerGeoHash,
      radius_cells: String(radiusCells),
    });

    const res = await fetch(`${this._baseUrl}/market/listings/nearby?${params}`);

    if (!res.ok) {
      throw new Error(`MarketService.browseNearby failed: ${res.status} ${res.statusText}`);
    }

    return res.json() as Promise<MarketListing[]>;
  }

  /**
   * Searches active listings by free-text {@link query} and optional
   * {@link category} filter.
   *
   * @param query     Free-text search query.
   * @param category  Optional category to filter by.
   */
  async search(query: string, category?: MarketCategory): Promise<MarketListing[]> {
    const params = new URLSearchParams({ query });
    if (category !== undefined) params.set("category", String(category));

    const res = await fetch(`${this._baseUrl}/market/listings/search?${params}`);

    if (!res.ok) {
      throw new Error(`MarketService.search failed: ${res.status} ${res.statusText}`);
    }

    return res.json() as Promise<MarketListing[]>;
  }

  /**
   * Initiates a trade against {@link listing}, creating a
   * {@link TradeEscrow} in the Initiated state.
   *
   * @param listing  The listing to trade against.
   */
  async initiateTrade(listing: MarketListing): Promise<TradeEscrow> {
    const res = await fetch(`${this._baseUrl}/market/trades`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ listingId: listing.listingId }),
    });

    if (!res.ok) {
      throw new Error(`MarketService.initiateTrade failed: ${res.status} ${res.statusText}`);
    }

    return res.json() as Promise<TradeEscrow>;
  }

  /**
   * Records confirmation from {@link role} and advances the escrow state
   * machine.
   *
   * @param escrow  The escrow to confirm.
   * @param role    Whether the confirming party is the buyer or seller.
   */
  async confirmTrade(escrow: TradeEscrow, role: TradeRole): Promise<TradeEscrow> {
    const res = await fetch(`${this._baseUrl}/market/trades/${escrow.escrowId}/confirm`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ role }),
    });

    if (!res.ok) {
      throw new Error(`MarketService.confirmTrade failed: ${res.status} ${res.statusText}`);
    }

    return res.json() as Promise<TradeEscrow>;
  }

  /**
   * Releases the escrow document to the buyer once the trade reaches
   * SellerConfirmed.
   *
   * @param escrow  The escrow whose document should be released.
   */
  async releaseDocument(escrow: TradeEscrow): Promise<void> {
    const res = await fetch(
      `${this._baseUrl}/market/trades/${escrow.escrowId}/release`,
      { method: "POST" },
    );

    if (!res.ok) {
      throw new Error(`MarketService.releaseDocument failed: ${res.status} ${res.statusText}`);
    }
  }

  /**
   * Opens a dispute on {@link escrow}, transitioning it to Disputed.
   *
   * @param escrow   The escrow to dispute.
   * @param reason   Human-readable reason for the dispute.
   */
  async dispute(escrow: TradeEscrow, reason: string): Promise<TradeEscrow> {
    const res = await fetch(
      `${this._baseUrl}/market/trades/${escrow.escrowId}/dispute`,
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ reason }),
      },
    );

    if (!res.ok) {
      throw new Error(`MarketService.dispute failed: ${res.status} ${res.statusText}`);
    }

    return res.json() as Promise<TradeEscrow>;
  }
}
