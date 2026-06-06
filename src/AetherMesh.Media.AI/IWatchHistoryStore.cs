// SPDX-License-Identifier: MIT

namespace AetherMesh.Media.AI;

/// <summary>
/// Records and retrieves per-viewer watch-completion signals used by
/// <see cref="IContentRanker"/> to personalise feed ranking.
///
/// <para>
/// All data stays on-device — nothing is transmitted to the mesh.
/// Implementations must be thread-safe; the store is typically shared
/// across the feed aggregator and the media player via a singleton
/// registration.
/// </para>
/// </summary>
public interface IWatchHistoryStore
{
    /// <summary>
    /// Records that <paramref name="viewerUhid"/> watched
    /// <paramref name="watchedMs"/> milliseconds of a piece of content whose
    /// full duration is <paramref name="durationMs"/>.
    /// </summary>
    /// <param name="viewerUhid">UHID of the viewer.</param>
    /// <param name="contentHash">SHA-256 hash identifying the content.</param>
    /// <param name="watchedMs">
    /// Milliseconds of content actually consumed (seek-adjusted).
    /// </param>
    /// <param name="durationMs">
    /// Total content duration in milliseconds. Pass 0 for live streams —
    /// any non-zero <paramref name="watchedMs"/> is treated as full completion.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask RecordWatchEventAsync(
        string viewerUhid,
        string contentHash,
        long   watchedMs,
        long   durationMs,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the viewer's historical completion rate for the given content
    /// as a value in [0, 1], or <see langword="null"/> when there is no history.
    /// </summary>
    /// <remarks>
    /// 0.0 = instantly skipped; 1.0 = watched to completion.
    /// Callers should treat <see langword="null"/> as a neutral 0.5 rather
    /// than penalising unseen content.
    /// </remarks>
    ValueTask<double?> GetCompletionRateAsync(
        string viewerUhid,
        string contentHash,
        CancellationToken ct = default);
}
