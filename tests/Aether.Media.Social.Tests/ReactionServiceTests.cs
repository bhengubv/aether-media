// SPDX-License-Identifier: MIT

using System.Text;
using System.Text.Json;
using Aether.Media.Core.Models;
using Aether.Media.Social.Tests.Helpers;
using Aether.Models;
using Aether.Protocol;

namespace Aether.Media.Social.Tests;

/// <summary>
/// Unit tests for <see cref="ReactionService"/>: outbound send (targeted + broadcast),
/// inbound packet handling (deserialise, store, event), error paths, and the 200-reaction cap.
/// </summary>
public sealed class ReactionServiceTests
{
    // ── Factory ────────────────────────────────────────────────────────────

    private static (ReactionService svc, FakeMeshSender sender) Make()
    {
        var sender = new FakeMeshSender();
        return (new ReactionService(sender), sender);
    }

    private static MediaReaction MakeReaction(
        string contentHash = "content-abc",
        string fromUhid    = "viewer-001",
        string? message    = null) =>
        new MediaReaction(
            reactionId:  Guid.NewGuid(),
            contentHash: contentHash,
            fromUhid:    fromUhid,
            type:        MediaReactionType.Like,
            positionMs:  1_500,
            message:     message,
            sentAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    // ── SendReactionAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task SendReactionAsync_Throws_WhenReactionIsNull()
    {
        var (svc, _) = Make();
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => svc.SendReactionAsync(null!));
    }

    [Fact]
    public async Task SendReactionAsync_SendsToTargetedPeer_WhenPeerIsConnected()
    {
        var (svc, sender) = Make();

        var reaction    = MakeReaction(fromUhid: "creator-X");
        var connectedPeer = new PeerInfo { Uhid = "creator-X" };
        sender.Peers.Add(connectedPeer);

        await svc.SendReactionAsync(reaction);

        Assert.Single(sender.SentPackets);
        Assert.Empty(sender.BroadcastedPackets);
        Assert.Equal("creator-X", sender.SentPackets[0].NextHop);
        Assert.Equal(PacketType.WatchReaction, sender.SentPackets[0].Packet.Type);
    }

    [Fact]
    public async Task SendReactionAsync_Broadcasts_WhenNoPeerFound()
    {
        var (svc, sender) = Make();
        // No peers in sender.Peers

        await svc.SendReactionAsync(MakeReaction());

        Assert.Empty(sender.SentPackets);
        Assert.Single(sender.BroadcastedPackets);
        Assert.Equal(PacketType.WatchReaction, sender.BroadcastedPackets[0].Type);
    }

    [Fact]
    public async Task SendReactionAsync_PacketPayloadContainsContentHash()
    {
        var (svc, sender) = Make();

        await svc.SendReactionAsync(MakeReaction(contentHash: "hash-send-verify"));

        var packet = sender.BroadcastedPackets[0];
        var json   = Encoding.UTF8.GetString(packet.Payload);
        Assert.Contains("hash-send-verify", json);
    }

    [Fact]
    public async Task SendReactionAsync_SetsCorrectSourceUhid()
    {
        var (svc, sender) = Make();
        sender.LocalUhid = "my-local-node";

        await svc.SendReactionAsync(MakeReaction());

        var packet = sender.BroadcastedPackets[0];
        Assert.Equal("my-local-node", packet.SourceUhid);
    }

    // ── HandlePacketAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task HandlePacketAsync_IgnoresNonReactionPackets()
    {
        var (svc, _) = Make();

        var fired = false;
        svc.ReactionReceived += (_, _) => fired = true;

        await svc.HandlePacketAsync(new MeshPacket
        {
            Type    = PacketType.Hello,
            Payload = Encoding.UTF8.GetBytes("{}"),
        });

        Assert.False(fired);
    }

    [Fact]
    public async Task HandlePacketAsync_IgnoresMalformedJson()
    {
        var (svc, _) = Make();

        var fired = false;
        svc.ReactionReceived += (_, _) => fired = true;

        await svc.HandlePacketAsync(new MeshPacket
        {
            Type    = PacketType.WatchReaction,
            Payload = Encoding.UTF8.GetBytes("NOT JSON"),
        });

        Assert.False(fired);
    }

    [Fact]
    public async Task HandlePacketAsync_IgnoresEmptyContentHash()
    {
        var (svc, _) = Make();

        var fired = false;
        svc.ReactionReceived += (_, _) => fired = true;

        var json = JsonSerializer.Serialize(new { content_hash = "" });
        await svc.HandlePacketAsync(new MeshPacket
        {
            Type    = PacketType.WatchReaction,
            Payload = Encoding.UTF8.GetBytes(json),
        });

        Assert.False(fired);
    }

