// SPDX-License-Identifier: MIT

using AetherMesh.Extensibility;
using Aether.Media.AI.Tests.Helpers;
using AetherMesh.Protocol;

namespace Aether.Media.AI.Tests;

/// <summary>
/// Unit tests for <see cref="ContentModerator.AssessSocialPacketAsync"/>.
///
/// <para>
/// The method combines two independent signals — AI assessment and velocity burst
/// detection — and returns the higher of the two.  Both signals must be tested in
/// isolation and in combination.
/// </para>
/// </summary>
public sealed class ContentModeratorSocialTests
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private static MeshPacket MakePacket(
        PacketType type      = PacketType.WatchReaction,
        string sourceUhid    = "source-1")
        => new MeshPacket
        {
            Type            = type,
            SourceUhid      = sourceUhid,
            DestinationUhid = string.Empty,
            Ttl             = 5,
            Priority        = 0,
            Payload         = Array.Empty<byte>(),
        };

    // ── Null packet ────────────────────────────────────────────────────────

    [Fact]
    public async Task AssessSocialPacket_NullPacket_ReturnsNone()
    {
        var ai  = new FakeAiProvider();
        var mod = new ContentModerator(ai);

        var result = await mod.AssessSocialPacketAsync(null!);

        Assert.Equal(AiThreatLevel.None, result);
    }

    // ── AI unavailable ─────────────────────────────────────────────────────

    [Fact]
    public async Task AssessSocialPacket_AiUnavailable_NormalRate_ReturnsNone()
    {
        var ai  = new FakeAiProvider { Available = false };
        var mod = new ContentModerator(ai);

        var result = await mod.AssessSocialPacketAsync(MakePacket());

        Assert.Equal(AiThreatLevel.None, result);
    }

    // ── AI signal propagates when no burst ────────────────────────────────

    [Fact]
    public async Task AssessSocialPacket_AiHighThreat_NoBurst_ReturnsAiLevel()
    {
        var ai = new FakeAiProvider { ThreatLevel = AiThreatLevel.High };
        var mod = new ContentModerator(ai);

        var result = await mod.AssessSocialPacketAsync(MakePacket());

        Assert.Equal(AiThreatLevel.High, result);
    }

    // ── Velocity burst (WatchReaction) ────────────────────────────────────

    [Fact]
    public async Task AssessSocialPacket_ReactionBurstExceeded_ReturnsMedium()
    {
        // ReactionBurstThreshold = 20 per 30 s.
        // Sending 21 packets from the same source must trigger Medium.
        var ai  = new FakeAiProvider { Available = false }; // AI off → only velocity signal
        var mod = new ContentModerator(ai);

        const int burst = 21; // one above the threshold of 20
        AiThreatLevel last = AiThreatLevel.None;

        for (var i = 0; i < burst; i++)
            last = await mod.AssessSocialPacketAsync(
                MakePacket(PacketType.WatchReaction, "burst-source"));

        Assert.Equal(AiThreatLevel.Medium, last);
    }

    [Fact]
    public async Task AssessSocialPacket_ReactionBurstNotExceeded_ReturnsNone()
    {
        // Exactly at threshold (20) — must NOT trigger Medium.
        var ai  = new FakeAiProvider { Available = false };
        var mod = new ContentModerator(ai);

        AiThreatLevel last = AiThreatLevel.None;

        for (var i = 0; i < 20; i++)
            last = await mod.AssessSocialPacketAsync(
                MakePacket(PacketType.WatchReaction, "ok-source"));

        Assert.Equal(AiThreatLevel.None, last);
    }

    // ── Velocity burst (social — non-reaction) ────────────────────────────

    [Fact]
    public async Task AssessSocialPacket_SocialBurstExceeded_ReturnsMedium()
    {
        // SocialBurstThreshold = 5 per 60 s.  Sending 6 ProfileSync packets triggers Medium.
        var ai  = new FakeAiProvider { Available = false };
        var mod = new ContentModerator(ai);

        AiThreatLevel last = AiThreatLevel.None;

        for (var i = 0; i < 6; i++)
            last = await mod.AssessSocialPacketAsync(
                MakePacket(PacketType.ProfileSync, "follow-spammer"));

        Assert.Equal(AiThreatLevel.Medium, last);
    }

    [Fact]
    public async Task AssessSocialPacket_SocialBurstNotExceeded_ReturnsNone()
    {
        // Exactly at threshold (5) — must NOT trigger Medium.
        var ai  = new FakeAiProvider { Available = false };
        var mod = new ContentModerator(ai);

        AiThreatLevel last = AiThreatLevel.None;

        for (var i = 0; i < 5; i++)
            last = await mod.AssessSocialPacketAsync(
                MakePacket(PacketType.ProfileSync, "ok-source"));

        Assert.Equal(AiThreatLevel.None, last);
    }

    // ── Combined: velocity wins over AI None ──────────────────────────────

    [Fact]
    public async Task AssessSocialPacket_BurstWithNeutralAi_ReturnsMedium()
    {
        // AI available but returns None; velocity detects burst → Medium returned.
        var ai  = new FakeAiProvider { ThreatLevel = AiThreatLevel.None };
        var mod = new ContentModerator(ai);

        AiThreatLevel last = AiThreatLevel.None;

        for (var i = 0; i < 21; i++)
            last = await mod.AssessSocialPacketAsync(
                MakePacket(PacketType.WatchReaction, "combined-source"));

        Assert.Equal(AiThreatLevel.Medium, last);
    }

    // ── AI wins over velocity None ─────────────────────────────────────────

    [Fact]
    public async Task AssessSocialPacket_AiHighWithNoBurst_ReturnsHigh()
    {
        // Single packet → no burst; AI says High → High is returned.
        var ai  = new FakeAiProvider { ThreatLevel = AiThreatLevel.High };
        var mod = new ContentModerator(ai);

        var result = await mod.AssessSocialPacketAsync(
            MakePacket(PacketType.ProfileSync, "suspicious-source"));

        Assert.Equal(AiThreatLevel.High, result);
    }

    // ── Source isolation ─────────────────────────────────────────────────

    [Fact]
    public async Task AssessSocialPacket_BurstFromOneSource_DoesNotAffectOther()
    {
        var ai  = new FakeAiProvider { Available = false };
        var mod = new ContentModerator(ai);

        // Burst source fires 21 reactions
        for (var i = 0; i < 21; i++)
            await mod.AssessSocialPacketAsync(MakePacket(PacketType.WatchReaction, "noisy"));

        // A different source sends just 1 reaction
        var result = await mod.AssessSocialPacketAsync(
            MakePacket(PacketType.WatchReaction, "quiet"));

        Assert.Equal(AiThreatLevel.None, result);
    }
}
