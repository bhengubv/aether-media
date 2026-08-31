// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Crossfade;
using AetherMedia.LocalLibrary.Audio.Effects;
using AetherMedia.LocalLibrary.Audio.Output;
using AetherMedia.LocalLibrary.Audio.Plugins;
using AetherMedia.LocalLibrary.Audio.Visualization;
using Microsoft.Extensions.Logging;

namespace AetherMedia.LocalLibrary.Audio.Playback;

/// <summary>
/// The piece that was missing: what actually plays music.
///
/// <para>Everything around it already existed — decoder contract, output contract, effects
/// chain, crossfade curve, spectrum analyser — but nothing composed them, so the whole
/// audio stack was unreachable. This is the composition.</para>
///
/// <para><b>The graph.</b> Output devices PULL. The device asks for N samples on its own
/// driver thread; this engine answers from the decoder, runs the effects chain over the
/// buffer, applies volume, and hands it back. Nothing is pushed and nothing is queued
/// behind the device, so latency is whatever the device buffer is and no more.</para>
///
/// <para><b>The audio thread rule.</b> <see cref="Fill"/> runs on the driver thread and must
/// never allocate, never block on I/O, and never run caller code. Scratch buffers are
/// allocated once at open; events are handed to the thread pool. Breaking any of these is
/// heard as a click or a dropout, not seen as an exception.</para>
/// </summary>
public sealed class AudioPlayerEngine : IAsyncDisposable
{
    private readonly IAudioSourceFactory _sources;
    private readonly IAudioOutput _output;
    private readonly IDspChain _dsp;
    private readonly ICrossfade _crossfade;
    private readonly IVisualizationFeed? _visualization;
    private readonly ILogger<AudioPlayerEngine> _log;

    /// <summary>Guards every field below. Held only for the length of a buffer — never across I/O.</summary>
    private readonly Lock _gate = new();

    private IInputPlugin? _current;
    private string? _currentPath;
    private IInputPlugin? _next;
    private string? _nextPath;
    private AudioFormat? _nextFormat;
    private AudioFormat? _format;
    private PlaybackState _state = PlaybackState.Idle;
    private long _framesOut;
    private float _volume = 1.0f;

    // Scratch. Allocated at open, reused for the life of the device — see the audio thread rule.
    private float[] _mixBuffer = [];
    private float[] _gainBuffer = [];
    private float[] _visSnapshot = [];
    private int _visFilled;

    /// <summary>Construct the engine over its collaborators. None of them is optional except
    /// the visualisation feed, which a head without a screen legitimately does not have.</summary>
    public AudioPlayerEngine(
        IAudioSourceFactory sources,
        IAudioOutput output,
        IDspChain dsp,
        ICrossfade crossfade,
        ILogger<AudioPlayerEngine> log,
        IVisualizationFeed? visualization = null)
    {
        _sources = sources ?? throw new ArgumentNullException(nameof(sources));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _dsp = dsp ?? throw new ArgumentNullException(nameof(dsp));
        _crossfade = crossfade ?? throw new ArgumentNullException(nameof(crossfade));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _visualization = visualization;
    }

    /// <summary>The effects chain applied to every buffer. Add the equaliser and any other
    /// effect here — the equaliser is itself an <see cref="IDspEffect"/>, so it belongs in
    /// the chain rather than beside it.</summary>
    public IDspChain Effects => _dsp;

    /// <summary>Crossfade behaviour between tracks.</summary>
    public ICrossfade Crossfade => _crossfade;

    /// <summary>Current state.</summary>
    public PlaybackState State { get { lock (_gate) return _state; } }

    /// <summary>Path of the track currently open, or null.</summary>
    public string? CurrentSource { get { lock (_gate) return _currentPath; } }

    /// <summary>Elapsed position in milliseconds, counted from frames actually delivered to
    /// the device — so it reflects what was heard, not what was decoded.</summary>
    public long PositionMs
    {
        get
        {
            lock (_gate)
            {
                if (_format is null || _format.SampleRateHz <= 0) return 0;
                return _framesOut * 1000L / _format.SampleRateHz;
            }
        }
    }

    /// <summary>Total length, or null when the decoder cannot say (live streams cannot).
    /// A UI must render a null total as "no total", never as zero.</summary>
    public long? DurationMs { get { lock (_gate) return _current?.DurationMs; } }

    /// <summary>Output level, 0..1.</summary>
    public float Volume
    {
        get { lock (_gate) return _volume; }
        set { lock (_gate) _volume = Math.Clamp(value, 0f, 1f); }
    }

    /// <summary>Raised when the state changes. Fired on the thread pool, never on the audio thread.</summary>
    public event EventHandler<PlaybackState>? StateChanged;

