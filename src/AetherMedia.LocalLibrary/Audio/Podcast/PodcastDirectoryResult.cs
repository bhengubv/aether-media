// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Podcast;

/// <summary>One hit from a podcast-directory search.</summary>
public sealed record PodcastDirectoryResult(
    long Id,
    string Title,
    string? Author,
    Uri FeedUrl,
    Uri? Homepage,
    Uri? ImageUrl,
    string? Categories,
    string? Description);
