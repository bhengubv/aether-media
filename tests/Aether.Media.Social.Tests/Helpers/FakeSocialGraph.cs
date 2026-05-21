// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;

namespace Aether.Media.Social.Tests.Helpers;

/// <summary>
/// In-memory ISocialGraph stub — instant, no DTN, no mesh.
/// </summary>
internal sealed class FakeSocialGraph : ISocialGraph
{
    public event EventHandler<string>? Followed;
    public event EventHandler<string>? Unfollowed;

    private readonly ConcurrentDictionary<string, bool> _following =
        new(StringComparer.OrdinalIgnoreCase);

    // Allow tests to force a fixed response for every IsFollowingAsync call
    public bool? OverrideIsFollowing { get; set; }

    public Task FollowAsync(string targetUhid, CancellationToken ct = default)
    {
        _following[targetUhid] = true;
        Followed?.Invoke(this, targetUhid);
        return Task.CompletedTask;
    }

    public Task UnfollowAsync(string targetUhid, CancellationToken ct = default)
    {
        _following.TryRemove(targetUhid, out _);
        Unfollowed?.Invoke(this, targetUhid);
        return Task.CompletedTask;
    }

    public Task<bool> IsFollowingAsync(string targetUhid, CancellationToken ct = default) =>
        Task.FromResult(OverrideIsFollowing ?? _following.ContainsKey(targetUhid));

    public Task<IReadOnlyList<string>> GetFollowingAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>([.. _following.Keys]);

    public Task<int> GetFollowerCountAsync(string targetUhid, CancellationToken ct = default) =>
        Task.FromResult(0);
}
