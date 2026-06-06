using AetherNet.Media.Core.Models;

namespace AetherNet.Media.Core;

/// <summary>
/// Provides a personalised, paginated stream of <see cref="MediaFeedItem"/>
/// entries and nearby live broadcasts for the authenticated user.
/// </summary>
public interface IMediaFeed
{
    // ── Events ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when a new item arrives in the feed (e.g. pushed by a
    /// subscribed creator or relayed by the local Aether mesh node).
    /// </summary>
    event EventHandler<MediaFeedItem>? ItemAdded;

    // ── Queries ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a page of feed items ordered by publication date descending.
    /// </summary>
    /// <param name="limit">Maximum number of items to return (default 50).</param>
    /// <param name="offset">Number of items to skip for pagination (default 0).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<MediaFeedItem>> GetFeedAsync(
        int limit = 50,
        int offset = 0,
        CancellationToken ct = default);

    /// <summary>
    /// Returns live streams whose broadcast origin node is geographically or
    /// topologically close to the local Aether node.  Implementations should
    /// favour streams with the fewest relay hops.
    /// </summary>
    Task<IReadOnlyList<LiveStream>> GetNearbyLiveStreamsAsync(CancellationToken ct = default);

    // ── Mutations ──────────────────────────────────────────────────────────

    /// <summary>
    /// Records a watch event for <paramref name="contentHash"/>.
    /// <paramref name="watchedMs"/> is the number of milliseconds of the item
    /// that were actually rendered (not counting buffering pauses).  This data
    /// feeds the recommendation and creator-analytics pipelines.
    /// </summary>
    Task MarkWatchedAsync(string contentHash, long watchedMs, CancellationToken ct = default);

    /// <summary>
    /// Triggers a full refresh of the feed from the upstream node or cache.
    /// Implementations should raise <see cref="ItemAdded"/> for each new item
    /// discovered during the refresh.
    /// </summary>
    Task RefreshAsync(CancellationToken ct = default);
}
