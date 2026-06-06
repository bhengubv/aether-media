// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Podcast;

/// <summary>One subscribed show.</summary>
public sealed record PodcastSubscription(
    Uri FeedUrl,
    string Title,
    DateTimeOffset SubscribedAtUtc,
    DateTimeOffset? LastRefreshedUtc,
    string? LastSeenEpisodeGuid);