    /// <summary>Raised when a track ends, for any reason. Thread pool, never the audio thread.</summary>
    public event EventHandler<TrackEndedEventArgs>? TrackEnded;

    /// <summary>
    /// Open a file and start playing it. Replaces whatever was playing. Returns false when
    /// nothing on this device can decode the file — the caller should say so rather than
    /// leave a dead-looking button.
    /// </summary>
    public async Task<bool> PlayAsync(string filePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        // Decoder open touches the filesystem, so it happens BEFORE the lock is taken.
        var source = await _sources.OpenAsync(filePath, ct).ConfigureAwait(false);
        if (source is null)
        {
            _log.LogWarning("No decoder on this device for {Path}", filePath);
            return false;
        }

        var format = await SafeOpenAsync(source, filePath, ct).ConfigureAwait(false);
        if (format is null) return false;

        IInputPlugin? retired;
        string? retiredPath;
        var reopenDevice = false;

        lock (_gate)
        {
            retired = _current;
            retiredPath = _currentPath;

            // The device is opened for one PCM format. A track at a different rate or channel
            // count needs the device reopened — Winamp has the same constraint, and pretending
            // otherwise resamples silently or plays at the wrong speed.
            reopenDevice = _format is null
                || _format.SampleRateHz != format.SampleRateHz
                || _format.Channels != format.Channels
                || _format.IsFloat != format.IsFloat;

            _current = source;
            _currentPath = filePath;
            _format = format;
            _framesOut = 0;
            DiscardPendingNext();
        }

        CloseQuietly(retired);
        if (retiredPath is not null)
            Raise(TrackEnded, new TrackEndedEventArgs(retiredPath, TrackEndReason.Replaced));

        if (reopenDevice)
        {
            _output.Stop();
            AllocateScratch(format);
            _output.Open(format, Fill);
        }

        _output.Volume = Volume;
        _output.Play();
        SetState(PlaybackState.Playing);
        _log.LogInformation("Playing {Path} at {Rate} Hz, {Channels}ch", filePath, format.SampleRateHz, format.Channels);
        return true;
    }

    /// <summary>
    /// Pre-open the track that follows, so the handover is seamless. A queue calls this as
    /// soon as it knows what is next. Without it, gapless is impossible and a crossfade has
    /// nothing to fade into — the engine will not open a file on the audio thread.
    /// </summary>
    public async Task<bool> SetNextAsync(string? filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            lock (_gate) DiscardPendingNext();
            return true;
        }

        var source = await _sources.OpenAsync(filePath, ct).ConfigureAwait(false);
        if (source is null) return false;

        var format = await SafeOpenAsync(source, filePath, ct).ConfigureAwait(false);
        if (format is null) return false;

