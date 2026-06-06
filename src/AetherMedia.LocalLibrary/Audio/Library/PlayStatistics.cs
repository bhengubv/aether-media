// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Library;

/// <summary>Aggregated play stats for one track.</summary>
public sealed record PlayStatistics(
    string FilePath,
    int PlayCount,
    long TotalListenedMs,
    DateTimeOffset? LastPlayedUtc);
