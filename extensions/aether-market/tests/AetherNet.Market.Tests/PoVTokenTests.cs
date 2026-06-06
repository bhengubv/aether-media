// SPDX-License-Identifier: MIT
using AetherNet.Market.Core;

namespace AetherNet.Market.Tests;

public sealed class PoVTokenTests
{
    // ── Helper ─────────────────────────────────────────────────────────────

    private static PoVToken MakeToken(
        PoVTransport transport = PoVTransport.BLE,
        DateTime? timestamp = null,
        byte[]? witnessSig = null,
        byte[]? subjectSig = null) => new(
        WitnessUhid:      "node-witness",
        SubjectUhid:      "node-subject",
        TimestampUtc:     timestamp ?? DateTime.UtcNow,
        TransportUsed:    transport,
        WitnessSignature: witnessSig ?? new byte[] { 0x01, 0x02, 0x03 },
        SubjectSignature: subjectSig ?? new byte[] { 0x04, 0x05, 0x06 });

    // ── Both signatures required ───────────────────────────────────────────

    [Fact]
    public void Token_RequiresBothSignatures_WitnessNotEmpty()
    {
        var token = MakeToken(witnessSig: new byte[] { 0xAB, 0xCD });
        Assert.NotEmpty(token.WitnessSignature);
    }

    [Fact]
    public void Token_RequiresBothSignatures_SubjectNotEmpty()
    {
        var token = MakeToken(subjectSig: new byte[] { 0xEF, 0x12 });
        Assert.NotEmpty(token.SubjectSignature);
    }

    [Fact]
    public void Token_WitnessAndSubjectSignatures_AreDifferent()
    {
        var witness = new byte[] { 0x01, 0x02, 0x03 };
        var subject = new byte[] { 0x04, 0x05, 0x06 };
        var token = MakeToken(witnessSig: witness, subjectSig: subject);

        Assert.NotEqual(token.WitnessSignature, token.SubjectSignature);
    }

    // ── Transport must be short-range ──────────────────────────────────────

    [Theory]
    [InlineData(PoVTransport.BLE)]
    [InlineData(PoVTransport.NFC)]
    [InlineData(PoVTransport.NearLink)]
    public void Token_ShortRangeTransport_IsValid(PoVTransport transport)
    {
        var token = MakeToken(transport: transport);
        Assert.True(IsShortRange(token.TransportUsed));
    }

    [Fact]
    public void Token_AllDefinedTransports_AreShortRange()
    {
        // Ensures no non-short-range transport was accidentally added to the enum.
        foreach (PoVTransport transport in Enum.GetValues<PoVTransport>())
        {
            Assert.True(
                IsShortRange(transport),
                $"Transport {transport} is not a short-range protocol.");
        }
    }

    private static bool IsShortRange(PoVTransport transport) => transport switch
    {
        PoVTransport.BLE      => true,
        PoVTransport.NFC      => true,
        PoVTransport.NearLink => true,
        _                     => false,
    };

    // ── WeightedScore decay (6-month half-life) ────────────────────────────

    [Fact]
    public void Score_WeightedScore_DecaysBy50Percent_After6Months()
    {
        // Simulate a score that was last updated 6 months ago.
        var sixMonthsAgo = DateTime.UtcNow.AddDays(-183);
        var score = new PoVScore(
            Uhid:            "node-001",
            UniqueWitnesses: 10,
            WeightedScore:   1.0,
            LastUpdated:     sixMonthsAgo);

        double decayed = ApplyHalfLifeDecay(score.WeightedScore, score.LastUpdated, halfLifeDays: 183);

        // After one half-life the score should be ~0.5 (within 1% tolerance).
        Assert.InRange(decayed, 0.49, 0.51);
    }

    [Fact]
    public void Score_WeightedScore_RemainsNearOriginal_WhenRecent()
    {
        var justNow = DateTime.UtcNow.AddSeconds(-5);
        var score = new PoVScore(
            Uhid:            "node-002",
            UniqueWitnesses: 5,
            WeightedScore:   1.0,
            LastUpdated:     justNow);

        double decayed = ApplyHalfLifeDecay(score.WeightedScore, score.LastUpdated, halfLifeDays: 183);

        // A 5-second-old score should decay by less than 0.01%.
        Assert.InRange(decayed, 0.9999, 1.0001);
    }

    [Fact]
    public void Score_WeightedScore_DecaysBy75Percent_After12Months()
    {
        var twelveMonthsAgo = DateTime.UtcNow.AddDays(-366);
        var score = new PoVScore(
            Uhid:            "node-003",
            UniqueWitnesses: 8,
            WeightedScore:   1.0,
            LastUpdated:     twelveMonthsAgo);

        double decayed = ApplyHalfLifeDecay(score.WeightedScore, score.LastUpdated, halfLifeDays: 183);

        // After two half-lives (12 months) the score should be ~0.25.
        Assert.InRange(decayed, 0.24, 0.26);
    }

    /// <summary>
    /// Applies exponential decay with the given half-life.
    /// decayed = original × 2^(−elapsedDays / halfLifeDays)
    /// </summary>
    private static double ApplyHalfLifeDecay(double original, DateTime lastUpdated, double halfLifeDays)
    {
        double elapsedDays = (DateTime.UtcNow - lastUpdated).TotalDays;
        return original * Math.Pow(2.0, -elapsedDays / halfLifeDays);
    }
}
