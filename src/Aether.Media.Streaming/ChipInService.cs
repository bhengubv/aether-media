// SPDX-License-Identifier: MIT
// ChipIn pools, contributions, and state are defined in Aether.Streaming.Models
// (part of the Aether Protocol).  This service manages the lifecycle on the
// local node and delegates transport to IWatchTogetherService.
using Aether.Streaming;
using Aether.Streaming.Models;
using System.Collections.Concurrent;

namespace Aether.Media.Streaming;

/// <summary>
/// Manages active ChipIn sessions for the local Aether Media node.
///
/// The protocol types (<see cref="ChipInPool"/>, <see cref="ChipInContribution"/>,
/// <see cref="ChipInState"/>) come from <c>Aether.Streaming.Models</c>.
/// Transport is via <see cref="IWatchTogetherService.StartChipInAsync"/> and
/// <see cref="IWatchTogetherService.ContributeAsync"/>; this class tracks the
/// in-memory state for the UI layer.
/// </summary>
public sealed class ChipInManager
{
    private readonly ConcurrentDictionary<Guid, ChipInPool> _pools = new();

    /// <summary>
    /// Register a pool that was returned by
    /// <see cref="IWatchTogetherService.StartChipInAsync"/>.
    /// </summary>
    public void Track(ChipInPool pool)
    {
        ArgumentNullException.ThrowIfNull(pool);
        _pools[pool.Id] = pool;
    }

    /// <summary>
    /// Apply a contribution update that was received via
    /// <see cref="IWatchTogetherService.ChipInUpdated"/>.
    /// </summary>
    public void ApplyUpdate(ChipInPool updated)
    {
        ArgumentNullException.ThrowIfNull(updated);
        _pools[updated.Id] = updated;
    }

    /// <summary>Returns the tracked pool with the given ID, or <see langword="null"/>.</summary>
    public ChipInPool? GetPool(Guid poolId)
        => _pools.TryGetValue(poolId, out var p) ? p : null;

    /// <summary>All pools that are still accepting contributions.</summary>
    public IReadOnlyCollection<ChipInPool> ActivePools
        => _pools.Values.Where(p => p.State == ChipInState.Collecting).ToList();

    /// <summary>All tracked pools.</summary>
    public IReadOnlyCollection<ChipInPool> AllPools => _pools.Values.ToList();

    /// <summary>Remove a pool from local tracking (e.g. after it is refunded or acquired).</summary>
    public bool Untrack(Guid poolId) => _pools.TryRemove(poolId, out _);
}
