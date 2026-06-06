// SPDX-License-Identifier: MIT

using AetherNet.Media.Core.Models;

namespace AetherNet.Media.AI;

/// <summary>
/// Ranks a flat list of <see cref="MediaFeedItem"/> values for a specific viewer
/// using a composite score drawn from creator reputation, AI transport biases,
/// content recency, and engagement signals.
/// </summary>
public interface IContentRanker
{
    /// <summary>
    /// Returns a new list containing every item from <paramref name="items"/>
    /// sorted by composite score descending. Items whose creator has a confirmed
    /// High or Critical threat level are pushed to the bottom with a score of 0.
    /// </summary>
    /// <param name="items">Unordered feed items to rank.</param>
    /// <param name="viewerUhid">UHID of the viewer requesting the feed.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<IReadOnlyList<MediaFeedItem>> RankFeedAsync(
        IReadOnlyList<MediaFeedItem> items,
        string viewerUhid,
        CancellationToken ct = default);
}
