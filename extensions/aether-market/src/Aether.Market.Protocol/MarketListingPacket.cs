// SPDX-License-Identifier: MIT
using System.Text.Json;
using System.Text.Json.Serialization;
using Aether.Market.Core;
using Aether.Vault.Core;

namespace Aether.Market.Protocol;

/// <summary>
/// Wire representation of a <see cref="MarketListing"/> for transmission over
/// the Aether mesh.  Packet type discriminator is <c>42</c>.
/// </summary>
public readonly struct MarketListingPacket
{
    /// <summary>
    /// Mesh packet-type discriminator for Aether Market listing frames.
    /// </summary>
    public const int PacketType = MarketProtocolConstants.MarketListing;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>UUID of the listing.</summary>
    [JsonPropertyName("listing_id")]
    public string ListingId { get; init; }

    /// <summary>Universal host ID of the seller.</summary>
    [JsonPropertyName("seller_uhid")]
    public string SellerUhid { get; init; }

    /// <summary>Short title for the listing.</summary>
    [JsonPropertyName("title")]
    public string Title { get; init; }

    /// <summary>Full description of the item or service.</summary>
    [JsonPropertyName("description")]
    public string Description { get; init; }

    /// <summary>Asking price in ZAR as a decimal string (avoids float precision loss).</summary>
    [JsonPropertyName("price_zar")]
    public string PriceZar { get; init; }

    /// <summary>Geohash cell of the seller's location.</summary>
    [JsonPropertyName("geo_hash")]
    public string GeoHash { get; init; }

    /// <summary>Numeric category code (maps to <see cref="MarketCategory"/>).</summary>
    [JsonPropertyName("category")]
    public int Category { get; init; }

    /// <summary>
    /// JSON-serialised <see cref="VaultManifest"/> if an escrow document is
    /// attached; <see langword="null"/> otherwise.
    /// </summary>
    [JsonPropertyName("escrow_manifest_json")]
    public string? EscrowManifestJson { get; init; }

    /// <summary>UTC creation timestamp as Unix seconds.</summary>
    [JsonPropertyName("created_at_unix")]
    public long CreatedAtUnix { get; init; }

    /// <summary>UTC expiry timestamp as Unix seconds.</summary>
    [JsonPropertyName("expires_at_unix")]
    public long ExpiresAtUnix { get; init; }

    // ── Conversion ─────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="MarketListingPacket"/> from a domain
    /// <see cref="MarketListing"/>.
    /// </summary>
    public static MarketListingPacket FromListing(MarketListing listing) => new()
    {
        ListingId          = listing.ListingId.ToString(),
        SellerUhid         = listing.SellerUhid,
        Title              = listing.Title,
        Description        = listing.Description,
        PriceZar           = listing.PriceZAR.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
        GeoHash            = listing.GeoHash,
        Category           = (int)listing.Category,
        EscrowManifestJson = listing.EscrowManifest is null
                                 ? null
                                 : JsonSerializer.Serialize(listing.EscrowManifest, SerializerOptions),
        CreatedAtUnix      = new DateTimeOffset(listing.CreatedAtUtc, TimeSpan.Zero).ToUnixTimeSeconds(),
        ExpiresAtUnix      = new DateTimeOffset(listing.ExpiresAtUtc, TimeSpan.Zero).ToUnixTimeSeconds(),
    };

    // ── Serialisation ──────────────────────────────────────────────────────

    /// <summary>Serialises this packet to a UTF-8 JSON byte array.</summary>
    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this, SerializerOptions);

    /// <summary>
    /// Deserialises a <see cref="MarketListingPacket"/> from a UTF-8 JSON byte span.
    /// </summary>
    /// <exception cref="JsonException">Thrown when the bytes are not valid JSON.</exception>
    public static MarketListingPacket Deserialize(ReadOnlySpan<byte> utf8Json) =>
        JsonSerializer.Deserialize<MarketListingPacket>(utf8Json, SerializerOptions);
}
