// SPDX-License-Identifier: MIT
using System.Text.Json;
using System.Text.Json.Serialization;
using AetherMesh.Space.Core;

namespace AetherMesh.Space.Protocol;

/// <summary>
/// Wire representation of a <see cref="SpaceBreadcrumb"/> for transmission
/// over the Aether mesh.  Packet type discriminator is <c>40</c>.
/// </summary>
public readonly struct SpaceBreadcrumbPacket
{
    /// <summary>
    /// Mesh packet-type discriminator for Aether Space breadcrumb frames.
    /// </summary>
    public const int PacketType = 40;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>SHA-256 hex digest of the payload bytes.</summary>
    [JsonPropertyName("content_hash")]
    public string ContentHash { get; init; }

    /// <summary>Geohash cell in which the breadcrumb was dropped.</summary>
    [JsonPropertyName("geo_hash")]
    public string GeoHash { get; init; }

    /// <summary>Universal host ID of the originating node.</summary>
    [JsonPropertyName("anchor_uhid")]
    public string AnchorUhid { get; init; }

    /// <summary>UTC creation timestamp (Unix seconds).</summary>
    [JsonPropertyName("created_at_utc_unix")]
    public long CreatedAtUtcUnix { get; init; }

    /// <summary>Time-to-live in hours.</summary>
    [JsonPropertyName("ttl_hours")]
    public int TtlHours { get; init; }

    /// <summary>Numeric breadcrumb type (maps to <see cref="BreadcrumbType"/>).</summary>
    [JsonPropertyName("breadcrumb_type")]
    public int BreadcrumbType { get; init; }

    /// <summary>Ed25519 signature as Base64.</summary>
    [JsonPropertyName("signature_b64")]
    public string SignatureB64 { get; init; }

    // ── Conversion ─────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="SpaceBreadcrumbPacket"/> from a domain
    /// <see cref="SpaceBreadcrumb"/>.
    /// </summary>
    public static SpaceBreadcrumbPacket FromBreadcrumb(SpaceBreadcrumb breadcrumb) => new()
    {
        ContentHash      = breadcrumb.ContentHash,
        GeoHash          = breadcrumb.GeoHash,
        AnchorUhid       = breadcrumb.AnchorUhid,
        CreatedAtUtcUnix = new DateTimeOffset(breadcrumb.CreatedAtUtc, TimeSpan.Zero).ToUnixTimeSeconds(),
        TtlHours         = breadcrumb.TtlHours,
        BreadcrumbType   = (int)breadcrumb.Type,
        SignatureB64     = Convert.ToBase64String(breadcrumb.Signature),
    };

    /// <summary>
    /// Converts this packet back to a domain <see cref="SpaceBreadcrumb"/>.
    /// </summary>
    public SpaceBreadcrumb ToBreadcrumb() => new(
        ContentHash:  ContentHash,
        GeoHash:      GeoHash,
        AnchorUhid:   AnchorUhid,
        CreatedAtUtc: DateTimeOffset.FromUnixTimeSeconds(CreatedAtUtcUnix).UtcDateTime,
        TtlHours:     TtlHours,
        Type:         (Core.BreadcrumbType)BreadcrumbType,
        Signature:    Convert.FromBase64String(SignatureB64));

    // ── Serialisation ──────────────────────────────────────────────────────

    /// <summary>Serialises this packet to a UTF-8 JSON byte array.</summary>
    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this, SerializerOptions);

    /// <summary>
    /// Deserialises a <see cref="SpaceBreadcrumbPacket"/> from a UTF-8 JSON
    /// byte span.
    /// </summary>
    /// <exception cref="JsonException">Thrown when the bytes are not valid JSON.</exception>
    public static SpaceBreadcrumbPacket Deserialize(ReadOnlySpan<byte> utf8Json) =>
        JsonSerializer.Deserialize<SpaceBreadcrumbPacket>(utf8Json, SerializerOptions);
}
