// SPDX-License-Identifier: MIT

using System.Text.Json;
using AetherNet.Media.Core.Models;
using AetherNet.Media.Identity.Tests.Helpers;
using AetherNet.Protocol;

namespace AetherNet.Media.Identity.Tests;

/// <summary>Unit tests for <see cref="ProfileSyncService"/>.</summary>
public sealed class ProfileSyncServiceTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    // ── Constructor ────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullProfileService_Throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ProfileSyncService(null!, new FakeMeshSender()));

    [Fact]
    public void Constructor_NullSender_Throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ProfileSyncService(new FakeProfileService(), null!));

    // ── HandleSyncPacketAsync — packet type filter ─────────────────────────

    [Fact]
    public async Task HandleSyncPacket_NonProfileSyncType_IsIgnored()
    {
        var service = MakeService(out _);

        var packet = new MeshPacket
        {
            Type    = PacketType.WatchSync,   // not ProfileSync
            Payload = JsonSerializer.SerializeToUtf8Bytes(MakeProfile("remote-1")),
        };

        // Must complete without error and fire no event.
        int eventFired = 0;
        service.ProfileReceived += (_, _) => eventFired++;

        await service.HandleSyncPacketAsync(packet);

        Assert.Equal(0, eventFired);
    }

    [Fact]
    public async Task HandleSyncPacket_NullPayload_IsIgnored()
    {
        var service = MakeService(out _);
        int eventFired = 0;
        service.ProfileReceived += (_, _) => eventFired++;

        var packet = new MeshPacket { Type = PacketType.ProfileSync, Payload = null! };
        await service.HandleSyncPacketAsync(packet);

        Assert.Equal(0, eventFired);
    }

    [Fact]
    public async Task HandleSyncPacket_EmptyPayload_IsIgnored()
    {
        var service = MakeService(out _);
        int eventFired = 0;
        service.ProfileReceived += (_, _) => eventFired++;

        var packet = new MeshPacket
        {
            Type    = PacketType.ProfileSync,
            Payload = Array.Empty<byte>(),
        };
        await service.HandleSyncPacketAsync(packet);

        Assert.Equal(0, eventFired);
    }

    // ── HandleSyncPacketAsync — valid packet ───────────────────────────────

    [Fact]
    public async Task HandleSyncPacket_ValidPacket_FiresProfileReceived()
    {
        var service = MakeService(out var sender);
        MediaProfile? received = null;
        service.ProfileReceived += (_, p) => received = p;

        await service.HandleSyncPacketAsync(MakePacket("remote-peer", sender.LocalUhid + "-not-me"));

        Assert.NotNull(received);
        Assert.Equal("remote-peer", received.Uhid);
    }

    [Fact]
    public async Task HandleSyncPacket_OwnUhidInPayload_IsIgnored()
    {
        var service = MakeService(out var sender);
        int eventFired = 0;
        service.ProfileReceived += (_, _) => eventFired++;

        // Packet carries our own UHID — should be silently dropped.
        await service.HandleSyncPacketAsync(MakePacket(sender.LocalUhid, sender.LocalUhid));

        Assert.Equal(0, eventFired);
    }

    [Fact]
    public async Task HandleSyncPacket_InvalidJson_DoesNotThrow()
    {
        var service = MakeService(out _);
        var packet = new MeshPacket
        {
            Type    = PacketType.ProfileSync,
            Payload = System.Text.Encoding.UTF8.GetBytes("this is not json"),
        };

        // Must not propagate the JSON exception.
        var ex = await Record.ExceptionAsync(() => service.HandleSyncPacketAsync(packet));
        Assert.Null(ex);
    }

    // ── SyncLocalProfileAsync ──────────────────────────────────────────────

    [Fact]
    public async Task SyncLocalProfile_BroadcastsProfileSyncPacket()
    {
        var sender = new FakeMeshSender { LocalUhid = "host-node" };
        var profileSvc = new FakeProfileService
        {
            LocalProfile = FakeProfileService.MakeProfile("host-node", "Local Host"),
        };
        var service = new ProfileSyncService(profileSvc, sender);

        await service.SyncLocalProfileAsync();

        Assert.Single(sender.BroadcastedPackets,
            p => p.Type == PacketType.ProfileSync);
    }

    [Fact]
    public async Task SyncLocalProfile_NoLocalProfile_DoesNotThrow()
    {
        // FakeProfileService with null LocalProfile throws InvalidOperationException.
        // ProfileSyncService must catch it and log, not propagate.
        var service = new ProfileSyncService(
            new FakeProfileService { LocalProfile = null },
            new FakeMeshSender());

        var ex = await Record.ExceptionAsync(() => service.SyncLocalProfileAsync());
        Assert.Null(ex);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static ProfileSyncService MakeService(out FakeMeshSender sender)
    {
        sender = new FakeMeshSender { LocalUhid = "local-node" };
        var profileSvc = new FakeProfileService
        {
            LocalProfile = FakeProfileService.MakeProfile("local-node", "Local Node"),
        };
        return new ProfileSyncService(profileSvc, sender);
    }

    private static MediaProfile MakeProfile(string uhid, string? displayName = null) =>
        new(Uhid: uhid, DisplayName: displayName ?? uhid, AvatarHash: null, Bio: null,
            AetherNetTagValue: string.Empty, FollowerCount: 0, FollowingCount: 0,
            ContentCount: 0, IsVerified: false, JoinedAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    private static MeshPacket MakePacket(string profileUhid, string senderUhid)
    {
        var profile = MakeProfile(profileUhid);
        return new MeshPacket
        {
            Type       = PacketType.ProfileSync,
            SourceUhid = senderUhid,
            Payload    = JsonSerializer.SerializeToUtf8Bytes(profile, JsonOpts),
        };
    }
}
