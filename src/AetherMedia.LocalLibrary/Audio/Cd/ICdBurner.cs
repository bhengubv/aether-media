// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Cd;

/// <summary>
/// Burns a CDDA (audio CD) session to a blank CD-R or CD-RW. PCM tracks
/// arrive as 16-bit signed little-endian stereo at 44.1 kHz — the only
/// format an audio CD accepts. Convert from any other rate / depth before
/// handing the bytes here.
/// </summary>
public interface ICdBurner
{
    /// <summary>Enumerate CD-recorder devices visible to the system.</summary>
    IReadOnlyList<string> EnumerateRecorders();

    /// <summary>Burn the requested tracks. Progress reports 0..1 per completed track.</summary>
    Task BurnAsync(
        CdBurnRequest request,
        IProgress<double>? progress = null,
        CancellationToken ct = default);
}
