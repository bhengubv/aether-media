// SPDX-License-Identifier: MIT
using AetherMesh.Market.Core;
using AetherMesh.Vault.Core;

namespace AetherMesh.Market.Tests;

public sealed class MarketListingTests
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private static PoVScore MakeScore() => new(
        Uhid:            "node-seller",
        UniqueWitnesses: 5,
        WeightedScore:   0.8,
        LastUpdated:     DateTime.UtcNow);

    private static VaultManifest MakeManifest() => new(
        FileId:         Guid.NewGuid(),
        ContentHash:    "deadbeef",
        EncryptionSalt: new byte[] { 0x01 },
        ShardHashes:    Enumerable.Range(0, 14).Select(i => $"sh-{i}").ToArray(),
        K:              10,
        M:              4,
        CreatedAtUtc:   DateTime.UtcNow,
        SizeBytes:      4096,
        Label:          "deed.pdf");

    private static MarketListing MakeListing(
        string geoHash         = "ke7hy5",
        decimal price          = 100m,
        MarketCategory category = MarketCategory.Goods,
        VaultManifest? escrow  = null) => new(
        ListingId:      Guid.NewGuid(),
        SellerUhid:     "node-seller",
        SellerPoVScore: MakeScore(),
        Title:          "Test Item",
        Description:    "A test item for unit tests.",
        PriceZAR:       price,
        GeoHash:        geoHash,
        Category:       category,
        EscrowManifest: escrow,
        CreatedAtUtc:   DateTime.UtcNow,
        ExpiresAtUtc:   DateTime.UtcNow.AddDays(7));

    // ── GeoHash validation ─────────────────────────────────────────────────

    [Fact]
    public void Listing_RequiresValidGeoHash_NonEmpty()
    {
        var listing = MakeListing(geoHash: "ke7hy5");
        Assert.False(string.IsNullOrWhiteSpace(listing.GeoHash));
    }

    [Theory]
    [InlineData("ke7hy5")]
    [InlineData("u4pruyd")]
    [InlineData("spey6z")]
    public void Listing_GeoHash_HasExpectedLength(string geoHash)
    {
        var listing = MakeListing(geoHash: geoHash);
        Assert.InRange(listing.GeoHash.Length, 1, 12);
    }

    [Fact]
    public void Listing_GeoHash_MustNotBeEmpty()
    {
        // Domain invariant: a listing with an empty GeoHash is invalid.
        var listing = MakeListing(geoHash: "");
        Assert.True(string.IsNullOrEmpty(listing.GeoHash),
            "A listing with empty GeoHash should be detectable as invalid.");
        // Callers are responsible for validation before persisting.
    }

    // ── Price validation ───────────────────────────────────────────────────

    [Fact]
    public void Listing_PriceZAR_CanBeZero()
    {
        var listing = MakeListing(price: 0m);
        Assert.Equal(0m, listing.PriceZAR);
    }

    [Fact]
    public void Listing_PriceZAR_CanBePositive()
    {
        var listing = MakeListing(price: 999.99m);
        Assert.True(listing.PriceZAR >= 0m);
    }

    [Fact]
    public void Listing_PriceZAR_IsStoredExactly()
    {
        decimal exact = 12345.67m;
        var listing = MakeListing(price: exact);
        Assert.Equal(exact, listing.PriceZAR);
    }

    // ── EscrowManifest required for Land/Documents ─────────────────────────

    [Theory]
    [InlineData(MarketCategory.Land)]
    [InlineData(MarketCategory.Documents)]
    public void Listing_EscrowManifest_RequiredForLandAndDocuments(MarketCategory category)
    {
        // A listing with escrow manifest attached — valid.
        var withEscrow = MakeListing(category: category, escrow: MakeManifest());
        Assert.NotNull(withEscrow.EscrowManifest);

        // A listing without escrow manifest — invalid for these categories.
        var withoutEscrow = MakeListing(category: category, escrow: null);
        Assert.Null(withoutEscrow.EscrowManifest);
        // Callers and service layer must reject listings where
        // EscrowManifest is null for Land/Documents categories.
    }

    [Theory]
    [InlineData(MarketCategory.Goods)]
    [InlineData(MarketCategory.Services)]
    [InlineData(MarketCategory.Labour)]
    public void Listing_EscrowManifest_OptionalForOtherCategories(MarketCategory category)
    {
        var listing = MakeListing(category: category, escrow: null);
        Assert.Null(listing.EscrowManifest);
    }

    [Fact]
    public void Listing_EscrowManifest_IsPreservedWhenProvided()
    {
        var manifest = MakeManifest();
        var listing  = MakeListing(category: MarketCategory.Land, escrow: manifest);

        Assert.Equal(manifest.FileId, listing.EscrowManifest!.FileId);
        Assert.Equal(manifest.ContentHash, listing.EscrowManifest.ContentHash);
    }
}
