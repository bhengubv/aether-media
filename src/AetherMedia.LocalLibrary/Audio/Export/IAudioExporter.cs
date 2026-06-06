// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Export;

/// <summary>
/// Writes processed PCM out to a file. The modern equivalent of Winamp's
/// burnlib / Portable plugins: take what the DSP chain produced, hand it to a
/// container.
/// </summary>
public interface IAudioExporter
{
    /// <summary>
    /// Write <paramref name="samples"/> to <paramref name="destinationPath"/>.
    /// </summary>
    Task ExportAsync(
        string destinationPath,
        ReadOnlyMemory<float> samples,
        int sampleRateHz,
        int channels,
        CancellationToken ct = default);
}
