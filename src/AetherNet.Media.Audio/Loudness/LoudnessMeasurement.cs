// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Audio.Loudness;

/// <summary>
/// Integrated loudness measurement for a single piece of content, in the units
/// defined by ITU-R BS.1770-4 / EBU R128. All values use the "K-weighted, gated"
/// loudness algorithm — the same algorithm Spotify, YouTube, Apple Music, Tidal,
/// and every broadcaster use.
/// </summary>
/// <param name="IntegratedLufs">
/// Program loudness over the entire content, in LUFS (Loudness Units relative
/// to Full Scale). Quieter content has more negative values
/// (e.g. classical: −23 LUFS; rock: −9 LUFS; modern pop: −7 LUFS).
/// </param>
/// <param name="TruePeakDbfs">
/// Maximum inter-sample peak after oversampling, in dBFS. ITU-R BS.1770 defines
/// true-peak as the peak of the reconstructed analogue signal (typically
/// 1.5–2.5 dB above the digital sample peak for compressed music).
/// </param>
/// <param name="LoudnessRangeLu">
/// Statistical range between 10th and 95th percentile of short-term loudness
/// (in Loudness Units, LU). A wide dynamic-range track has high LRA; a
/// brick-wall mastered track has very low LRA.
/// </param>
/// <param name="SampleRateHz">Sample rate of the analysed audio (Hz).</param>
/// <param name="DurationSeconds">Duration of the analysed audio (seconds).</param>
/// <param name="MeasuredAtMs">
/// Unix epoch ms when this measurement was computed. Useful for cache
/// invalidation if the analyser version changes.
/// </param>
public sealed record LoudnessMeasurement(
    double IntegratedLufs,
    double TruePeakDbfs,
    double LoudnessRangeLu,
    int SampleRateHz,
    double DurationSeconds,
    long MeasuredAtMs)
{
    /// <summary>
    /// Compute the linear gain factor required to normalise this content to the
    /// given target loudness, with optional headroom to avoid true-peak clipping.
    /// </summary>
    /// <param name="targetLufs">
    /// Target loudness, e.g. <see cref="LoudnessTargets.Spotify"/> (−14 LUFS),
    /// <see cref="LoudnessTargets.AppleMusic"/> (−16 LUFS),
    /// <see cref="LoudnessTargets.EbuR128Broadcast"/> (−23 LUFS).
    /// </param>
    /// <param name="truePeakCeilingDbfs">
    /// Maximum permitted true-peak after gain. Default −1.0 dBFS leaves
    /// headroom for downstream codec / DAC reconstruction.
    /// </param>
    /// <returns>Linear gain (1.0 = unchanged, 2.0 = +6 dB, 0.5 = −6 dB).</returns>
    public double GainToTarget(double targetLufs, double truePeakCeilingDbfs = -1.0)
    {
        // Loudness-based gain: how much to scale to hit the target LUFS.
        var loudnessGainDb = targetLufs - IntegratedLufs;

        // True-peak headroom limit: cap so post-gain peak stays below ceiling.
        var peakHeadroomDb = truePeakCeilingDbfs - TruePeakDbfs;

        // Apply the more conservative of the two (never clip).
        var finalGainDb = Math.Min(loudnessGainDb, peakHeadroomDb);
        return Math.Pow(10.0, finalGainDb / 20.0);
    }
}
