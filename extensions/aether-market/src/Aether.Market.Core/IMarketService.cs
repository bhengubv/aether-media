// SPDX-License-Identifier: MIT
namespace Aether.Market.Core;

/// <summary>
/// Provides operations for publishing, browsing, and trading on the Aether
/// Market decentralised peer-to-peer marketplace.
/// </summary>
public interface IMarketService
{
    // ── Observable ─────────────────────────────────────────────────────────

    /// <summary>
    /// Hot observable that emits each <see cref="MarketListing"/> as it
    /// arrives from a nearby mesh node.
    /// </summary>
    IObservable<MarketListing> ListingReceived { get; }

    // ── Mutations ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates and broadcasts a new <see cref="MarketListing"/> derived from
    /// <paramref name="item"/>.  When the item includes a
    /// <see cref="MarketItem.DocumentPath"/>, the document is encrypted and
    /// stored in Aether Vault before the listing is broadcast.
    /// </summary>
    /// <param name="item">Input model describing the listing to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created <see cref="MarketListing"/>.</returns>
    Task<MarketListing> CreateListingAsync(MarketItem item, CancellationToken ct = default);

    /// <summary>
    /// Initiates a trade against <paramref name="listing"/> by creating a
    /// <see cref="TradeEscrow"/> in <see cref="TradeState.Initiated"/> state.
    /// </summary>
    /// <param name="listing">The listing to trade against.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created <see cref="TradeEscrow"/>.</returns>
    Task<TradeEscrow> InitiateTradeAsync(MarketListing listing, CancellationToken ct = default);

    /// <summary>
    /// Records confirmation from <paramref name="role"/> and advances the
    /// escrow state machine.
    /// </summary>
    /// <param name="escrow">The escrow to confirm.</param>
    /// <param name="role">Whether the confirming party is the buyer or seller.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="TradeEscrow"/>.</returns>
    Task<TradeEscrow> ConfirmTradeAsync(TradeEscrow escrow, TradeRole role, CancellationToken ct = default);

    /// <summary>
    /// Releases the escrow document to the buyer once the trade reaches
    /// <see cref="TradeState.SellerConfirmed"/>.
    /// </summary>
    /// <param name="escrow">The escrow whose document should be released.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ReleaseDocumentAsync(TradeEscrow escrow, CancellationToken ct = default);

    /// <summary>
    /// Opens a dispute on <paramref name="escrow"/>, transitioning it to
    /// <see cref="TradeState.Disputed"/> and broadcasting the
    /// <paramref name="reason"/> to mesh mediator nodes.
    /// </summary>
    /// <param name="escrow">The escrow to dispute.</param>
    /// <param name="reason">Human-readable reason for the dispute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="TradeEscrow"/>.</returns>
    Task<TradeEscrow> DisputeAsync(TradeEscrow escrow, string reason, CancellationToken ct = default);

    // ── Queries ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns active listings within <paramref name="radiusCells"/> geohash
    /// cells of <paramref name="centerGeoHash"/>.
    /// </summary>
    /// <param name="centerGeoHash">Centre geohash cell to search from.</param>
    /// <param name="radiusCells">Number of cells to expand outward.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<MarketListing>> BrowseNearbyAsync(
        string centerGeoHash,
        int radiusCells,
        CancellationToken ct = default);

    /// <summary>
    /// Searches active listings by free-text <paramref name="query"/> and
    /// optional <paramref name="category"/> filter.
    /// </summary>
    /// <param name="query">Full-text search query.</param>
    /// <param name="category">Optional category to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<MarketListing>> SearchAsync(
        string query,
        MarketCategory? category,
        CancellationToken ct = default);
}
