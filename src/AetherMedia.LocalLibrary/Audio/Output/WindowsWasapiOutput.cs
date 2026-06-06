// SPDX-License-Identifier: MIT

using System.Runtime.Versioning;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AetherMedia.LocalLibrary.Audio.Output;

/// <summary>
/// WASAPI output for Windows. Backed by NAudio.Wasapi. Honours WASAPI
/// shared and exclusive modes — exclusive avoids the system audio mixer and
/// gives bit-exact playback (Winamp's "WASAPI Exclusive" option).
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsWasapiOutput : IAudioOutput
{
    private WasapiOut? _out;
    private SampleProviderAdapter? _provider;
    private readonly bool _exclusive;
    private readonly int _latencyMs;

    /// <summary>Construct a shared-mode output with default 100 ms latency.</summary>
    public WindowsWasapiOutput() : this(exclusive: false, latencyMs: 100) { }

    /// <summary>
    /// Construct with explicit mode + latency. Exclusive mode requires the
    /// audio device to accept the requested format directly (no mixer).
    /// </summary>
    public WindowsWasapiOutput(bool exclusive, int latencyMs)
    {
        _exclusive = exclusive;
        _latencyMs = Math.Max(5, latencyMs);
    }

    /// <inheritdoc/>
    public string Id => _exclusive ? "windows-wasapi-exclusive" : "windows-wasapi-shared";

    /// <inheritdoc/>
    public string DisplayName => _exclusive ? "WASAPI (Exclusive)" : "WASAPI (Shared)";

    /// <inheritdoc/>
    public float Volume
    {
        get => _out?.Volume ?? 1.0f;
        set { if (_out is not null) _out.Volume = Math.Clamp(value, 0f, 1f); }
    }

    /// <inheritdoc/>
    public void Open(AudioFormat format, Func<Memory<float>, int> sampleProvider)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(sampleProvider);
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("WindowsWasapiOutput is Windows-only.");

        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRateHz, format.Channels);
        _provider = new SampleProviderAdapter(waveFormat, sampleProvider);

        var mode = _exclusive ? AudioClientShareMode.Exclusive : AudioClientShareMode.Shared;
        _out = new WasapiOut(mode, useEventSync: true, latency: _latencyMs);
        _out.Init(_provider);
    }

    /// <inheritdoc/>
    public void Play() => _out?.Play();

    /// <inheritdoc/>
    public void Pause() => _out?.Pause();

    /// <inheritdoc/>
    public void Stop()
    {
        _out?.Stop();
        _out?.Dispose();
        _out = null;
    }

    /// <inheritdoc/>
    public void Dispose() => Stop();

    /// <summary>Bridge from NAudio's pull-style <see cref="IWaveProvider"/> to our sample callback.</summary>
    private sealed class SampleProviderAdapter : IWaveProvider
    {
        private readonly Func<Memory<float>, int> _provider;
        public WaveFormat WaveFormat { get; }

        public SampleProviderAdapter(WaveFormat format, Func<Memory<float>, int> provider)
        {
            WaveFormat = format;
            _provider = provider;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            var floats = count / 4;
            var managed = new float[floats];
            var got = _provider(managed.AsMemory());
            Buffer.BlockCopy(managed, 0, buffer, offset, got * 4);
            return got * 4;
        }
    }
}
