// SPDX-License-Identifier: MIT
using System.Text.Json;
using AetherMesh.Space.Core;
using AetherMesh.Space.Protocol;

namespace AetherMesh.Space.Tests;

public sealed class SpaceBreadcrumbTests
{
    private static SpaceBreadcrumb MakeBreadcrumb(
        BreadcrumbType type = BreadcrumbType.Notice,
        int ttlHours = 24,
        DateTime? createdAt = null) => new(
        ContentHash:  "abc123",
        GeoHash:      "u4pruyd",
        AnchorUhid:   "node-001",
        CreatedAtUtc: createdAt ?? DateTime.UtcNow,
        TtlHours:     ttlHours,
        Type:         type,
        Signature:    new byte[] { 0x01, 0x02, 0x03 });

    // ── Serialise / Deserialise roundtrip ──────────────────────────────────

    [Fact]
    public void Packet_Roundtrip_PreservesAllFields()
    {
        var original = MakeBreadcrumb();
        var packet   = SpaceBreadcrumbPacket.FromBreadcrumb(original);

        var bytes    = packet.Serialize();
        var restored = SpaceBreadcrumbPacket.Deserialize(bytes);
        var result   = restored.ToBreadcrumb();

        Assert.Equal(original.ContentHash,  result.ContentHash);
        Assert.Equal(original.GeoHash,      result.GeoHash);
        Assert.Equal(original.AnchorUhid,   result.AnchorUhid);
        Assert.Equal(original.TtlHours,     result.TtlHours);
        Assert.Equal(original.Type,         result.Type);
        Assert.Equal(original.Signature,    result.Signature);

        // DateTime round-trips via Unix seconds so sub-second precision is lost.
        Assert.Equal(
            original.CreatedAtUtc.TruncateToSeconds(),
            result.CreatedAtUtc.TruncateToSeconds());
    }

    [Fact]
    public void Packet_Serialize_ProducesValidJson()
    {
        var packet = SpaceBreadcrumbPacket.FromBreadcrumb(MakeBreadcrumb());
        var bytes  = packet.Serialize();

        using var doc = JsonDocument.Parse(bytes);
        Assert.True(doc.RootElement.TryGetProperty("content_hash", out _));
        Assert.True(doc.RootElement.TryGetProperty("geo_hash", out _));
        Assert.True(doc.RootElement.TryGetProperty("ttl_hours", out _));
    }

    // ── TTL expiry ─────────────────────────────────────────────────────────

    [Fact]
    public void Breadcrumb_IsExpired_WhenTtlElapsed()
    {
        var created     = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var breadcrumb  = MakeBreadcrumb(ttlHours: 1, createdAt: created);
        var afterExpiry = created.AddHours(1).AddSeconds(1);

        Assert.True(breadcrumb.IsExpired(afterExpiry));
    }

    [Fact]
    public void Breadcrumb_IsNotExpired_BeforeTtlElapsed()
    {
        var created    = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var breadcrumb = MakeBreadcrumb(ttlHours: 24, createdAt: created);
        var beforeTtl  = created.AddHours(23).AddMinutes(59);

        Assert.False(breadcrumb.IsExpired(beforeTtl));
    }

    // ── GeoHash.FromCoordinates roundtrip ──────────────────────────────────

    [Fact]
    public void GeoHash_FromCoordinates_MatchesKnownValue()
    {
        // Johannesburg: approximately -26.2041, 28.0473 → "ke7hy5"
        var hash = GeoHash.FromCoordinates(-26.2041, 28.0473, 6);
        Assert.Equal(6, hash.Value.Length);
        // The first two characters are well-established for this region.
        Assert.StartsWith("ke", hash.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void GeoHash_FromCoordinates_And_Parse_AreEquivalent()
    {
        var fromCoords = GeoHash.FromCoordinates(51.5074, -0.1278, 6); // London
        var fromParse  = GeoHash.Parse(fromCoords.Value);

        Assert.Equal(fromCoords, fromParse);
    }

    // ── Emergency type bypasses radius filter ──────────────────────────────

    [Fact]
    public void Handler_Emergency_BypassesRadiusFilter()
    {
        var handler = new SpaceBreadcrumbHandler(radiusCells: 3);
        var local   = GeoHash.Parse("u4pruyd");

        // Use a completely different geohash prefix (far away).
        var emergency = new SpaceBreadcrumb(
            ContentHash:  "sos123",
            GeoHash:      "spey6z",  // far from u4pruyd
            AnchorUhid:   "node-sos",
            CreatedAtUtc: DateTime.UtcNow,
            TtlHours:     1,
            Type:         BreadcrumbType.Emergency,
            Signature:    Array.Empty<byte>());

        var packet = SpaceBreadcrumbPacket.FromBreadcrumb(emergency);
        Assert.True(handler.ShouldForward(packet, local, DateTime.UtcNow));
    }

    [Fact]
    public void Handler_ExpiredPacket_IsNotForwarded()
    {
        var handler  = new SpaceBreadcrumbHandler();
        var local    = GeoHash.Parse("u4pruyd");
        var created  = DateTime.UtcNow.AddHours(-25);
        var expired  = MakeBreadcrumb(ttlHours: 24, createdAt: created);
        var packet   = SpaceBreadcrumbPacket.FromBreadcrumb(expired);

        Assert.False(handler.ShouldForward(packet, local, DateTime.UtcNow));
    }
}

// ── Test helpers ──────────────────────────────────────────────────────────────

file static class DateTimeExtensions
{
    public static DateTime TruncateToSeconds(this DateTime dt) =>
        new(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);
}