        lock (_gate)
        {
            // Only a format-identical follower can hand over without reopening the device.
            // A mismatched one is still worth holding: EOS will take the slow path and
            // reopen, which costs a gap but plays the right track at the right speed.
            DiscardPendingNext();
            _next = source;
            _nextPath = filePath;
            _nextFormat = format;
        }
        return true;
    }

    /// <summary>Pause. Position is preserved. Safe when nothing is open.</summary>
    public void Pause()
    {
        lock (_gate) { if (_state != PlaybackState.Playing) return; }
        _output.Pause();
        SetState(PlaybackState.Paused);
    }

    /// <summary>Resume. Safe when nothing is open.</summary>
    public void Resume()
    {
        lock (_gate) { if (_state != PlaybackState.Paused) return; }
        _output.Play();
        SetState(PlaybackState.Playing);
    }

    /// <summary>
    /// Jump to an absolute position. Returns the position actually reached — compressed
    /// formats land on a frame boundary, not the exact millisecond. Returns null when the
    /// source cannot seek, which is a fact about the source, not an error.
    /// </summary>
    public async Task<long?> SeekAsync(long positionMs, CancellationToken ct = default)
    {
        IInputPlugin? source;
        lock (_gate) source = _current;
        if (source is null) return null;

        var landed = await source.SeekAsync(Math.Max(0, positionMs), ct).ConfigureAwait(false);
        if (landed is null) return null;

        lock (_gate)
        {
            if (!ReferenceEquals(source, _current)) return null;   // track changed under us
            _framesOut = _format is null ? 0 : landed.Value * _format.SampleRateHz / 1000L;
        }
        return landed;
    }

    /// <summary>Stop, close the device, and release the sources.</summary>
    public void Stop()
    {
        IInputPlugin? cur, nxt;
        string? curPath;
        lock (_gate)
        {
            cur = _current; curPath = _currentPath; nxt = _next;
            _current = null; _currentPath = null; _next = null; _nextPath = null;
            _format = null; _framesOut = 0; _visFilled = 0;
        }

        _output.Stop();
        CloseQuietly(cur);
        CloseQuietly(nxt);
        if (curPath is not null)
            Raise(TrackEnded, new TrackEndedEventArgs(curPath, TrackEndReason.Replaced));
        SetState(PlaybackState.Idle);
    }

    /// <summary>
    /// Latest spectrum + waveform for the visualiser, or null when nothing is playing or no
    /// analyser was supplied.
    ///
    /// <para>The transform runs on the CALLER's thread, not the audio thread — the audio
    /// thread only ever memcpy's the buffer it just played. An FFT inside the driver
    /// callback is how visualisers cause dropouts.</para>
    /// </summary>
    public SpectrumFrame? CaptureSpectrum()
    {
        if (_visualization is null) return null;

        float[] copy;
        int count, rate, channels;
        lock (_gate)
        {
            if (_format is null || _visFilled == 0) return null;
            count = _visFilled;
            copy = new float[count];
            Array.Copy(_visSnapshot, copy, count);
            rate = _format.SampleRateHz;
            channels = _format.Channels;
        }
        return _visualization.Analyse(copy.AsSpan(0, count), rate, channels);
    }

    // ── The audio thread. Everything below runs on the driver's callback. ──────────────

    /// <summary>
    /// Fill one device buffer. Returns the number of samples written; 0 means end-of-stream
    /// and stops the device.
    /// </summary>
    private int Fill(Memory<float> destination)
    {
        var dest = destination.Span;
        int written;
        int rate, channels;
        float volume;
        string? endedPath = null;
        var endedReason = TrackEndReason.Completed;

        lock (_gate)
        {
            if (_current is null || _format is null) return 0;

            rate = _format.SampleRateHz;
            channels = _format.Channels;
            volume = _volume;

            written = ReadWithHandover(destination, out endedPath, out endedReason);
            if (written <= 0)
            {
                if (endedPath is not null) RaiseTrackEnded(endedPath, endedReason);
                return 0;
            }

            ApplyCrossfadeIfDue(dest[..written], rate, channels);
            _framesOut += written / Math.Max(1, channels);
            SnapshotForVisualiser(dest[..written]);
        }

        // Effects run outside the lock: the chain is caller-mutable and a long effect must
        // not hold up a state read. The buffer is ours alone at this point.
        _dsp.Process(dest[..written], rate, channels);

        if (volume < 0.999f)
        {
            for (var i = 0; i < written; i++) dest[i] *= volume;
        }

        if (endedPath is not null) RaiseTrackEnded(endedPath, endedReason);
        return written;
    }

    /// <summary>
    /// Pull from the current decoder, and on end-of-stream hand over to the pre-opened
    /// follower and keep filling the same buffer — that continuity IS gapless playback.
    /// Caller holds the lock.
    /// </summary>
    private int ReadWithHandover(Memory<float> destination, out string? endedPath, out TrackEndReason reason)
    {
        endedPath = null;
        reason = TrackEndReason.Completed;

        var total = 0;
        var dest = destination;

        while (total < destination.Length)
        {
            int got;
            try
            {
                got = _current!.ReadSamples(dest);
            }
            catch (Exception ex)
            {
                // A decoder that throws mid-file is a broken source, not a broken player.
                // Report it and stop this track rather than tearing down the device.
                _log.LogWarning(ex, "Decoder failed on {Path}", _currentPath);
                endedPath = _currentPath;
                reason = TrackEndReason.Failed;
                RetireCurrent();
                return total;
            }

            if (got > 0)
            {
                total += got;
                if (total >= destination.Length) break;
                dest = destination[total..];
                continue;
            }

            // End of this track.
            endedPath ??= _currentPath;
            reason = TrackEndReason.Completed;

            if (_next is null || !NextCanHandOverGaplessly())
            {
                RetireCurrent();
                return total;
            }

            PromoteNext();
        }

        return total;
    }

    /// <summary>
    /// Mix the outgoing and incoming tracks across the fade window. Requires a known
    /// duration — a source that cannot say how long it is cannot be faded out on time, so
    /// it hands over gaplessly instead of pretending. Caller holds the lock.
    /// </summary>
    private void ApplyCrossfadeIfDue(Span<float> buffer, int rate, int channels)
    {
        if (_crossfade.Mode != CrossfadeMode.Crossfade) return;
        if (_next is null) return;

        var total = _current?.DurationMs;
        if (total is null or <= 0) return;

        var positionMs = _framesOut * 1000L / Math.Max(1, rate);
        var remaining = total.Value - positionMs;
        if (remaining > _crossfade.FadeDurationMs) return;

        var frames = buffer.Length / Math.Max(1, channels);
        if (_gainBuffer.Length < frames || _mixBuffer.Length < buffer.Length) return;

        var outGains = _gainBuffer.AsSpan(0, frames);
        _crossfade.ComputeGainRamp((int)positionMs, frames, rate, fadingOut: true, outGains);

        var incoming = _mixBuffer.AsMemory(0, buffer.Length);
        int got;
        try { got = _next.ReadSamples(incoming); }
        catch { return; }   // incoming track misbehaving: keep the outgoing one clean
        if (got <= 0) return;

        var incomingSpan = _mixBuffer.AsSpan(0, got);
        for (var f = 0; f < frames; f++)
        {
            var gOut = outGains[f];
            var gIn = 1.0f - gOut;
            for (var c = 0; c < channels; c++)
            {
                var i = f * channels + c;
                if (i >= buffer.Length) break;
                var incomingSample = i < got ? incomingSpan[i] : 0f;
                buffer[i] = buffer[i] * gOut + incomingSample * gIn;
            }
        }
    }

    /// <summary>Copy the buffer we just played for the visualiser. A memcpy, nothing more —
    /// the transform happens on whoever calls <see cref="CaptureSpectrum"/>.
    /// Caller holds the lock.</summary>
    private void SnapshotForVisualiser(ReadOnlySpan<float> buffer)
    {
        if (_visSnapshot.Length == 0) return;
        var n = Math.Min(buffer.Length, _visSnapshot.Length);
        buffer[..n].CopyTo(_visSnapshot);
        _visFilled = n;
    }

    // ── Plumbing ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Can the follower take over without reopening the device? Only if it decodes to the
    /// same PCM shape — the device was opened for one format and cannot be reconfigured from
    /// the audio thread. A mismatch is not a failure: it costs a gap, because the queue has
    /// to restart the device on the calling thread. Caller holds the lock.
    /// </summary>
    private bool NextCanHandOverGaplessly()
        => _nextFormat is not null
           && _format is not null
           && _nextFormat.SampleRateHz == _format.SampleRateHz
           && _nextFormat.Channels == _format.Channels
           && _nextFormat.IsFloat == _format.IsFloat;

    private void PromoteNext()
    {
        CloseQuietly(_current);
        _current = _next;
        _currentPath = _nextPath;
        _next = null;
        _nextPath = null;
        _nextFormat = null;
        _framesOut = 0;
    }

    private void RetireCurrent()
    {
        CloseQuietly(_current);
        _current = null;
        _currentPath = null;
    }

    private void DiscardPendingNext()
    {
        CloseQuietly(_next);
        _next = null;
        _nextPath = null;
    }

    private void AllocateScratch(AudioFormat format)
    {
        // One second of headroom is far more than any device buffer, and it is allocated
        // once per device open rather than per callback.
        var samples = Math.Max(4096, format.SampleRateHz * format.Channels);
        _mixBuffer = new float[samples];
        _gainBuffer = new float[Math.Max(1024, format.SampleRateHz)];
        _visSnapshot = new float[Math.Min(samples, 8192)];
        _visFilled = 0;
    }

    private async Task<AudioFormat?> SafeOpenAsync(IInputPlugin source, string path, CancellationToken ct)
    {
        try
        {
            return await source.OpenAsync(path, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not open {Path}", path);
            CloseQuietly(source);
            Raise(TrackEnded, new TrackEndedEventArgs(path, TrackEndReason.Failed, ex));
            return null;
        }
    }

    private void CloseQuietly(IInputPlugin? source)
    {
        if (source is null) return;
        try { source.Close(); }
        catch (Exception ex) { _log.LogDebug(ex, "Decoder close threw; ignoring"); }
    }

    private void SetState(PlaybackState state)
    {
        lock (_gate)
        {
            if (_state == state) return;
            _state = state;
        }
        Raise(StateChanged, state);
    }

    /// <summary>Fire an event without letting caller code run on the audio thread.</summary>
    private void Raise<T>(EventHandler<T>? handler, T args)
    {
        if (handler is null) return;
        ThreadPool.UnsafeQueueUserWorkItem(_ =>
        {
            try { handler(this, args); }
            catch (Exception ex) { _log.LogWarning(ex, "Playback event handler threw"); }
        }, null);
    }

    private void RaiseTrackEnded(string path, TrackEndReason reason)
        => Raise(TrackEnded, new TrackEndedEventArgs(path, reason));

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Stop();
        _output.Dispose();
        return ValueTask.CompletedTask;
    }
}
