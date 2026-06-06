// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Social;

/// <summary>
/// Decentralised follow graph for the Aether Media network.
/// All mutations are durable: follows are delivered via DTN (works offline),
/// unfollows are broadcast best-effort. Follower counts are maintained from
/// inbound follow bundles received from other nodes.
/// </summary>
public interface ISocialGraph
{
    /// <summary>Raised when the local node successfully follows a creator. Argument is the followed UHID.</summary>
    event EventHandler<string>? Followed;

    /// <summary>Raised when the local node unfollows a creator. Argument is the unfollowed UHID.</summary>
    event EventHandler<string>? Unfollowed;

    /// <summary>Follow a creator. The follow intent is queued as a DTN bundle so it survives offline periods.</summary>
    Task FollowAsync(string targetUhid, CancellationToken ct = default);

    /// <summary>Unfollow a creator. Broadcast best-effort over the mesh (no DTN delivery guarantee).</summary>
    Task UnfollowAsync(string targetUhid, CancellationToken ct = default);

    /// <summary>Returns true if the local node is following <paramref name="targetUhid"/>.</summary>
    Task<bool> IsFollowingAsync(string targetUhid, CancellationToken ct = default);

    /// <summary>Returns the full set of UHIDs the local node is following.</summary>
    Task<IReadOnlyList<string>> GetFollowingAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the follower count for <paramref name="targetUhid"/> as tracked by inbound
    /// follow bundles received on this node. May differ from the authoritative server count.
    /// </summary>
    Task<int> GetFollowerCountAsync(string targetUhid, CancellationToken ct = default);
}
