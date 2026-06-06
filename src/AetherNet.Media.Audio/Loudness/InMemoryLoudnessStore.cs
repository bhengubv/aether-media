// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;

namespace AetherNet.Media.Audio.Loudness;

/// <summary>
/// Default in-process implementation of <see cref="ILoudnessStore"/> backed by
/// a <see cref="ConcurrentDictionary{TKey,TValue}"/>. Threadsafe; non-persistent
/// (lost on process restart). For persistence wrap with an
/// <c>AetherNet.Storage.IKeyValueStore</c>-backed adapter.
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
