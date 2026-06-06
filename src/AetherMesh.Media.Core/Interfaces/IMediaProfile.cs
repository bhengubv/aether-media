// SPDX-License-Identifier: MIT
namespace AetherMesh.Media.Core.Interfaces;

/// <summary>
/// Public profile of an Aether Media creator.
/// </summary>
public interface IMediaProfile
{
    string  Uhid          { get; }
    string  DisplayName   { get; }
    string? AvatarHash    { get; }
    string? Bio           { get; }
    string  AetherMeshTag     { get; }
    int     FollowerCount  { get; }
    int     FollowingCount { get; }
    int     ContentCount   { get; }
    bool    IsVerified     { get; }
    long    JoinedAtMs     { get; }
}
