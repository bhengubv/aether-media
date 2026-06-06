// SPDX-License-Identifier: MIT

using AetherMedia.Identity.Tests.Helpers;

namespace AetherMedia.Identity.Tests;

/// <summary>Unit tests for <see cref="AvatarService"/>.</summary>
public sealed class AvatarServiceTests
{
    // ── Constructor ────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullContent_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new AvatarService(null!));

    // ── PublishAvatarAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task PublishAvatar_ReturnsNonEmptyHash()
    {
        var content = new FakeContentService();
        var service = new AvatarService(content);

        var hash = await service.PublishAvatarAsync(new byte[] { 1, 2, 3 }, "image/png");

        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public async Task PublishAvatar_NullBytes_Throws()
    {
        var service = new AvatarService(new FakeContentService());
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.PublishAvatarAsync(null!, "image/png"));
    }

    [Fact]
    public async Task PublishAvatar_EmptyBytes_Throws()
    {
        var service = new AvatarService(new FakeContentService());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.PublishAvatarAsync(Array.Empty<byte>(), "image/png"));
    }

    [Fact]
    public async Task PublishAvatar_BlankMimeType_Throws()
    {
        var service = new AvatarService(new FakeContentService());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.PublishAvatarAsync(new byte[] { 1 }, ""));
    }

    // ── GetLocalAvatarHashAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetLocalAvatarHash_BeforePublish_ReturnsNull()
    {
        var service = new AvatarService(new FakeContentService());
        var hash = await service.GetLocalAvatarHashAsync();
        Assert.Null(hash);
    }

    [Fact]
    public async Task GetLocalAvatarHash_AfterPublish_ReturnsNonNullHash()
    {
        var content = new FakeContentService();
        var service = new AvatarService(content);

        await service.PublishAvatarAsync(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, "image/jpeg");
        var hash = await service.GetLocalAvatarHashAsync();

        Assert.NotNull(hash);
        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    // ── FetchAvatarAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task FetchAvatar_AfterPublish_ReturnsCachedBytes_WithoutNetworkCall()
    {
        var content = new FakeContentService();
        var service = new AvatarService(content);
        var imageBytes = new byte[] { 1, 2, 3, 4, 5 };

        var hash = await service.PublishAvatarAsync(imageBytes, "image/png");

        // FetchAvatar for the just-published hash must hit the local cache.
        var fetched = await service.FetchAvatarAsync(hash, "owner-uhid");

        Assert.NotNull(fetched);
        Assert.Equal(imageBytes, fetched);
    }

    [Fact]
    public async Task FetchAvatar_NotCachedAndAssembleFails_ReturnsNull()
    {
        var content = new FakeContentService { AssembleResult = null };
        var service = new AvatarService(content);

        var result = await service.FetchAvatarAsync("unknown-hash", "owner-uhid");

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchAvatar_NotCachedAndAssembleSucceeds_ReturnsBytesAndCaches()
    {
        var imageBytes = new byte[] { 9, 8, 7 };
        var content = new FakeContentService { AssembleResult = imageBytes };
        var service = new AvatarService(content);

        var result = await service.FetchAvatarAsync("some-hash", "peer-uhid");

        Assert.Equal(imageBytes, result);

        // Second fetch must be served from cache (AssembleResult = null to prove it).
        content.AssembleResult = null;
        var cached = await service.FetchAvatarAsync("some-hash", "peer-uhid");
        Assert.Equal(imageBytes, cached);
    }

    [Fact]
    public async Task FetchAvatar_BlankHash_Throws()
    {
        var service = new AvatarService(new FakeContentService());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.FetchAvatarAsync("", "owner"));
    }

    [Fact]
    public async Task FetchAvatar_BlankOwnerUhid_Throws()
    {
        var service = new AvatarService(new FakeContentService());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.FetchAvatarAsync("some-hash", ""));
    }
}
