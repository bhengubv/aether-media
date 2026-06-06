// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;

namespace AetherMedia.Market.Core;

/// <summary>
/// Input model used to create a new <see cref="MarketListing"/>.
/// </summary>
/// <param name="Title">Short title for the listing.</param>
/// <param name="Description">Full description of the item or service.</param>
/// <param name="PriceZAR">Asking price in South African Rand (must be &gt;= 0).</param>
/// <param name="Category">Category classifying the type of offering.</param>
/// <param name="DocumentPath">
/// Optional local file-system path to a document to store in escrow.
/// Required when <see cref="Category"/> is <see cref="MarketCategory.Land"/>
/// or <see cref="MarketCategory.Documents"/>.
/// </param>
public sealed record MarketItem(
    [property: JsonPropertyName("title")]         string          Title,
    [property: JsonPropertyName("description")]   string          Description,
    [property: JsonPropertyName("price_zar")]     decimal         PriceZAR,
    [property: JsonPropertyName("category")]      MarketCategory  Category,
    [property: JsonPropertyName("document_path")] string?         DocumentPath = null);
