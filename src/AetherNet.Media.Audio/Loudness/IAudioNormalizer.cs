// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Audio.Loudness;

/// <summary>
/// Loudness analysis + normalisation contract.
///
/// <para>
/// Implementations measure the integrated loudness of audio content per
/// ITU-R BS.1770-4 / EBU R128 (the same algorithm Spotify, YouTube, Apple Music,
/// Tidal, and every broadcaster use), and compute the linear gain required to
/// normalise that content to a target loudness without exceeding a true-peak
/// ceiling.
/// </para>
/// <para>
/// This is the "no track jumps in volume" feature: every modern player ships
/// it. AetherNet Media is no different — see <see cref="LoudnessTargets"/> for
/// the reference targets used by the major platforms.
/// </para>
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
    /// whole content as a single read-only span; for large files use the
    /// streaming overload.
    /// </param>
    /// <param name="sampleRateHz">Sample rate in Hz (e.g. 44100, 48000).</param>
    /// <param name="channels">Channel count (1 = mono, 2 = stereo, …).</param>
    /// <returns>
    /// Integrated loudness, true peak, and loudness range, per ITU-R BS.1770-4.
    /// </returns>
    LoudnessMeasurement Measure(
        ReadOnlySpan<float> samples,
        int sampleRateHz,
        int channels);

    /// <summary>
    /// Streaming version of <see cref="Measure"/> for content too large to
    /// hold in memory. Reads PCM frames from <paramref name="pcmStream"/>
    /// until exhaustion.
    /// </summary>
    /// <param name="pcmStream">
    /// Source of interleaved 32-bit float PCM bytes (little-endian, native
    /// .NET <see cref="float"/> layout).
    /// </param>
    /// <param name="sampleRateHz">Sample rate in Hz.</param>
    /// <param name="channels">Channel count.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<LoudnessMeasurement> MeasureAsync(
        Stream pcmStream,
        int sampleRateHz,
        int channels,
        CancellationToken ct = default);
}
