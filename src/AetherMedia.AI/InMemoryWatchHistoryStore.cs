// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;

namespace AetherMedia.AI;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="IWatchHistoryStore"/>.
///
/// <para>
/// Multiple watch events for the same viewer/content pair are blended with an
/// EWMA (α = <see cref="Alpha"/>) so that re-watches and partial re-views
/// update the signal without overwriting prior history.
/// </para>
///
/// <para>
/// The store is capped at <see cref="MaxEntriesPerViewer"/> entries per
/// viewer. When the cap is reached the oldest entry is evicted to prevent
/// unbounded memory growth on long-running nodes.
/// </para>
/// </summary>
public sealed class InMemoryWatchHistoryStore : IWatchHistoryStore
{
    // ── Configuration ──────────────────────────────────────────────────────

    /// <summary>
    /// Maximum number of distinct content entries tracked per viewer.
    /// </summary>
    public const int MaxEntriesPerViewer = 1_000;

    /// <summary>
    /// EWMA smoothing factor for blending repeated watch events.
    /// α = 0.4 weights the newest observation at 40 %; prior history at 60 %.
    /// </summary>
    private const double Alpha = 0.4;

    // ── Storage ─────────────────────────────────────────────────────────────
    // viewer UHID → per-viewer record (ordered dict + dedicated lock)

    private sealed class ViewerRecord
    {
        internal readonly Lock Gate = new();
        // Insertion-ordered: index 0 = oldest (evicted first)
        internal readonly OrderedDictionary<string, double> Dict = new();
    }

    private readonly ConcurrentDictionary<string, ViewerRecord> _store = new();

    // ── IWatchHistoryStore ──────────────────────────────────────────────────

    /// <inheritdoc/>
    public ValueTask RecordWatchEventAsync(
        string viewerUhid,
        string contentHash,
        long   watchedMs,
        long   durationMs,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(viewerUhid) ||
            string.IsNullOrWhiteSpace(contentHash))
            return ValueTask.CompletedTask;

        // Completion rate for this single event [0, 1]
        double observed;
        if (durationMs <= 0)
            // Live stream: any watch time counts as full completion
            observed = watchedMs > 0 ? 1.0 : 0.0;
        else
            observed = Math.Clamp((double)watchedMs / durationMs, 0.0, 1.0);

        var rec = _store.GetOrAdd(viewerUhid, _ => new ViewerRecord());

        lock (rec.Gate)
        {
            if (rec.Dict.TryGetValue(contentHash, out double prior))
            {
                // EWMA blend: new observation at α, prior history at 1 − α
                rec.Dict[contentHash] = Alpha * observed + (1.0 - Alpha) * prior;
            }
            else
            {
                // Evict the oldest entry when the cap is reached
                if (rec.Dict.Count >= MaxEntriesPerViewer)
                    rec.Dict.RemoveAt(0);

                rec.Dict.Add(contentHash, observed);
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask<double?> GetCompletionRateAsync(
        string viewerUhid,
        string contentHash,
        CancellationToken ct = default)
    {
        if (!_store.TryGetValue(viewerUhid, out var rec))
            return ValueTask.FromResult<double?>(null);

        lock (rec.Gate)
        {
            return rec.Dict.TryGetValue(contentHash, out double rate)
                ? ValueTask.FromResult<double?>(rate)
                : ValueTask.FromResult<double?>(null);
        }
    }
}
