// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Cd;

/// <summary>
/// Reads an audio CD's TOC and rips individual tracks to PCM byte streams.
/// Decoupled from the file format so the consumer can write WAV via
/// <c>WavExporter</c>, FLAC via a future encoder, or hand the bytes
/// straight to the codec engine.
/// </summary>
public interface ICdRipper
{
    /// <summary>Enumerate CD drives available on this system.</summary>
    IReadOnlyList<string> EnumerateDrives();

    /// <summary>Read the table of contents of the disc currently in <paramref name="drivePath"/>.</summary>
    Task<CdToc> ReadTocAsync(string drivePath, CancellationToken ct = default);

    /// <summary>
    /// Rip the audio track <paramref name="track"/> as raw 16-bit stereo PCM
    /// (2 channels × 16 bits × 44.1 kHz) into <paramref name="destination"/>.
    /// Use <c>WavExporter</c> on the destination buffer to produce a WAV
    /// file; the byte stream is the same little-endian PCM that fits in a
    /// WAV data chunk.
    /// </summary>
    Task RipTrackAsync(
        string drivePath,
        CdTrack track,
        Stream destination,
        IProgress<double>? progress = null,
        CancellationToken ct = default);
}
