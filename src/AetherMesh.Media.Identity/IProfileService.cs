// SPDX-License-Identifier: MIT

using AetherMesh.Media.Core.Models;

namespace AetherMesh.Media.Identity;

/// <summary>
/// Manages creator profiles on the Aether Media network. Profiles are keyed by UHID
/// and resolved from an in-process store that is populated via ProfileSync packets.
/// </summary>
public interface IProfileService
{
    Task<MediaProfile> CreateProfileAsync(string displayName, string? bio, CancellationToken ct = default);
    Task<MediaProfile> UpdateProfileAsync(string uhid, string displayName, string? bio, CancellationToken ct = default);
    Task<MediaProfile?> GetProfileAsync(string uhid, CancellationToken ct = default);
    Task<MediaProfile?> GetByAetherMeshTagAsync(string aethermeshTag, CancellationToken ct = default);
    Task<MediaProfile> GetLocalProfileAsync(CancellationToken ct = default);
}
