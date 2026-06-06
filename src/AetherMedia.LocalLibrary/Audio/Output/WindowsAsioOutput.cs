// SPDX-License-Identifier: MIT

using System.Runtime.Versioning;
using NAudio.Wave;

namespace AetherMedia.LocalLibrary.Audio.Output;

/// <summary>
/// ASIO output for Windows. Backed by NAudio.Asio against any installed
/// Steinberg ASIO driver. Driver enumeration via
/// <see cref="EnumerateDriverNames"/>; pass the chosen driver name to the
/// constructor.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsAsioOutput : IAudioOutput
{
    private readonly string? _driverName;
    private AsioOut? _out;
    private IWaveProvider? _provider;

    /// <summary>Use the first installed ASIO driver.</summary>
    public WindowsAsioOutput() : this(driverName: null) { }

    /// <summary>Use the driver with the given name (case-insensitive).</summary>
    public WindowsAsioOutput(string? driverName)
    {
        _driverName = driverName;
    }

    /// <summary>Names of every installed ASIO driver, in the order the registry lists them.</summary>
    public static IReadOnlyList<string> EnumerateDriverNames()
    {
        if (!OperatingSystem.IsWindows()) return Array.Empty<string>();
        try { return AsioOut.GetDriverNames(); }
        catch (Exception) { return Array.Empty<string>(); }
    }

    /// <inheritdoc/>
    public string Id => "windows-asio";

    /// <inheritdoc/>
    public string DisplayName => $"ASIO ({_driverName ?? "default"})";

    /// <inheritdoc/>
    public float Volume { get; set; } = 1.0f;

    /// <inheritdoc/>
    public void Open(AudioFormat format, Func<Memory<float>, int> sampleProvider)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(sampleProvider);
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("WindowsAsioOutput is Windows-only.");

        var driver = _driverName ?? EnumerateDriverNames().FirstOrDefault()
                   ?? throw new InvalidOperationException("No ASIO driver installed.");

        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRateHz, format.Channels);
        _provider = new SampleProviderAdapter(waveFormat, sampleProvider);
        _out = new AsioOut(driver);
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
