// SPDX-License-Identifier: MIT

using Microsoft.Extensions.Logging;

namespace AetherMedia.LocalLibrary.Audio.Playback;

/// <summary>What happens when a track finishes.</summary>
public enum RepeatMode
{
    /// <summary>Stop at the end of the list.</summary>
    Off = 0,

    /// <summary>Replay the current track forever.</summary>
    One = 1,

    /// <summary>Wrap around to the start.</summary>
    All = 2
}

/// <summary>One entry in the queue. Metadata is carried so a notification or lock screen can
/// name the track without re-reading tags off disk on the audio thread.</summary>
public sealed record QueueItem(
    string Path,
    string? Title = null,
    string? Artist = null,
    string? Album = null,
    long? DurationMs = null,
    string? ArtworkPath = null)
{
    /// <summary>What to show when the file has no tags — the filename, not an empty string.</summary>
    public string DisplayTitle => string.IsNullOrWhiteSpace(Title)
        ? System.IO.Path.GetFileNameWithoutExtension(Path)
        : Title;
}

/// <summary>
/// The list, and the rules for moving through it: next, previous, shuffle, repeat.
///
/// <para>Kept separate from <see cref="AudioPlayerEngine"/> on purpose. The engine knows how to
/// play one thing and hand over to another; it has no opinion about order. Everything order-shaped
/// lives here, which is also what makes gapless work — the queue can tell the engine what is coming
/// next long before the current track ends.</para>
///
/// <para><b>Handover is why this is subtler than it looks.</b> When the engine reaches the end of a
/// track it may have ALREADY switched to the pre-armed follower (that continuity is gapless
/// playback). So on a completion this queue asks what the engine is actually playing before
/// deciding: if the engine moved on by itself, only the index needs to catch up; if it retired,
/// the next track has to be started properly. Getting that wrong either restarts the track that
/// just began or leaves the queue silently stalled.</para>
/// </summary>
public sealed class PlaybackQueue : IAsyncDisposable
{
    private readonly AudioPlayerEngine _engine;
    private readonly ILogger<PlaybackQueue> _log;
    private readonly Lock _gate = new();

    private readonly List<QueueItem> _items = [];
    /// <summary>Positions into <see cref="_items"/>, in play order. Shuffling permutes this and
    /// leaves the underlying list alone, so turning shuffle off restores the real order.</summary>
    private readonly List<int> _order = [];

    private int _cursor = -1;          // index into _order, not into _items
    private bool _shuffle;
    private RepeatMode _repeat = RepeatMode.Off;

