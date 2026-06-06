// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;
using AetherNet.Vault.Core;

namespace AetherNet.Market.Core;

/// <summary>
/// Tracks the lifecycle of an escrow agreement between a buyer and seller
/// for a specific <see cref="MarketListing"/>.
/// The associated <see cref="VaultManifest"/> holds the document or asset
/// in escrow until both parties confirm the trade.
/// </summary>
/// <param name="EscrowId">Unique identifier for this escrow agreement.</param>
/// <param name="ListingId">The listing this escrow is associated with.</param>
/// <param name="BuyerUhid">Universal host ID of the buyer.</param>
/// <param name="State">Current state of the trade state machine.</param>
/// <param name="VaultManifest">Vault manifest for the document or asset held in escrow.</param>
public sealed record TradeEscrow(
    [property: JsonPropertyName("escrow_id")]       Guid          EscrowId,
    [property: JsonPropertyName("listing_id")]      Guid          ListingId,
    [property: JsonPropertyName("buyer_uhid")]      string        BuyerUhid,
    [property: JsonPropertyName("state")]           TradeState    State,
    [property: JsonPropertyName("vault_manifest")]  VaultManifest VaultManifest);
