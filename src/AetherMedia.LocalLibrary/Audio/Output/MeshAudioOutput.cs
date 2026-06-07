// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using AetherNet.Streaming;
using AetherNet.Streaming.Models;

namespace AetherMedia.LocalLibrary.Audio.Output;

/// <summary>
/// "Play on …" — Spotify-Connect-equivalent <see cref="IAudioOutput"/>.
/// Instead of emitting samples to a local audio device, this output starts
/// an <see cref="IStreamingService"/> session and pushes 20 ms PCM segments
/// over the mesh. Any peer that subscribes receives the segments and plays
/// them locally.
///
/// <para>
/// Segment layout on the wire: raw IEEE-754 little-endian 32-bit float PCM.
/// The format is announced via <see cref="StreamingService"/>'s
/// <see cref="StreamSession"/> metadata (codec = <c>"pcm-f32le"</c>) so the
/// subscriber knows the sample rate / channels without parsing each segment.
/// </para>
///
/// <para>
/// Backed by <c>formal/stream-abr</c> (the ABR controller can downsample
/// when the receiver reports a low-bandwidth path) and
/// <c>formal/watch-together-timed</c> (segments are sequence-numbered, so
/// multiple subscribers stay within a bounded latency window).
/// </para>
/// </summary>
public sealed class MeshAudioOutput : IAudioOutput
{
    private readonly IStreamingService _streaming;
    private readonly string _displayName;
    private readonly int _segmentMs;

    private AudioFormat? _format;
    private Func<Memory<float>, int>? _provider;
    private float[]? _scratch;
    private StreamSession? _session;
    private CancellationTokenSource? _cts;
    private Task? _publishLoop;
    private uint _sequence;
    private bool _paused;
    private float _volume = 1.0f;

    public MeshAudioOutput(IStreamingService streaming, string displayName = "Mesh", int segmentMs = 20)
    {
        _streaming = streaming ?? throw new ArgumentNullException(nameof(streaming));
        _displayName = displayName;
        _segmentMs = Math.Clamp(segmentMs, 5, 200);
    }

    /// <inheritdoc/>
    public string Id => "mesh-audio";

    /// <inheritdoc/>
    public string DisplayName => $"Mesh output ({_displayName})";

    /// <inheritdoc/>
    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>The current stream session, or null when not playing.</summary>
    public StreamSession? Session => _session;

    /// <inheritdoc/>
    public void Open(AudioFormat format, Func<Memory<float>, int> sampleProvider)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(sampleProvider);
        _format = format;
        _provider = sampleProvider;
        var samplesPerSegment = (format.SampleRateHz * format.Channels * _segmentMs) / 1000;
        _scratch = new float[Math.Max(1, samplesPerSegment)];
    }

    /// <inheritdoc/>
    public void Play()
    {
        if (_provider is null || _format is null || _scratch is null)
            throw new InvalidOperationException("Open must be called before Play.");
        if (_session is not null) { _paused = false; return; }

        // Start the stream synchronously so the session id is available before the loop.
        _session = _streaming.StartStreamAsync(
            title: _displayName,
            contentType: "audio/pcm",
            codec: $"pcm-f32le-{_format.SampleRateHz}-{_format.Channels}",
            segmentDurationMs: _segmentMs,
            profile: StreamProfile.ProfileA).GetAwaiter().GetResult();

        _cts = new CancellationTokenSource();
        _paused = false;
        _publishLoop = Task.Run(() => PublishLoopAsync(_cts.Token));
    }

    /// <inheritdoc/>
    public void Pause() => _paused = true;

    /// <inheritdoc/>
    public void Stop()
    {
        try { _cts?.Cancel(); } catch (ObjectDisposedException) { }
        try { _publishLoop?.Wait(TimeSpan.FromSeconds(2)); } catch (AggregateException) { }
        if (_session is not null)
        {
            try { _streaming.EndStreamAsync(_session.Id).GetAwaiter().GetResult(); }
            catch (Exception) { /* best-effort */ }
            _session = null;
        }
        _cts?.Dispose();
        _cts = null;
        _publishLoop = null;
        _sequence = 0;
    }

    /// <inheritdoc/>
    public void Dispose() => Stop();

    private async Task PublishLoopAsync(CancellationToken ct)
    {
        var period = TimeSpan.FromMilliseconds(_segmentMs);
        var nextTick = DateTime.UtcNow;
        while (!ct.IsCancellationRequested && _session is not null && _provider is not null && _scratch is not null)
        {
            if (_paused)
            {
                try { await Task.Delay(period, ct).ConfigureAwait(false); } catch (OperationCanceledException) { break; }
                continue;
            }

            var produced = _provider(_scratch.AsMemory());
            if (produced > 0)
            {
                // Apply volume in-place, then serialise as raw bytes.
                if (Math.Abs(_volume - 1.0f) > 1e-4f)
                {
                    var span = _scratch.AsSpan(0, produced);
                    for (var i = 0; i < span.Length; i++) span[i] *= _volume;
                }
                var bytes = MemoryMarshal.AsBytes(_scratch.AsSpan(0, produced)).ToArray();
                try
                {
                    await _streaming.PublishSegmentAsync(
                        streamId: _session.Id,
                        encoded: bytes,
                        sequence: _sequence++,
                        isKeyframe: _sequence == 1,
                        cancellationToken: ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (InvalidOperationException) { break; /* session closed */ }
            }

            nextTick += period;
            var delay = nextTick - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                try { await Task.Delay(delay, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }
    }
}
