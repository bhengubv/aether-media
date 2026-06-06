// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Loudness;

/// <summary>
/// Pure-C# implementation of ITU-R BS.1770-4 / EBU R128 integrated loudness —
/// the same algorithm Spotify, YouTube, Apple Music, Tidal, every broadcaster,
/// and the libebur128 / pyloudnorm reference implementations use.
///
/// <para>
/// Algorithm (ITU-R BS.1770-4 Annex 1):
/// </para>
/// <list type="number">
/// <item>K-weight every channel via cascaded biquad pre-filter.</item>
/// <item>Slide a 400 ms window with 75 % overlap (100 ms hop). For each
/// window, compute the channel-weighted mean-square energy.</item>
/// <item>Convert each window to LUFS: <c>L = −0.691 + 10·log10(z)</c>.</item>
/// <item>Absolute gate at −70 LUFS.</item>
/// <item>Compute relative threshold: <c>−0.691 + 10·log10(meanZ_abs) − 10</c>.</item>
/// <item>Relative gate.</item>
/// <item>Integrated loudness = <c>−0.691 + 10·log10(meanZ_rel)</c>.</item>
/// </list>
/// </summary>
public sealed class Itu1770Analyzer : IAudioNormalizer
{
    private const double LoudnessOffset = -0.691;
    private const double AbsoluteGateLufs = -70.0;
    private const double RelativeGateOffsetDb = -10.0;
    private const double WindowDurationSec = 0.400;
    private const double WindowOverlap = 0.75;

