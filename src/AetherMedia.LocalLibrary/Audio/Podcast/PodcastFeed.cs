// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Podcast;

/// <summary>A parsed podcast RSS feed.</summary>
public sealed record PodcastFeed(
    string Title,
    string? Description,
    Uri? Link,
    Uri? ImageUrl,
    string? Language,
    IReadOnlyList<PodcastEpisode> Episodes);

/// <summary>One episode from a podcast feed.</summary>
public sealed record PodcastEpisode(
    string Guid,
    string Title,
    string? Description,
    DateTimeOffset PublishedAtUtc,
    Uri AudioUrl,
    long? LengthBytes,
    string? MimeType,
    TimeSpan? Duration);
