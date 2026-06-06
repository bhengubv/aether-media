// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Output;

/// <summary>
/// Headless / test <see cref="IAudioOutput"/>. Pulls from the sample
/// provider on a background timer and discards every byte — useful in unit
/// tests + headless mesh-node deployments that never play audio locally.
/// </summary>
public sealed class NullAudioOutput : IAudioOutput
{
    private System.Threading.Timer? _timer;
    private Func<Memory<float>, int>? _provider;
    private float[]? _buffer;
    private bool _disposed;

    /// <inheritdoc/>
    public string Id => "null";

    /// <inheritdoc/>
    public string DisplayName => "Null output";

    /// <inheritdoc/>
    public float Volume { get; set; } = 1.0f;

    /// <inheritdoc/>
    public void Open(AudioFormat format, Func<Memory<float>, int> sampleProvider)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(sampleProvider);
        ObjectDisposedException.ThrowIf(_disposed, this);
        // 20 ms buffer at the given rate / channels.
        var samplesPerBuffer = (format.SampleRateHz * format.Channels) / 50;
        _buffer = new float[samplesPerBuffer];
        _provider = sampleProvider;
    }

    /// <inheritdoc/>
    public void Play()
    {
        if (_provider is null || _buffer is null) return;
        _timer ??= new System.Threading.Timer(_ =>
        {
            if (_provider is null || _buffer is null) return;
            _provider(_buffer.AsMemory());
        }, state: null, dueTime: 0, period: 20);
    }

    /// <inheritdoc/>
    public void Pause()
    {
        _timer?.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
    }

    /// <inheritdoc/>
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
        _provider = null;
        _buffer = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
