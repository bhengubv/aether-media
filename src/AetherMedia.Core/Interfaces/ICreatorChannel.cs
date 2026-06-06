using AetherMedia.Core.Models;

namespace AetherMedia.Core;

/// <summary>
/// Provides access to a creator's public channel: their profile, uploaded
/// content, active live streams, and the viewer's subscription state.
/// </summary>
public interface ICreatorChannel
{
    /// <summary>
    /// Returns the public <see cref="MediaProfile"/> for the creator
    /// identified by <paramref name="creatorUhid"/>.
    /// </summary>
    Task<MediaProfile> GetProfileAsync(string creatorUhid, CancellationToken ct = default);

    /// <summary>
    /// Returns up to <paramref name="limit"/> pieces of content published by
    /// <paramref name="creatorUhid"/>, ordered by creation date descending.
    /// </summary>
    Task<IReadOnlyList<MediaContent>> GetContentAsync(
        string creatorUhid,
        int limit = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Returns all currently active live streams hosted by
    /// <paramref name="creatorUhid"/>.  Returns an empty list when the creator
    /// is not broadcasting.
    /// </summary>
    Task<IReadOnlyList<LiveStream>> GetLiveStreamsAsync(
        string creatorUhid,
        CancellationToken ct = default);

    /// <summary>
    /// Subscribes the authenticated viewer to <paramref name="creatorUhid"/>'s
    /// channel.  Future content and live-stream events from that creator will
    /// appear in the viewer's feed.  No-ops if already subscribed.
    /// </summary>
    Task SubscribeAsync(string creatorUhid, CancellationToken ct = default);

    /// <summary>
    /// Removes the authenticated viewer's subscription to
    /// <paramref name="creatorUhid"/>'s channel.  No-ops if not subscribed.
    /// </summary>
    Task UnsubscribeAsync(string creatorUhid, CancellationToken ct = default);

    /// <summary>
    /// Returns <c>true</c> when the authenticated viewer is subscribed to
    /// <paramref name="creatorUhid"/>'s channel.
    /// </summary>
    Task<bool> IsSubscribedAsync(string creatorUhid, CancellationToken ct = default);
}
