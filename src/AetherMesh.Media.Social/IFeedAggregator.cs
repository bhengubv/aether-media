// SPDX-License-Identifier: MIT

using AetherMesh.Media.Core.Models;

namespace AetherMesh.Media.Social;

/// <summary>
/// Aggregates the local node's media feed from stream announcements and content
/// publications by followed creators.  Call <see cref="StartAsync"/> once to
/// begin listening to mesh events; call <see cref="StopAsync"/> to detach.
/// </summary>
public interface IFeedAggregator
{
    /// <summary>Raised when a new item (live stream or published content) arrives in the feed.</summary>
    event EventHandler<MediaFeedItem>? ItemArrived;

    /// <summary>
    /// Returns a paginated slice of the feed, newest first.
    /// </summary>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <param name="offset">Zero-based starting index within the sorted list.</param>
    Task<IReadOnlyList<MediaFeedItem>> GetFeedAsync(int limit = 50, int offset = 0, CancellationToken ct = default);

    /// <summary>Returns live streams currently active from followed or nearby creators.</summary>
    Task<IReadOnlyList<LiveStream>> GetNearbyLiveStreamsAsync(CancellationToken ct = default);

    /// <summary>
    /// Records that the local user watched <paramref name="watchedMs"/> milliseconds of
    /// <paramref name="contentHash"/>, incrementing that item's watch counter in the feed.
    /// </summary>
    Task MarkWatchedAsync(string contentHash, long watchedMs, CancellationToken ct = default);

    /// <summary>Start listening to mesh streaming and content events.</summary>
    Task StartAsync(CancellationToken ct = default);

    /// <summary>Stop listening and detach event handlers.</summary>
    Task StopAsync(CancellationToken ct = default);
}
