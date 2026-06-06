// SPDX-License-Identifier: MIT

using AetherMesh.Media.Core.Models;

namespace AetherMesh.Media.Identity.Tests.Helpers;

/// <summary>
/// Configurable IProfileService stub for ProfileSyncService tests.
/// Populate <see cref="LocalProfile"/> to control what GetLocalProfileAsync returns.
/// </summary>
internal sealed class FakeProfileService : IProfileService
{
    public MediaProfile? LocalProfile { get; set; }

    public Task<MediaProfile> GetLocalProfileAsync(CancellationToken ct = default)
    {
        if (LocalProfile is null)
            throw new InvalidOperationException("Local profile not set.");
        return Task.FromResult(LocalProfile);
    }

    public Task<MediaProfile?> GetProfileAsync(string uhid, CancellationToken ct = default)
        => Task.FromResult<MediaProfile?>(null);

    public Task<MediaProfile?> GetByAetherMeshTagAsync(string aethermeshTag, CancellationToken ct = default)
        => Task.FromResult<MediaProfile?>(null);

    public Task<MediaProfile> CreateProfileAsync(string displayName, string? bio, CancellationToken ct = default)
        => Task.FromResult(MakeProfile("local-uhid", displayName));

    public Task<MediaProfile> UpdateProfileAsync(string uhid, string displayName, string? bio, CancellationToken ct = default)
        => Task.FromResult(MakeProfile(uhid, displayName));

    internal static MediaProfile MakeProfile(string uhid, string displayName) =>
        new(Uhid: uhid, DisplayName: displayName, AvatarHash: null, Bio: null,
            AetherMeshTagValue: string.Empty, FollowerCount: 0, FollowingCount: 0,
            ContentCount: 0, IsVerified: false, JoinedAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
}