    /// <summary>Construct over an engine and start listening for track completions.</summary>
    public PlaybackQueue(AudioPlayerEngine engine, ILogger<PlaybackQueue> log)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _engine.TrackEnded += OnTrackEnded;
    }

    /// <summary>The queue in its real order, regardless of shuffle.</summary>
    public IReadOnlyList<QueueItem> Items { get { lock (_gate) return [.. _items]; } }

    /// <summary>What is playing, or null when the queue is empty or finished.</summary>
    public QueueItem? Current
    {
        get
        {
            lock (_gate) return CurrentLocked();
        }
    }

    /// <summary>Position in the play order, or -1.</summary>
    public int CursorIndex { get { lock (_gate) return _cursor; } }

    /// <summary>Repeat behaviour at the end of a track.</summary>
    public RepeatMode Repeat
    {
        get { lock (_gate) return _repeat; }
        set { lock (_gate) _repeat = value; _ = ArmNextAsync(); }
    }

    /// <summary>
    /// Play in a random order. Toggling this never loses the real order — the underlying list is
    /// untouched and only the traversal is permuted, so switching back resumes the album.
    /// </summary>
    public bool Shuffle
    {
        get { lock (_gate) return _shuffle; }
        set
        {
            lock (_gate)
            {
                if (_shuffle == value) return;
                _shuffle = value;
                RebuildOrderLocked();
            }
            _ = ArmNextAsync();
        }
    }

    /// <summary>Raised whenever the current track changes, including on automatic advance.</summary>
    public event EventHandler<QueueItem?>? CurrentChanged;

    /// <summary>
    /// Replace the queue and start playing at <paramref name="startIndex"/>. Returns false when
    /// nothing could be played — an empty list, or a first track no decoder can open.
    /// </summary>
    public async Task<bool> PlayAsync(IEnumerable<QueueItem> items, int startIndex = 0, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        lock (_gate)
        {
            _items.Clear();
            _items.AddRange(items);
            RebuildOrderLocked();
            _cursor = _items.Count == 0 ? -1 : _order.IndexOf(Math.Clamp(startIndex, 0, _items.Count - 1));
        }

        return await StartCurrentAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Append without disturbing what is playing.</summary>
    public void Enqueue(QueueItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        lock (_gate)
        {
            _items.Add(item);
            _order.Add(_items.Count - 1);   // appended at the end whether shuffled or not
        }
        _ = ArmNextAsync();
    }

    /// <summary>Skip forward. Honours shuffle and repeat; returns false at a hard end.</summary>
    public async Task<bool> NextAsync(CancellationToken ct = default)
    {
        lock (_gate)
        {
            // A manual skip ignores Repeat.One — the listener asked to move, not to hear it again.
            if (!AdvanceCursorLocked(manual: true)) return false;
        }
        return await StartCurrentAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Skip back, or restart the current track when more than three seconds in — the convention
    /// every music player uses, and the one listeners expect.
    /// </summary>
    public async Task<bool> PreviousAsync(CancellationToken ct = default)
    {
        if (_engine.PositionMs > 3_000)
        {
            var landed = await _engine.SeekAsync(0, ct).ConfigureAwait(false);
            if (landed is not null) return true;
            // An unseekable source cannot restart, so fall through and treat this as a real skip.
        }

        lock (_gate)
        {
            if (_cursor <= 0)
            {
                if (_repeat != RepeatMode.All || _order.Count == 0) return false;
                _cursor = _order.Count - 1;
            }
            else
            {
                _cursor--;
            }
        }
        return await StartCurrentAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Stop and clear.</summary>
    public void Clear()
    {
        _engine.Stop();
        lock (_gate)
        {
            _items.Clear();
            _order.Clear();
            _cursor = -1;
        }
        RaiseCurrentChanged(null);
    }

    // ── Internals ─────────────────────────────────────────────────────────────────────

    private async Task<bool> StartCurrentAsync(CancellationToken ct)
    {
        QueueItem? item;
        lock (_gate) item = CurrentLocked();
        if (item is null) return false;

        var ok = await _engine.PlayAsync(item.Path, ct).ConfigureAwait(false);
        if (!ok)
        {
            _log.LogWarning("Nothing could play {Path}; skipping it", item.Path);
            // A file the device cannot decode must not stall the queue. Step over it — but only
            // once per call, so a queue of entirely unplayable files ends rather than spinning.
            lock (_gate)
            {
                if (!AdvanceCursorLocked(manual: true)) return false;
            }
            var next = await StartCurrentAsync(ct).ConfigureAwait(false);
            return next;
        }

        RaiseCurrentChanged(item);
        await ArmNextAsync(ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Tell the engine what follows, so the handover has something to hand over TO. Without this
    /// there is no gapless and no crossfade — the engine will not open a file on the audio thread.
    /// </summary>
    private async Task ArmNextAsync(CancellationToken ct = default)
    {
        string? nextPath;
        lock (_gate) nextPath = PeekNextLocked()?.Path;

        try
        {
            await _engine.SetNextAsync(nextPath, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Failing to pre-open the follower costs gaplessness, not playback.
            _log.LogDebug(ex, "Could not pre-arm {Path}", nextPath);
        }
    }

    private void OnTrackEnded(object? sender, TrackEndedEventArgs e)
    {
        // Replaced means something else took over deliberately — a new PlayAsync, or Stop. The
        // queue must not "advance" on its own action or it would skip a track every time.
        if (e.Reason == TrackEndReason.Replaced) return;

        _ = HandleCompletionAsync(e);
    }

    private async Task HandleCompletionAsync(TrackEndedEventArgs e)
    {
        try
        {
            if (e.Reason == TrackEndReason.Failed)
                _log.LogWarning("{Path} failed mid-play; moving on", e.SourcePath);

            // Repeat.One: replay rather than move. Seek is cheaper than reopening the file, and on
            // a source that cannot seek the reopen below is the honest fallback.
            if (Repeat == RepeatMode.One && e.Reason == TrackEndReason.Completed)
            {
                if (await _engine.SeekAsync(0).ConfigureAwait(false) is not null) return;
                await StartCurrentAsync(CancellationToken.None).ConfigureAwait(false);
                return;
            }

            var enginePath = _engine.CurrentSource;
            QueueItem? armed;
            lock (_gate) armed = PeekNextLocked();

            if (enginePath is not null && armed is not null && enginePath == armed.Path)
            {
                // The engine already handed over gaplessly. Only the bookkeeping is behind.
                lock (_gate) AdvanceCursorLocked(manual: false);
                RaiseCurrentChanged(Current);
                await ArmNextAsync().ConfigureAwait(false);
                return;
            }

            // The engine retired — different formats, nothing armed, or a failure. Start the next
            // track properly, which reopens the device for whatever shape it is.
            bool more;
            lock (_gate) more = AdvanceCursorLocked(manual: false);
            if (!more)
            {
                RaiseCurrentChanged(null);
                return;
            }
            await StartCurrentAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Queue failed to advance after {Path}", e.SourcePath);
        }
    }

    /// <summary>Move the cursor. Caller holds the lock. Returns false when the queue is finished.</summary>
    private bool AdvanceCursorLocked(bool manual)
    {
        if (_order.Count == 0) { _cursor = -1; return false; }

        if (!manual && _repeat == RepeatMode.One) return true;   // stay put

        if (_cursor + 1 < _order.Count) { _cursor++; return true; }

        if (_repeat == RepeatMode.All) { _cursor = 0; return true; }

        _cursor = -1;
        return false;
    }

    /// <summary>What plays after the current track, without moving. Caller holds the lock.</summary>
    private QueueItem? PeekNextLocked()
    {
        if (_order.Count == 0 || _cursor < 0) return null;
        if (_repeat == RepeatMode.One) return CurrentLocked();

        if (_cursor + 1 < _order.Count) return _items[_order[_cursor + 1]];
        return _repeat == RepeatMode.All ? _items[_order[0]] : null;
    }

    private QueueItem? CurrentLocked()
        => _cursor >= 0 && _cursor < _order.Count ? _items[_order[_cursor]] : null;

    /// <summary>
    /// Rebuild the traversal order, keeping whatever is playing under the cursor. Caller holds the
    /// lock. Shuffling must not restart the current track, which is why the current entry is lifted
    /// to the front rather than the whole list being permuted blindly.
    /// </summary>
    private void RebuildOrderLocked()
    {
        var currentItemIndex = _cursor >= 0 && _cursor < _order.Count ? _order[_cursor] : -1;

        _order.Clear();
        for (var i = 0; i < _items.Count; i++) _order.Add(i);

        if (_shuffle)
        {
            // Fisher-Yates.
            for (var i = _order.Count - 1; i > 0; i--)
            {
                var j = Random.Shared.Next(i + 1);
                (_order[i], _order[j]) = (_order[j], _order[i]);
            }

            if (currentItemIndex >= 0)
            {
                var at = _order.IndexOf(currentItemIndex);
                if (at > 0) (_order[0], _order[at]) = (_order[at], _order[0]);
                _cursor = 0;
                return;
            }
        }

        _cursor = currentItemIndex >= 0 ? _order.IndexOf(currentItemIndex) : (_items.Count > 0 ? 0 : -1);
    }

    private void RaiseCurrentChanged(QueueItem? item)
    {
        try { CurrentChanged?.Invoke(this, item); }
        catch (Exception ex) { _log.LogWarning(ex, "CurrentChanged handler threw"); }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        _engine.TrackEnded -= OnTrackEnded;
        return ValueTask.CompletedTask;
    }
}
