// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Audio.Loudness;

/// <summary>
/// Persistent store for loudness measurements keyed by content hash. Lets the
/// player apply ReplayGain on every play without re-running the analyser every
/// time — and lets a creator's measurement be gossiped to peers over the mesh
/// so each device doesn't have to decode the whole file just to learn its loudness.
/// </summary>
public interface ILoudnessStore
{
    /// <summary>
    /// Fetch the stored loudness measurement for <paramref name="contentHash"/>,
    /// or <c>null</c> when none exists yet.
    /// </summary>
    Task<LoudnessMeasurement?> GetAsync(string contentHash, CancellationToken ct = default);

    /// <summary>Store a fresh measurement, overwriting any prior entry.</summary>
    Task SetAsync(string contentHash, LoudnessMeasurement measurement, CancellationToken ct = default);

    /// <summary>Remove the measurement for <paramref name="contentHash"/>.</summary>
    Task<bool> RemoveAsync(string contentHash, CancellationToken ct = default);

    /// <summary>
    /// Number of entries currently stored. For diagnostics and cache-size
    /// dashboards.
    /// </summary>
    int Count { get; }
}