    [Fact]
    public async Task HandlePacketAsync_StoresReactionAndRaisesEvent()
    {
        var (svc, _) = Make();

        MediaReaction? received = null;
        svc.ReactionReceived += (_, r) => received = r;

        // Build a valid wire payload by round-tripping through SendReactionAsync
        var (svc2, sender2) = Make();
        var original = MakeReaction(contentHash: "hash-rtrip", fromUhid: "viewer-rt");
        await svc2.SendReactionAsync(original);

        var packet = sender2.BroadcastedPackets[0];
        await svc.HandlePacketAsync(packet);

        Assert.NotNull(received);
        Assert.Equal("hash-rtrip",  received!.ContentHash);
        Assert.Equal("viewer-rt",   received!.FromUhid);
        Assert.Equal(1_500,         received!.PositionMs);

        var stored = await svc.GetReactionsAsync("hash-rtrip");
        Assert.Single(stored);
    }

    [Fact]
    public async Task HandlePacketAsync_AssignsNewGuid_WhenReactionIdIsEmpty()
    {
        var (svc, _) = Make();

        // Craft a payload where reaction_id is the empty GUID
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var payload = new
        {
            reaction_id  = Guid.Empty,
            content_hash = "hash-newguid",
            from_uhid    = "viewer-X",
            type         = 0,
            position_ms  = 0L,
            message      = (string?)null,
            sent_at_ms   = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        await svc.HandlePacketAsync(new MeshPacket
        {
            Type    = PacketType.WatchReaction,
            Payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, options)),
        });

        var stored = await svc.GetReactionsAsync("hash-newguid");
        Assert.Single(stored);
        Assert.NotEqual(Guid.Empty, stored[0].ReactionId);
    }

    [Fact]
    public async Task HandlePacketAsync_FallsBackToSourceUhid_WhenFromUhidIsEmpty()
    {
        var (svc, _) = Make();

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var payload = new
        {
            reaction_id  = Guid.NewGuid(),
            content_hash = "hash-fallback",
            from_uhid    = "",           // empty → should use SourceUhid
            type         = 0,
            position_ms  = 0L,
            message      = (string?)null,
            sent_at_ms   = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        };

        await svc.HandlePacketAsync(new MeshPacket
        {
            Type        = PacketType.WatchReaction,
            SourceUhid  = "fallback-node",
            Payload     = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, options)),
        });

        var stored = await svc.GetReactionsAsync("hash-fallback");
        Assert.Single(stored);
        Assert.Equal("fallback-node", stored[0].FromUhid);
    }

    [Fact]
    public async Task HandlePacketAsync_RoundTrips_AllFields()
    {
        var (svc, _) = Make();

        // Build wire packet via another instance
        var (svc2, sender2) = Make();
        var original = new MediaReaction(
            reactionId:  Guid.Parse("11111111-2222-3333-4444-555555555555"),
            contentHash: "hash-roundtrip-full",
            fromUhid:    "viewer-roundtrip",
            type:        MediaReactionType.Comment,
            positionMs:  30_000,
            message:     "Nice shot!",
            sentAtMs:      new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds());

        await svc2.SendReactionAsync(original);
        await svc.HandlePacketAsync(sender2.BroadcastedPackets[0]);

        var stored = await svc.GetReactionsAsync("hash-roundtrip-full");
        Assert.Single(stored);
        var r = stored[0];
        Assert.Equal(original.ReactionId,   r.ReactionId);
        Assert.Equal(original.ContentHash,  r.ContentHash);
        Assert.Equal(original.FromUhid,     r.FromUhid);
        Assert.Equal(original.Type,         r.Type);
        Assert.Equal(original.PositionMs,   r.PositionMs);
        Assert.Equal(original.Message,      r.Message);
    }

    // ── GetReactionsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetReactionsAsync_ReturnsEmpty_ForUnknownHash()
    {
        var (svc, _) = Make();
        var reactions = await svc.GetReactionsAsync("does-not-exist");
        Assert.Empty(reactions);
    }

    [Fact]
    public async Task GetReactionsAsync_ReturnsEmpty_ForBlankHash()
    {
        var (svc, _) = Make();
        var reactions = await svc.GetReactionsAsync("   ");
        Assert.Empty(reactions);
    }

    [Fact]
    public async Task GetReactionsAsync_ReturnsNewestFirst()
    {
        var (svc, _) = Make();

        // Handle two packets sequentially
        for (var i = 1; i <= 3; i++)
        {
            var (svc2, sender2) = Make();
            await svc2.SendReactionAsync(MakeReaction(contentHash: "hash-order", fromUhid: $"viewer-{i:D3}"));
            await svc.HandlePacketAsync(sender2.BroadcastedPackets[0]);
        }

        var stored = await svc.GetReactionsAsync("hash-order");
        Assert.Equal(3, stored.Count);
        // Each insert goes at index 0 — last inserted is first
        Assert.Equal("viewer-003", stored[0].FromUhid);
        Assert.Equal("viewer-001", stored[2].FromUhid);
    }

    [Fact]
    public async Task HandlePacketAsync_CapsAt200Reactions()
    {
        var (svc, _) = Make();

        for (var i = 0; i < 210; i++)
        {
            var (svc2, sender2) = Make();
            await svc2.SendReactionAsync(MakeReaction(contentHash: "hash-cap", fromUhid: $"viewer-{i:D4}"));
            await svc.HandlePacketAsync(sender2.BroadcastedPackets[0]);
        }

        var stored = await svc.GetReactionsAsync("hash-cap");
        Assert.Equal(200, stored.Count);
    }
}
