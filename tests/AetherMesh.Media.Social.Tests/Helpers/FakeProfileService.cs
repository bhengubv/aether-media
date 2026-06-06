// SPDX-License-Identifier: MIT

using AetherMesh.Media.Core.Models;
using AetherMesh.Media.Identity;

namespace AetherMesh.Media.Social.Tests.Helpers;

/// <summary>
/// Configurable IProfileService stub. Tests may populate <see cref="Profiles"/>
/// to control what GetProfileAsync returns.
/// </summary>
internal sealed class FakeProfileService : IProfileService
{
    /// <summary>UHID → MediaProfile; if absent GetProfileAsync returns null.</summary>
    public Dictionary<string, MediaProfile> Profiles { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>If set, GetProfileAsync throws this exception (used to test error-path).</summary>
    public Exception? ThrowOnGet { get; set; }

    public Task<MediaProfile?> GetProfileAsync(string uhid, CancellationToken ct = default)
    {
        if (ThrowOnGet is not null) throw ThrowOnGet;
        Profiles.TryGetValue(uhid, out var profile);
        return Task.FromResult(profile);
    }

    public Task<MediaProfile?> GetByAetherMeshTagAsync(string aethermeshTag, CancellationToken ct = default) =>
        Task.FromResult<MediaProfile?>(null);

    public Task<MediaProfile> CreateProfileAsync(string displayName, string? bio, CancellationToken ct = default)
    {
        var profile = MakeProfile("local-uhid", displayName);
        return Task.FromResult(profile);
    }

    public Task<MediaProfile> UpdateProfileAsync(string uhid, string displayName, string? bio, CancellationToken ct = default)
    {
        var updated = MakeProfile(uhid, displayName);
        Profiles[uhid] = updated;
        return Task.FromResult(updated);
    }

    public Task<MediaProfile> GetLocalProfileAsync(CancellationToken ct = default) =>
        Task.FromResult(MakeProfile("local-uhid", "Local Test User"));

    // ── Helper ──────────────────────────────────────────────────────────────
    internal static MediaProfile MakeProfile(string uhid, string displayName) =>
        new MediaProfile(
            Uhid: uhid,
            DisplayName: displayName,
            AvatarHash: null,
            Bio: null,
            AetherMeshTagValue: string.Empty,
            FollowerCount: 0,
            FollowingCount: 0,
            ContentCount: 0,
            IsVerified: false,
            JoinedAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
}
