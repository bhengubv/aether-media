// SPDX-License-Identifier: MIT
using Aether.Space.Core;

namespace Aether.Space.Tests;

public sealed class GeoHashTests
{
    // ── Parse ──────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_ValidGeohash_ReturnsCorrectValue()
    {
        var hash = GeoHash.Parse("u4pruyd");
        Assert.Equal("u4pruyd", hash.Value);
    }

    [Fact]
    public void Parse_NormalisesToLowercase()
    {
        var hash = GeoHash.Parse("U4PRUYD");
        Assert.Equal("u4pruyd", hash.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyOrWhitespace_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => GeoHash.Parse(value));
    }

    [Fact]
    public void Parse_InvalidCharacter_Throws()
    {
        // 'a' is not in the Geohash base-32 alphabet.
        Assert.Throws<ArgumentException>(() => GeoHash.Parse("u4pruya"));
    }

    // ── FromCoordinates ────────────────────────────────────────────────────

    [Fact]
    public void FromCoordinates_Precision6_Produces6CharHash()
    {
        var hash = GeoHash.FromCoordinates(0, 0, 6);
        Assert.Equal(6, hash.Value.Length);
    }

    [Theory]
    [InlineData(-91.0, 0)]
    [InlineData(91.0, 0)]
    [InlineData(0, -181.0)]
    [InlineData(0, 181.0)]
    public void FromCoordinates_OutOfRange_Throws(double lat, double lon)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => GeoHash.FromCoordinates(lat, lon));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void FromCoordinates_InvalidPrecision_Throws(int precision)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GeoHash.FromCoordinates(0, 0, precision));
    }

    [Fact]
    public void FromCoordinates_KnownPoint_London()
    {
        // London: 51.5074, -0.1278 → known prefix "gcpvj"
        var hash = GeoHash.FromCoordinates(51.5074, -0.1278, 5);
        Assert.Equal(5, hash.Value.Length);
        Assert.StartsWith("gcpvj", hash.Value, StringComparison.Ordinal);
    }

    // ── Implicit conversion ────────────────────────────────────────────────

    [Fact]
    public void ImplicitStringConversion_ReturnsValue()
    {
        var hash = GeoHash.Parse("u4pruyd");
        string str = hash;
        Assert.Equal("u4pruyd", str);
    }

    [Fact]
    public void ExplicitConversion_FromString_RoundTrips()
    {
        var hash1 = GeoHash.Parse("u4pruyd");
        var hash2 = (GeoHash)"u4pruyd";
        Assert.Equal(hash1, hash2);
    }
}
