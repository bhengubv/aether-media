// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;

namespace AetherMedia.LocalLibrary.Audio.Loudness;

/// <summary>
/// Default in-process <see cref="ILoudnessStore"/> backed by a thread-safe
/// dictionary. Non-persistent (lost on process restart); wrap with a
/// content-store-backed adapter for persistence.
/// </summary>
public sealed class InMemoryLoudnessStore : ILoudnessStore
{
    private readonly ConcurrentDictionary<string, LoudnessMeasurement> _store = new();

    /// <inheritdoc/>
    public int Count => _store.Count;

    /// <inheritdoc/>
    public Task<LoudnessMeasurement?> GetAsync(string contentHash, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentHash);
        _store.TryGetValue(contentHash, out var m);
        return Task.FromResult(m);
    }

    /// <inheritdoc/>
    public Task SetAsync(string contentHash, LoudnessMeasurement measurement, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentHash);
        ArgumentNullException.ThrowIfNull(measurement);
        _store[contentHash] = measurement;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> RemoveAsync(string contentHash, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentHash);
        return Task.FromResult(_store.TryRemove(contentHash, out _));
    }
}
