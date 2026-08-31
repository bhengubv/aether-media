// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Crossfade;
using AetherMedia.LocalLibrary.Audio.Effects;
using AetherMedia.LocalLibrary.Audio.Playback;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherMedia.LocalLibrary.Tests.Audio.Playback;

/// <summary>
/// The order rules — next, previous, shuffle, repeat — and the one genuinely subtle case: the
/// engine may have ALREADY moved on by itself when a track completes, because that continuity is
/// what gapless playback is. The queue has to notice and only catch its bookkeeping up, rather
/// than restarting the track that just began.
/// </summary>
public sealed class PlaybackQueueTests
{
    private const int Channels = PlaybackTestDefaults.Channels;

    [Fact]
    public async Task Plays_the_first_item()
    {
        var (queue, engine, _) = Build(Track("a.mp3"), Track("b.mp3"));

        Assert.True(await queue.PlayAsync(Items("a.mp3", "b.mp3")));

        Assert.Equal("a.mp3", queue.Current?.Path);
        Assert.Equal("a.mp3", engine.CurrentSource);
    }

    [Fact]
    public async Task Arms_the_follower_so_the_handover_has_somewhere_to_go()
    {
        var (queue, engine, output) = Build(Track("a.mp3", 100), Track("b.mp3", 100));
        await queue.PlayAsync(Items("a.mp3", "b.mp3"));

        // If the queue had not pre-armed B, this straddling pull would come back short at A's
        // boundary — the engine will not open a file on the audio thread.
        Assert.Equal(150 * Channels, output.Pull(150 * Channels));
        Assert.Equal("b.mp3", engine.CurrentSource);
    }

    [Fact]
    public async Task Catches_up_its_index_when_the_engine_hands_over_by_itself()
    {
        var (queue, _, output) = Build(Track("a.mp3", 100), Track("b.mp3", 100));
        await queue.PlayAsync(Items("a.mp3", "b.mp3"));

        output.Pull(150 * Channels);            // crosses the join
        await WaitFor(() => queue.Current?.Path == "b.mp3");

        Assert.Equal("b.mp3", queue.Current?.Path);
        Assert.Equal(1, queue.CursorIndex);     // index followed; the track was NOT restarted
    }

    [Fact]
    public async Task Next_skips_forward()
    {
        var (queue, engine, _) = Build(Track("a.mp3"), Track("b.mp3"), Track("c.mp3"));
        await queue.PlayAsync(Items("a.mp3", "b.mp3", "c.mp3"));

        Assert.True(await queue.NextAsync());

        Assert.Equal("b.mp3", queue.Current?.Path);
        Assert.Equal("b.mp3", engine.CurrentSource);
    }

    [Fact]
    public async Task Next_at_the_end_stops_when_repeat_is_off()
    {
        var (queue, _, _) = Build(Track("a.mp3"));
        await queue.PlayAsync(Items("a.mp3"));

        Assert.False(await queue.NextAsync());
        Assert.Null(queue.Current);
    }

    [Fact]
    public async Task Next_at_the_end_wraps_when_repeating_all()
    {
        var (queue, _, _) = Build(Track("a.mp3"), Track("b.mp3"));
        await queue.PlayAsync(Items("a.mp3", "b.mp3"), startIndex: 1);
        queue.Repeat = RepeatMode.All;

        Assert.True(await queue.NextAsync());
        Assert.Equal("a.mp3", queue.Current?.Path);
    }

    [Fact]
    public async Task Previous_restarts_the_track_when_more_than_three_seconds_in()
    {
        // The convention every music player uses: "previous" early in a track means the one
        // before; later in a track it means "start this one again".
        var (queue, engine, output) = Build(
            Track("a.mp3", PlaybackTestDefaults.Rate * 10),
            Track("b.mp3"));
        await queue.PlayAsync(Items("a.mp3", "b.mp3"), startIndex: 0);

        output.Pull(PlaybackTestDefaults.Rate * 5 * Channels);   // five seconds in
        Assert.True(await queue.PreviousAsync());

        Assert.Equal("a.mp3", queue.Current?.Path);
        Assert.InRange(engine.PositionMs, 0, 50);
    }

    [Fact]
    public async Task Previous_early_in_a_track_goes_to_the_one_before()
    {
        var (queue, _, _) = Build(Track("a.mp3"), Track("b.mp3"));
        await queue.PlayAsync(Items("a.mp3", "b.mp3"), startIndex: 1);

        Assert.True(await queue.PreviousAsync());
        Assert.Equal("a.mp3", queue.Current?.Path);
    }

    [Fact]
    public async Task Skips_a_file_no_decoder_can_open_instead_of_stalling()
    {
        var factory = new FakeSourceFactory(Track("a.mp3"), Track("b.mp3"));
        factory.Undecodable.Add("a.mp3");
        var (queue, engine, _) = Build(factory);

        Assert.True(await queue.PlayAsync(Items("a.mp3", "b.mp3")));

        Assert.Equal("b.mp3", queue.Current?.Path);
        Assert.Equal("b.mp3", engine.CurrentSource);
    }

