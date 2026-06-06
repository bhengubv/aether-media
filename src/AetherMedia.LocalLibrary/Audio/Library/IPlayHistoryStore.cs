// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Library;

/// <summary>
/// Records and queries <see cref="PlayEvent"/>s. Backs Winamp's <c>Play
/// count</c>, <c>Most Played</c>, <c>Least Played</c>, and <c>Recently
/// Played</c> smart views.
/// </summary>
public interface IPlayHistoryStore
{
    /// <summary>Append a play event.</summary>
    Task RecordAsync(PlayEvent ev, CancellationToken ct = default);

    /// <summary>Aggregated stats for one file (PlayCount = 0 if never played).</summary>
    Task<PlayStatistics> GetAsync(string filePath, CancellationToken ct = default);

    /// <summary>Top-N most-played tracks by play count.</summary>
    Task<IReadOnlyList<PlayStatistics>> MostPlayedAsync(int limit, CancellationToken ct = default);

    /// <summary>Bottom-N least-played tracks (PlayCount &gt; 0).</summary>
    Task<IReadOnlyList<PlayStatistics>> LeastPlayedAsync(int limit, CancellationToken ct = default);

    /// <summary>Most-recently-played tracks (one entry per file, ordered by last play).</summary>
    Task<IReadOnlyList<PlayStatistics>> RecentlyPlayedAsync(int limit, CancellationToken ct = default);
}
