// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Loudness;

/// <summary>
/// Loudness analysis + normalisation contract. Implementations measure the
/// integrated loudness of audio content per ITU-R BS.1770-4 / EBU R128 — the
/// same algorithm Spotify, YouTube, Apple Music, Tidal, and every broadcaster
/// use — and produce a <see cref="LoudnessMeasurement"/> that can compute the
/// linear gain required to normalise that content to a target loudness
/// without exceeding a true-peak ceiling.
/// </summary>
public interface IAudioNormalizer
{
    /// <summary>
    /// Measure the integrated loudness of a stream of mono or interleaved
    /// multi-channel 32-bit float PCM samples.
    /// </summary>
    /// <param name="samples">
    /// Interleaved PCM samples in the range [−1.0, 1.0]. For multi-channel
    /// audio (e.g. stereo) channels are interleaved: L R L R … . Pass the
    /// whole content as a single read-only span; for large files use
    /// <see cref="MeasureAsync"/>.
    /// </param>
    /// <param name="sampleRateHz">Sample rate in Hz (e.g. 44100, 48000).</param>
    /// <param name="channels">Channel count (1 = mono, 2 = stereo, …).</param>
    LoudnessMeasurement Measure(
        ReadOnlySpan<float> samples,
        int sampleRateHz,
        int channels);

    /// <summary>
    /// Streaming version of <see cref="Measure"/> for content too large to
    /// hold in memory.
    /// </summary>
    Task<LoudnessMeasurement> MeasureAsync(
        Stream pcmStream,
        int sampleRateHz,
        int channels,
        CancellationToken ct = default);
}
