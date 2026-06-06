// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Loudness;
using Xunit;

namespace AetherMedia.LocalLibrary.Tests.Audio.Loudness;

/// <summary>
/// ITU-R BS.1770-4 reference-signal tests. Tolerances of ±0.5–1.0 dB match
/// the precision of libebur128 / pyloudnorm reference implementations.
/// </summary>
public class Itu1770AnalyzerTests
{
    private const int SampleRate = 48000;
    private const int DurationSec = 10;
    private const int TotalFrames = SampleRate * DurationSec;

    [Fact]
    public void Measure_DigitalSilence_ReturnsBelowAbsoluteGate()
    {
        var samples = new float[TotalFrames * 2];
        var analyzer = new Itu1770Analyzer();
        var m = analyzer.Measure(samples, SampleRate, channels: 2);

        Assert.True(double.IsNegativeInfinity(m.IntegratedLufs),
            $"Silence should be below absolute gate; got {m.IntegratedLufs} LUFS");
        Assert.True(double.IsNegativeInfinity(m.TruePeakDbfs));
    }

    [Fact]
    public void Measure_ZeroDbfsSineAt1kHz_GivesAbout_Minus3Lufs()
    {
        // A 0 dBFS 1 kHz sine measures ~−3 LUFS per ITU-R BS.1770.
        var samples = GenerateSine(frequencyHz: 1000, amplitude: 1.0f, frames: TotalFrames);
        var analyzer = new Itu1770Analyzer();
        var m = analyzer.Measure(samples, SampleRate, channels: 1);

        Assert.InRange(m.IntegratedLufs, -3.7, -2.3);
    }

    [Fact]
    public void Measure_HalfAmplitudeSine_Is_Approximately_6dB_Quieter_ThanFullScale()
    {
        var loud  = GenerateSine(1000, 1.0f, TotalFrames);
        var quiet = GenerateSine(1000, 0.5f, TotalFrames);
        var analyzer = new Itu1770Analyzer();
        var mLoud  = analyzer.Measure(loud,  SampleRate, channels: 1);
        var mQuiet = analyzer.Measure(quiet, SampleRate, channels: 1);

        var delta = mLoud.IntegratedLufs - mQuiet.IntegratedLufs;
        Assert.InRange(delta, 5.7, 6.3);
    }

    [Fact]
    public void GainToTarget_QuietContent_ProducesPositiveGain()
    {
        var m = new LoudnessMeasurement(
            IntegratedLufs: -23.0, TruePeakDbfs: -10.0,
            LoudnessRangeLu: 5.0, SampleRateHz: 48000,
            DurationSeconds: 10, MeasuredAtMs: 0);

        var gain = m.GainToTarget(targetLufs: LoudnessTargets.Spotify);
        Assert.InRange(gain, 2.7, 2.9);
    }

    [Fact]
    public void GainToTarget_AlreadyAtTarget_ReturnsUnityGain()
    {
        var m = new LoudnessMeasurement(
            IntegratedLufs: -14.0, TruePeakDbfs: -3.0,
            LoudnessRangeLu: 5.0, SampleRateHz: 48000,
            DurationSeconds: 10, MeasuredAtMs: 0);

        var gain = m.GainToTarget(targetLufs: LoudnessTargets.Spotify);
        Assert.InRange(gain, 0.99, 1.01);
    }

    [Fact]
    public void GainToTarget_NeverExceedsTruePeakCeiling()
    {
        // Content at −1 dBFS true peak — any positive gain would clip.
        var m = new LoudnessMeasurement(
            IntegratedLufs: -27.0, TruePeakDbfs: -1.0,
            LoudnessRangeLu: 8.0, SampleRateHz: 48000,
            DurationSeconds: 10, MeasuredAtMs: 0);

        var gain = m.GainToTarget(
            targetLufs: LoudnessTargets.Spotify,
            truePeakCeilingDbfs: -1.0);

        Assert.InRange(gain, 0.99, 1.01);
    }

    [Fact]
    public async Task MeasureAsync_FromMemoryStream_MatchesSyncOverload()
    {
        var samples = GenerateSine(1000, 0.5f, TotalFrames);
        var bytes = new byte[samples.Length * sizeof(float)];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        using var stream = new MemoryStream(bytes);

        var analyzer = new Itu1770Analyzer();
        var fromStream = await analyzer.MeasureAsync(stream, SampleRate, channels: 1);
        var fromSpan   = analyzer.Measure(samples, SampleRate, channels: 1);

        Assert.Equal(fromSpan.IntegratedLufs, fromStream.IntegratedLufs, 6);
    }

    [Fact]
    public async Task InMemoryLoudnessStore_RoundTripsMeasurement()
    {
        var store = new InMemoryLoudnessStore();
        var m = new LoudnessMeasurement(
            IntegratedLufs: -14.0, TruePeakDbfs: -1.5,
            LoudnessRangeLu: 6.0, SampleRateHz: 48000,
            DurationSeconds: 180, MeasuredAtMs: 1700000000000);

        await store.SetAsync("abc123", m);
        var fetched = await store.GetAsync("abc123");

        Assert.NotNull(fetched);
        Assert.Equal(m.IntegratedLufs, fetched.IntegratedLufs);
        Assert.Equal(1, store.Count);

        var removed = await store.RemoveAsync("abc123");
        Assert.True(removed);
        Assert.Equal(0, store.Count);
    }

    private static float[] GenerateSine(double frequencyHz, float amplitude, int frames)
    {
        var samples = new float[frames];
        var omega = 2.0 * Math.PI * frequencyHz / SampleRate;
        for (var i = 0; i < frames; i++)
            samples[i] = amplitude * (float)Math.Sin(omega * i);
        return samples;
    }
}
