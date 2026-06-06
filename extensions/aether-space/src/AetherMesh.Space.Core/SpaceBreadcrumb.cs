// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;

namespace AetherMesh.Space.Core;

/// <summary>
/// An immutable record that describes a single piece of geo-anchored content
/// dropped into the Aether Space layer.
/// </summary>
/// <param name="ContentHash">SHA-256 hex digest of the payload bytes.</param>
/// <param name="GeoHash">Geohash cell in which the breadcrumb was dropped.</param>
/// <param name="AnchorUhid">Universal host ID of the node that dropped this breadcrumb.</param>
/// <param name="CreatedAtUtc">UTC timestamp of creation.</param>
/// <param name="TtlHours">Time-to-live in hours before the breadcrumb is considered expired.</param>
/// <param name="Type">Semantic classification of the breadcrumb content.</param>
/// <param name="Signature">Ed25519 signature over the serialised fields, produced by the anchor node.</param>
public sealed record SpaceBreadcrumb(
    [property: JsonPropertyName("content_hash")]  string ContentHash,
    [property: JsonPropertyName("geo_hash")]      string GeoHash,
    [property: JsonPropertyName("anchor_uhid")]   string AnchorUhid,
    [property: JsonPropertyName("created_at_utc")] DateTime CreatedAtUtc,
    [property: JsonPropertyName("ttl_hours")]     int TtlHours,
    [property: JsonPropertyName("type")]          BreadcrumbType Type,
    [property: JsonPropertyName("signature")]     byte[] Signature)
{
    /// <summary>
    /// Returns <see langword="true"/> when the breadcrumb's TTL has elapsed
    /// relative to <paramref name="utcNow"/>.
    /// </summary>
    public bool IsExpired(DateTime utcNow) =>
        utcNow >= CreatedAtUtc.AddHours(TtlHours);
}
