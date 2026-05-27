// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;
using Aether.Vault.Core;

namespace Aether.Market.Core;

/// <summary>
/// An immutable record describing a single item available for trade on the
/// Aether Market peer-to-peer marketplace.
/// </summary>
/// <param name="ListingId">Unique identifier for this listing.</param>
/// <param name="SellerUhid">Universal host ID of the seller node.</param>
/// <param name="SellerPoVScore">Current Proof-of-Vicinity reputation score of the seller.</param>
/// <param name="Title">Short title for this listing (displayed in browse views).</param>
/// <param name="Description">Full description of the item or service on offer.</param>
/// <param name="PriceZAR">Asking price in South African Rand (must be &gt;= 0).</param>
/// <param name="GeoHash">
/// Geohash cell where the seller is physically located.  Must be a valid
/// non-empty geohash string.
/// </param>
/// <param name="Category">Category classifying the type of offering.</param>
/// <param name="EscrowManifest">
/// Vault manifest for the document in escrow.  Required for
/// <see cref="MarketCategory.Land"/> and <see cref="MarketCategory.Documents"/>
/// listings; <see langword="null"/> for other categories.
/// </param>
/// <param name="CreatedAtUtc">UTC timestamp when the listing was created.</param>
/// <param name="ExpiresAtUtc">UTC timestamp after which the listing is no longer active.</param>
public sealed record MarketListing(
    [property: JsonPropertyName("listing_id")]        Guid           ListingId,
    [property: JsonPropertyName("seller_uhid")]       string         SellerUhid,
    [property: JsonPropertyName("seller_pov_score")]  PoVScore       SellerPoVScore,
    [property: JsonPropertyName("title")]             string         Title,
    [property: JsonPropertyName("description")]       string         Description,
    [property: JsonPropertyName("price_zar")]         decimal        PriceZAR,
    [property: JsonPropertyName("geo_hash")]          string         GeoHash,
    [property: JsonPropertyName("category")]          MarketCategory Category,
    [property: JsonPropertyName("escrow_manifest")]   VaultManifest? EscrowManifest,
    [property: JsonPropertyName("created_at_utc")]    DateTime       CreatedAtUtc,
    [property: JsonPropertyName("expires_at_utc")]    DateTime       ExpiresAtUtc);
