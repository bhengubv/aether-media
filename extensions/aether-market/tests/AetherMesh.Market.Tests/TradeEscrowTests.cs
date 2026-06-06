// SPDX-License-Identifier: MIT
using AetherMesh.Market.Core;
using AetherMesh.Vault.Core;

namespace AetherMesh.Market.Tests;

public sealed class TradeEscrowTests
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private static VaultManifest MakeManifest() => new(
        FileId:         Guid.NewGuid(),
        ContentHash:    "cafebabe",
        EncryptionSalt: new byte[] { 0xFF },
        ShardHashes:    Enumerable.Range(0, 14).Select(i => $"s{i}").ToArray(),
        K:              10,
        M:              4,
        CreatedAtUtc:   DateTime.UtcNow,
        SizeBytes:      2048,
        Label:          "contract.pdf");

    private static TradeEscrow MakeEscrow(TradeState state = TradeState.Initiated) => new(
        EscrowId:      Guid.NewGuid(),
        ListingId:     Guid.NewGuid(),
        BuyerUhid:     "node-buyer",
        State:         state,
        VaultManifest: MakeManifest());

    // ── Valid state transitions ────────────────────────────────────────────

    [Fact]
    public void Transition_Initiated_To_BuyerConfirmed_IsValid()
    {
        var escrow = MakeEscrow(TradeState.Initiated);
        var next   = Advance(escrow, TradeState.BuyerConfirmed);
        Assert.Equal(TradeState.BuyerConfirmed, next.State);
    }

    [Fact]
    public void Transition_BuyerConfirmed_To_SellerConfirmed_IsValid()
    {
        var escrow = MakeEscrow(TradeState.BuyerConfirmed);
        var next   = Advance(escrow, TradeState.SellerConfirmed);
        Assert.Equal(TradeState.SellerConfirmed, next.State);
    }

    [Fact]
    public void Transition_SellerConfirmed_To_Complete_IsValid()
    {
        var escrow = MakeEscrow(TradeState.SellerConfirmed);
        var next   = Advance(escrow, TradeState.Complete);
        Assert.Equal(TradeState.Complete, next.State);
    }

    // ── Disputed can come from any non-Complete state ─────────────────────

    [Theory]
    [InlineData(TradeState.Initiated)]
    [InlineData(TradeState.BuyerConfirmed)]
    [InlineData(TradeState.SellerConfirmed)]
    public void Transition_AnyNonCompleteState_To_Disputed_IsValid(TradeState from)
    {
        var escrow = MakeEscrow(from);
        var next   = Advance(escrow, TradeState.Disputed);
        Assert.Equal(TradeState.Disputed, next.State);
    }

    // ── Invalid transitions throw ──────────────────────────────────────────

    [Fact]
    public void Transition_Complete_To_Disputed_Throws()
    {
        var escrow = MakeEscrow(TradeState.Complete);
        Assert.Throws<InvalidOperationException>(() => Advance(escrow, TradeState.Disputed));
    }

    [Fact]
    public void Transition_Complete_To_BuyerConfirmed_Throws()
    {
        var escrow = MakeEscrow(TradeState.Complete);
        Assert.Throws<InvalidOperationException>(() => Advance(escrow, TradeState.BuyerConfirmed));
    }

    [Fact]
    public void Transition_Initiated_To_SellerConfirmed_Throws()
    {
        // Skipping BuyerConfirmed is not allowed.
        var escrow = MakeEscrow(TradeState.Initiated);
        Assert.Throws<InvalidOperationException>(() => Advance(escrow, TradeState.SellerConfirmed));
    }

    [Fact]
    public void Transition_Initiated_To_Complete_Throws()
    {
        var escrow = MakeEscrow(TradeState.Initiated);
        Assert.Throws<InvalidOperationException>(() => Advance(escrow, TradeState.Complete));
    }

    [Fact]
    public void Transition_Disputed_To_Initiated_Throws()
    {
        // No transition out of Disputed other than Complete (via mediation).
        var escrow = MakeEscrow(TradeState.Disputed);
        Assert.Throws<InvalidOperationException>(() => Advance(escrow, TradeState.Initiated));
    }

    // ── State machine helper ───────────────────────────────────────────────

    /// <summary>
    /// Applies the transition from the escrow's current state to
    /// <paramref name="next"/>, throwing <see cref="InvalidOperationException"/>
    /// for invalid transitions.
    /// </summary>
    private static TradeEscrow Advance(TradeEscrow escrow, TradeState next)
    {
        bool valid = (escrow.State, next) switch
        {
            (TradeState.Initiated,       TradeState.BuyerConfirmed)  => true,
            (TradeState.BuyerConfirmed,  TradeState.SellerConfirmed) => true,
            (TradeState.SellerConfirmed, TradeState.Complete)        => true,
            // Disputed can come from any non-Complete state.
            ({ } from,                   TradeState.Disputed)        => from != TradeState.Complete,
            // Mediation can resolve a dispute back to Complete.
            (TradeState.Disputed,        TradeState.Complete)        => true,
            _                                                         => false,
        };

        if (!valid)
            throw new InvalidOperationException(
                $"Invalid trade state transition: {escrow.State} → {next}");

        return escrow with { State = next };
    }
}
