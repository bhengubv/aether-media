// SPDX-License-Identifier: MIT

using AetherNet.Protocol;
using AetherNet.Media.Identity.Tests.Helpers;

namespace AetherNet.Media.Identity.Tests;

/// <summary>Unit tests for <see cref="ProfileService"/>.</summary>
public sealed class ProfileServiceTests
{
    // A deterministic 32-byte Ed25519-like test key (all zeroes is valid for hashing purposes).
    private static readonly byte[] TestKey = new byte[32];

    // ── Constructor ────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullSender_Throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            new ProfileService(null!, TestKey));

    [Fact]
    public void Constructor_NullKey_Throws()
    {
        var sender = new FakeMeshSender();
        Assert.Throws<ArgumentNullException>(() =>
            new ProfileService(sender, null!));
    }

    [Fact]
    public void Constructor_WrongKeyLength_Throws()
    {
        var sender = new FakeMeshSender();
        Assert.Throws<ArgumentException>(() =>
            new ProfileService(sender, new byte[16]));
    }

    [Fact]
    public void Constructor_ValidKey_DoesNotThrow()
    {
        var sender = new FakeMeshSender();
        var _ = new ProfileService(sender, TestKey);
    }

    // ── CreateProfileAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateProfile_ReturnsProfileWithCorrectDisplayName()
    {
        var sender  = new FakeMeshSender();
        var service = new ProfileService(sender, TestKey);

        var profile = await service.CreateProfileAsync("Thabo Bhengu", null);

        Assert.Equal("Thabo Bhengu", profile.DisplayName);
    }

    [Fact]
    public async Task CreateProfile_TrimsDisplayName()
    {
        var sender  = new FakeMeshSender();
        var service = new ProfileService(sender, TestKey);

        var profile = await service.CreateProfileAsync("  Alice  ", null);

        Assert.Equal("Alice", profile.DisplayName);
    }

    [Fact]
    public async Task CreateProfile_SetsUhidToSenderLocalUhid()
    {
        var sender = new FakeMeshSender { LocalUhid = "my-local-uhid" };
        var service = new ProfileService(sender, TestKey);

        var profile = await service.CreateProfileAsync("Alice", null);

        Assert.Equal("my-local-uhid", profile.Uhid);
    }

    [Fact]
    public async Task CreateProfile_BroadcastsProfileSyncPacket()
    {
        var sender  = new FakeMeshSender();
        var service = new ProfileService(sender, TestKey);

        await service.CreateProfileAsync("Bob", null);

        Assert.Single(sender.BroadcastedPackets,
            p => p.Type == PacketType.ProfileSync);
    }

    [Fact]
    public async Task CreateProfile_ThenGetLocalProfile_ReturnsSameProfile()
    {
        var sender  = new FakeMeshSender();
        var service = new ProfileService(sender, TestKey);

        var created = await service.CreateProfileAsync("Carol", null);
        var fetched = await service.GetLocalProfileAsync();

        Assert.Equal(created.DisplayName, fetched.DisplayName);
        Assert.Equal(created.Uhid,        fetched.Uhid);
    }

    [Fact]
    public async Task CreateProfile_ThenGetProfileAsync_ReturnsProfile()
    {
        var sender  = new FakeMeshSender { LocalUhid = "uhid-test" };
        var service = new ProfileService(sender, TestKey);

        await service.CreateProfileAsync("Dave", null);

        var profile = await service.GetProfileAsync("uhid-test");
        Assert.NotNull(profile);
        Assert.Equal("Dave", profile.DisplayName);
    }

    [Fact]
    public async Task CreateProfile_EmptyDisplayName_Throws()
    {
        var service = new ProfileService(new FakeMeshSender(), TestKey);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateProfileAsync("", null));
    }

    // ── UpdateProfileAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateProfile_ChangesDisplayName()
    {
        var sender  = new FakeMeshSender { LocalUhid = "local-uid" };
        var service = new ProfileService(sender, TestKey);
        await service.CreateProfileAsync("Original Name", null);

        var updated = await service.UpdateProfileAsync("local-uid", "Updated Name", null);

        Assert.Equal("Updated Name", updated.DisplayName);
    }

    [Fact]
    public async Task UpdateProfile_SetsBio()
    {
        var sender  = new FakeMeshSender { LocalUhid = "local-uid" };
        var service = new ProfileService(sender, TestKey);
        await service.CreateProfileAsync("Alice", null);

        var updated = await service.UpdateProfileAsync("local-uid", "Alice", "My bio");

        Assert.Equal("My bio", updated.Bio);
    }

    [Fact]
    public async Task UpdateProfile_UnknownUhid_Throws()
    {
        var service = new ProfileService(new FakeMeshSender(), TestKey);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateProfileAsync("no-such-uhid", "Name", null));
    }

    [Fact]
    public async Task UpdateProfile_LocalUhid_BroadcastsSync()
    {
        var sender  = new FakeMeshSender { LocalUhid = "local-uid" };
        var service = new ProfileService(sender, TestKey);
        await service.CreateProfileAsync("Alice", null);
        sender.BroadcastedPackets.Clear();

        await service.UpdateProfileAsync("local-uid", "Alice Updated", null);

        Assert.Single(sender.BroadcastedPackets);
    }

    // ── GetProfileAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfileAsync_UnknownUhid_ReturnsNull()
    {
        var service = new ProfileService(new FakeMeshSender(), TestKey);
        var profile = await service.GetProfileAsync("ghost");
        Assert.Null(profile);
    }

    // ── GetLocalProfileAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetLocalProfile_BeforeCreate_Throws()
    {
        var service = new ProfileService(new FakeMeshSender(), TestKey);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetLocalProfileAsync());
    }

    // ── GetByAetherNetTagAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetByAetherNetTagAsync_InvalidTag_ReturnsNull()
    {
        var service = new ProfileService(new FakeMeshSender(), TestKey);
        var profile = await service.GetByAetherNetTagAsync("not-a-valid-tag");
        Assert.Null(profile);
    }
}
