// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Podcast;

/// <summary>
/// Manages the user's subscribed shows. Fetches feeds, tracks the last-seen
/// episode per show, surfaces fresh episodes to the UI.
/// </summary>
public interface IPodcastSubscriptionService
{
    /// <summary>Subscribe to a feed by URL; refreshes immediately and returns the resulting subscription.</summary>
    Task<PodcastSubscription> SubscribeAsync(Uri feedUrl, CancellationToken ct = default);

    /// <summary>Drop a subscription. No-op if not subscribed.</summary>
    Task UnsubscribeAsync(Uri feedUrl, CancellationToken ct = default);

    /// <summary>All current subscriptions.</summary>
    Task<IReadOnlyList<PodcastSubscription>> ListAsync(CancellationToken ct = default);

    /// <summary>
    /// Refresh every subscription. Yields the fresh episodes per subscription
    /// (episodes whose GUID is newer than the last seen).
    /// </summary>
    Task<IReadOnlyList<(PodcastSubscription Subscription, IReadOnlyList<PodcastEpisode> NewEpisodes)>>
        RefreshAllAsync(CancellationToken ct = default);
}
