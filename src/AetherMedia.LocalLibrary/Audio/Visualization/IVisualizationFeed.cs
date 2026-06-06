// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Visualization;

/// <summary>
/// Produces visualisation frames (spectrum + waveform) from real-time PCM.
/// Hand a buffer in, get a <see cref="SpectrumFrame"/> out — the player's UI
/// renders the bars or curves.
/// </summary>
public interface IVisualizationFeed
{
    /// <summary>FFT window size in samples (power of 2, e.g. 1024, 2048, 4096).</summary>
    int FftSize { get; }

    /// <summary>
    /// Analyse one buffer of interleaved 32-bit float PCM. Multi-channel is
    /// downmixed to mono before transformation. Returns null when the buffer
    /// has fewer than <see cref="FftSize"/> samples per channel.
    /// </summary>
    SpectrumFrame? Analyse(ReadOnlySpan<float> samples, int sampleRateHz, int channels);
}
