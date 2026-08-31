// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Effects;
using AetherMedia.LocalLibrary.Audio.Output;
using AetherMedia.LocalLibrary.Audio.Playback;
using AetherMedia.LocalLibrary.Audio.Plugins;

namespace AetherMedia.LocalLibrary.Tests.Audio.Playback;

/// <summary>
/// Stand-ins for the two things only hardware can provide — a sound card and a decoder — so the
/// engine and the queue can be exercised with neither. Shared by the engine and queue suites;
/// the alternative was two copies drifting apart.
/// </summary>
internal static class PlaybackTestDefaults
{
    public const int Rate = 44_100;
    public const int Channels = 2;
}

/// <summary>A track the fake factory knows how to open. Defaults keep call sites short.</summary>
internal sealed record FakeTrack(
    string Path,
    int Frames,
    int Chans = PlaybackTestDefaults.Channels,
    float Value = 0.1f);

/// <summary>Stands in for a sound card: holds the engine's callback and pulls on demand, so a
/// test controls exactly how much audio the "hardware" asks for and when.</summary>
internal sealed class FakeOutput : IAudioOutput
{
    private Func<Memory<float>, int>? _provider;

    public string Id => "fake";
    public string DisplayName => "Fake";
    public float Volume { get; set; } = 1f;

    public void Open(AudioFormat format, Func<Memory<float>, int> sampleProvider)
        => _provider = sampleProvider;

    public void Play() { }
    public void Pause() { }
    public void Stop() => _provider = null;
    public void Dispose() => Stop();

    /// <summary>One device callback. Returns samples written.</summary>
    public int Pull(int samples)
        => _provider is null ? 0 : _provider(new float[samples].AsMemory());

    /// <summary>One device callback, handing back what was actually written.</summary>
    public float[] PullInto(int samples)
    {
        if (_provider is null) return [];
        var buffer = new float[samples];
        var got = _provider(buffer.AsMemory());
        return buffer[..got];
    }

    /// <summary>Pull until the device is told end-of-stream. Returns the total.</summary>
    public int DrainAll()
    {
        var total = 0;
        while (true)
        {
            var got = Pull(512);
            if (got <= 0) return total;
            total += got;
        }
    }
}

internal sealed class FakeSourceFactory(params FakeTrack[] files) : IAudioSourceFactory
{
    public int ThrowAfter { get; set; } = -1;
    public bool Seekable { get; set; } = true;

    /// <summary>Paths the factory should refuse, standing in for a codec this device lacks.</summary>
    public HashSet<string> Undecodable { get; } = [];

    public IReadOnlyList<string> SupportedExtensions => ["mp3", "aac"];

    public Task<IInputPlugin?> OpenAsync(string filePath, CancellationToken ct = default)
    {
        if (Undecodable.Contains(filePath)) return Task.FromResult<IInputPlugin?>(null);

        var match = files.FirstOrDefault(f => f.Path == filePath);
        if (match is null) return Task.FromResult<IInputPlugin?>(null);

        return Task.FromResult<IInputPlugin?>(
            new FakeInput(match.Path, match.Frames, match.Chans, match.Value, ThrowAfter, Seekable));
    }
}

/// <summary>A decoder producing a known constant, so what comes out of the graph is checkable.</summary>
internal sealed class FakeInput(
    string path, int frames, int channels, float value, int throwAfter, bool seekable)
    : IInputPlugin
{
    private int _framesRead;

    public string Id => "fake-input";
    public string DisplayName => "Fake Input";
    public IReadOnlyList<string> SupportedExtensions => ["mp3", "aac"];
    public bool CanDecode(string filePath) => filePath == path;

    public Task<AudioFormat> OpenAsync(string filePath, CancellationToken ct = default)
        => Task.FromResult(new AudioFormat(PlaybackTestDefaults.Rate, channels));

    public long? DurationMs => frames * 1000L / PlaybackTestDefaults.Rate;

    public Task<long?> SeekAsync(long positionMs, CancellationToken ct = default)
    {
        if (!seekable) return Task.FromResult<long?>(null);
        _framesRead = (int)(positionMs * PlaybackTestDefaults.Rate / 1000);
        return Task.FromResult<long?>(positionMs);
    }

    public int ReadSamples(Memory<float> destination)
    {
        if (throwAfter >= 0 && _framesRead >= throwAfter)
            throw new InvalidDataException("simulated corrupt frame");

        var remainingFrames = frames - _framesRead;
        if (remainingFrames <= 0) return 0;

        var takeFrames = Math.Min(destination.Length / channels, remainingFrames);
        var samples = takeFrames * channels;

        destination.Span[..samples].Fill(value);
        _framesRead += takeFrames;
        return samples;
    }

    public void Close() { }
}

internal sealed class GainEffect(float gain) : IDspEffect
{
    public string Id => "test-gain";
    public string DisplayName => "Test Gain";
    public bool IsEnabled { get; set; } = true;

    public void Process(Span<float> samples, int sampleRateHz, int channels)
    {
        for (var i = 0; i < samples.Length; i++) samples[i] *= gain;
    }
}
