// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Cd;

/// <summary>One track on an audio CD.</summary>
/// <param name="Number">1-based track index as listed in the TOC.</param>
/// <param name="StartLba">Logical block address of the first sector.</param>
/// <param name="SectorCount">Number of 2352-byte audio sectors.</param>
/// <param name="IsAudio">False for data tracks (mixed-mode discs).</param>
public sealed record CdTrack(
    int Number,
    int StartLba,
    int SectorCount,
    bool IsAudio)
{
    /// <summary>Track duration in seconds — derived from sector count (75 sectors/sec).</summary>
    public double DurationSeconds => SectorCount / 75.0;

    /// <summary>Byte length of the raw PCM payload (2352 bytes per audio sector).</summary>
    public long RawByteLength => (long)SectorCount * 2352;
}