    /// <inheritdoc/>
    public LoudnessMeasurement Measure(
        ReadOnlySpan<float> samples,
        int sampleRateHz,
        int channels)
    {
        if (samples.Length == 0)
            throw new ArgumentException("PCM samples are empty.", nameof(samples));
        if (channels < 1)
            throw new ArgumentOutOfRangeException(nameof(channels), "Channels must be ≥ 1.");
        if (samples.Length % channels != 0)
            throw new ArgumentException(
                $"PCM length {samples.Length} is not a multiple of channels {channels}.",
                nameof(samples));

        var frames = samples.Length / channels;
        var truePeak = ComputeTruePeakDbfs(samples, channels);

        var windowFrames = (int)Math.Round(WindowDurationSec * sampleRateHz);
        var hopFrames = (int)Math.Round(WindowDurationSec * (1.0 - WindowOverlap) * sampleRateHz);
        if (hopFrames < 1) hopFrames = 1;

        var kFilter = new KWeightingFilter(sampleRateHz, channels);
        var kWeighted = new double[samples.Length];
        for (var f = 0; f < frames; f++)
        {
            for (var c = 0; c < channels; c++)
            {
                var idx = f * channels + c;
                kWeighted[idx] = kFilter.ProcessSample(samples[idx], c);
            }
        }

        var windowZ = new List<double>();
        var lastWindowStart = frames - windowFrames;
        if (lastWindowStart < 0)
        {
            windowZ.Add(ChannelWeightedMeanSquare(kWeighted, 0, frames, channels));
        }
        else
        {
            for (var start = 0; start <= lastWindowStart; start += hopFrames)
                windowZ.Add(ChannelWeightedMeanSquare(kWeighted, start, windowFrames, channels));
        }

        var absGated = new List<double>(windowZ.Count);
        foreach (var z in windowZ)
        {
            if (z <= 0) continue;
            var lufs = LoudnessOffset + 10.0 * Math.Log10(z);
            if (lufs > AbsoluteGateLufs) absGated.Add(z);
        }

        double integrated;
        double loudnessRange;
        if (absGated.Count == 0)
        {
            integrated = double.NegativeInfinity;
            loudnessRange = 0.0;
        }
        else
        {
            var meanZAbs = MeanOf(absGated);
            var relativeThresholdLufs = LoudnessOffset + 10.0 * Math.Log10(meanZAbs) + RelativeGateOffsetDb;

            var relGated = new List<double>(absGated.Count);
            foreach (var z in absGated)
            {
                var lufs = LoudnessOffset + 10.0 * Math.Log10(z);
                if (lufs > relativeThresholdLufs) relGated.Add(z);
            }

            if (relGated.Count == 0)
            {
                integrated = LoudnessOffset + 10.0 * Math.Log10(meanZAbs);
                loudnessRange = 0.0;
            }
            else
            {
                integrated = LoudnessOffset + 10.0 * Math.Log10(MeanOf(relGated));
                loudnessRange = ComputeLoudnessRangeLu(relGated);
            }
        }

        return new LoudnessMeasurement(
            IntegratedLufs: integrated,
            TruePeakDbfs: truePeak,
            LoudnessRangeLu: loudnessRange,
            SampleRateHz: sampleRateHz,
            DurationSeconds: (double)frames / sampleRateHz,
            MeasuredAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    /// <inheritdoc/>
    public async Task<LoudnessMeasurement> MeasureAsync(
        Stream pcmStream,
        int sampleRateHz,
        int channels,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pcmStream);
        if (channels < 1)
            throw new ArgumentOutOfRangeException(nameof(channels), "Channels must be ≥ 1.");

        using var ms = new MemoryStream();
        await pcmStream.CopyToAsync(ms, ct).ConfigureAwait(false);
        var bytes = ms.ToArray();
        if (bytes.Length % sizeof(float) != 0)
            throw new ArgumentException(
                "PCM stream length is not a multiple of 4 bytes (float32).",
                nameof(pcmStream));

        var floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return Measure(floats, sampleRateHz, channels);
    }

    private static double ChannelWeightedMeanSquare(
        double[] kWeighted, int startFrame, int frameCount, int channels)
    {
        double z = 0.0;
        for (var c = 0; c < channels; c++)
        {
            double sumSq = 0.0;
            for (var f = 0; f < frameCount; f++)
            {
                var s = kWeighted[(startFrame + f) * channels + c];
                sumSq += s * s;
            }
            var meanSq = sumSq / frameCount;
            z += ChannelWeight(c, channels) * meanSq;
        }
        return z;
    }

    private static double ChannelWeight(int channelIndex, int totalChannels)
    {
        // 5.1 layout (L, R, C, LFE, Ls, Rs): LFE=0, surrounds=1.41, others=1.0
        if (totalChannels == 6)
        {
            return channelIndex switch
            {
                0 or 1 or 2 => 1.0,
                3 => 0.0,
                4 or 5 => 1.41,
                _ => 1.0,
            };
        }
        return 1.0;
    }

    private static double MeanOf(List<double> values)
    {
        if (values.Count == 0) return 0;
        double sum = 0;
        foreach (var v in values) sum += v;
        return sum / values.Count;
    }

    private static double ComputeLoudnessRangeLu(List<double> gatedZ)
    {
        if (gatedZ.Count < 2) return 0;
        var lufs = new double[gatedZ.Count];
        for (var i = 0; i < gatedZ.Count; i++)
            lufs[i] = LoudnessOffset + 10.0 * Math.Log10(gatedZ[i]);
        Array.Sort(lufs);
        var lo = lufs[(int)Math.Floor(0.10 * (lufs.Length - 1))];
        var hi = lufs[(int)Math.Floor(0.95 * (lufs.Length - 1))];
        return hi - lo;
    }

    private static double ComputeTruePeakDbfs(ReadOnlySpan<float> samples, int channels)
    {
        if (samples.Length == 0) return double.NegativeInfinity;
        float maxAbs = 0f;
        for (var c = 0; c < channels; c++)
        {
            float prev = samples[c];
            var lastFrame = (samples.Length / channels) - 1;
            for (var f = 1; f <= lastFrame; f++)
            {
                float curr = samples[f * channels + c];
                for (var s = 0; s < 4; s++)
                {
                    var t = s / 4f;
                    var v = prev * (1f - t) + curr * t;
                    var abs = Math.Abs(v);
                    if (abs > maxAbs) maxAbs = abs;
                }
                prev = curr;
            }
            var absLast = Math.Abs(prev);
            if (absLast > maxAbs) maxAbs = absLast;
        }
        if (maxAbs <= 0f) return double.NegativeInfinity;
        return 20.0 * Math.Log10(maxAbs);
    }
}
