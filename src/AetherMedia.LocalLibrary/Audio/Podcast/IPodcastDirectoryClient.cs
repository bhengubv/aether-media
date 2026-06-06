// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Podcast;

/// <summary>Searchable podcast directory — the "browse new podcasts" tab.</summary>
public interface IPodcastDirectoryClient
{
    Task<IReadOnlyList<PodcastDirectoryResult>> SearchAsync(string query, int limit = 25, CancellationToken ct = default);
    Task<IReadOnlyList<PodcastDirectoryResult>> TrendingAsync(int limit = 25, CancellationToken ct = default);
}
