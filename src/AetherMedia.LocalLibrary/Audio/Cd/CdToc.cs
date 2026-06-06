// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Cd;

/// <summary>The table of contents of an audio CD.</summary>
public sealed record CdToc(IReadOnlyList<CdTrack> Tracks)
{
    /// <summary>Combined duration across all tracks.</summary>
    public TimeSpan TotalDuration =>
        TimeSpan.FromSeconds(Tracks.Sum(t => t.DurationSeconds));
}