    [Fact]
    public async Task A_queue_of_entirely_unplayable_files_ends_rather_than_spinning()
    {
        var factory = new FakeSourceFactory(Track("a.mp3"), Track("b.mp3"));
        factory.Undecodable.Add("a.mp3");
        factory.Undecodable.Add("b.mp3");
        var (queue, _, _) = Build(factory);

        Assert.False(await queue.PlayAsync(Items("a.mp3", "b.mp3")));
        Assert.Null(queue.Current);
    }

    [Fact]
    public async Task Shuffle_keeps_playing_the_current_track()
    {
        var (queue, engine, _) = Build(
            Track("a.mp3"), Track("b.mp3"), Track("c.mp3"), Track("d.mp3"));
        await queue.PlayAsync(Items("a.mp3", "b.mp3", "c.mp3", "d.mp3"), startIndex: 2);

        queue.Shuffle = true;

        // Shuffling must reorder what is COMING, never interrupt what is playing.
        Assert.Equal("c.mp3", queue.Current?.Path);
        Assert.Equal("c.mp3", engine.CurrentSource);
    }

    [Fact]
    public async Task Turning_shuffle_off_restores_the_real_order()
    {
        var (queue, _, _) = Build(Track("a.mp3"), Track("b.mp3"), Track("c.mp3"));
        await queue.PlayAsync(Items("a.mp3", "b.mp3", "c.mp3"), startIndex: 0);

        queue.Shuffle = true;
        queue.Shuffle = false;

        Assert.Equal("a.mp3", queue.Current?.Path);
        Assert.True(await queue.NextAsync());
        Assert.Equal("b.mp3", queue.Current?.Path);   // the album order, not a leftover permutation
    }

    [Fact]
    public async Task Shuffle_still_covers_every_track_exactly_once()
    {
        var (queue, _, _) = Build(
            Track("a.mp3"), Track("b.mp3"), Track("c.mp3"), Track("d.mp3"), Track("e.mp3"));
        await queue.PlayAsync(Items("a.mp3", "b.mp3", "c.mp3", "d.mp3", "e.mp3"));
        queue.Shuffle = true;

        var seen = new List<string> { queue.Current!.Path };
        while (await queue.NextAsync()) seen.Add(queue.Current!.Path);

        Assert.Equal(5, seen.Count);
        Assert.Equal(5, seen.Distinct().Count());   // a permutation, not a resampling
    }

    [Fact]
    public async Task Enqueue_appends_without_disturbing_playback()
    {
        var (queue, engine, _) = Build(Track("a.mp3"), Track("b.mp3"));
        await queue.PlayAsync(Items("a.mp3"));

        queue.Enqueue(new QueueItem("b.mp3"));

        Assert.Equal("a.mp3", engine.CurrentSource);
        Assert.Equal(2, queue.Items.Count);
        Assert.True(await queue.NextAsync());
        Assert.Equal("b.mp3", queue.Current?.Path);
    }

    [Fact]
    public void DisplayTitle_falls_back_to_the_filename()
    {
        // An untagged file must show something a person recognises, not an empty row.
        Assert.Equal("Track 01", new QueueItem("/music/Track 01.mp3").DisplayTitle);
        Assert.Equal("Real Title", new QueueItem("/music/Track 01.mp3", "Real Title").DisplayTitle);
    }

    [Fact]
    public async Task Clear_stops_playback_and_empties_the_list()
    {
        var (queue, engine, _) = Build(Track("a.mp3"));
        await queue.PlayAsync(Items("a.mp3"));

        queue.Clear();

        Assert.Empty(queue.Items);
        Assert.Null(queue.Current);
        Assert.Null(engine.CurrentSource);
    }

    // ── Harness ───────────────────────────────────────────────────────────────────────

    private static FakeTrack Track(string path, int frames = 500) => new(path, frames);

    private static QueueItem[] Items(params string[] paths)
        => [.. paths.Select(p => new QueueItem(p))];

    private static (PlaybackQueue, AudioPlayerEngine, FakeOutput) Build(params FakeTrack[] tracks)
        => Build(new FakeSourceFactory(tracks));

    private static (PlaybackQueue, AudioPlayerEngine, FakeOutput) Build(FakeSourceFactory factory)
    {
        var output = new FakeOutput();
        var engine = new AudioPlayerEngine(
            factory, output, new DspChain(), new CrossfadeController(),
            NullLogger<AudioPlayerEngine>.Instance);
        var queue = new PlaybackQueue(engine, NullLogger<PlaybackQueue>.Instance);
        return (queue, engine, output);
    }

    /// <summary>
    /// The engine raises completions on the thread pool so the audio thread never runs caller
    /// code, which means the queue advances asynchronously. Poll rather than sleep a fixed span:
    /// a fixed sleep is either flaky or slow, and usually both.
    /// </summary>
    private static async Task WaitFor(Func<bool> condition, int timeoutMs = 5_000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            if (condition()) return;
            await Task.Delay(10);
        }
        throw new TimeoutException($"Condition not met within {timeoutMs} ms");
    }
}
