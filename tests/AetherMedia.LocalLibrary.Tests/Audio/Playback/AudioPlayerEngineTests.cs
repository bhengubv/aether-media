// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Crossfade;
using AetherMedia.LocalLibrary.Audio.Effects;
using AetherMedia.LocalLibrary.Audio.Output;
using AetherMedia.LocalLibrary.Audio.Playback;
using AetherMedia.LocalLibrary.Audio.Plugins;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherMedia.LocalLibrary.Tests.Audio.Playback;

/// <summary>
/// Exercises the pull graph with no sound card and no phone. The engine is deliberately
/// portable so its logic — handover, end-of-stream, position, the effects chain, volume —
/// can be proven here, leaving only the platform decoder and speaker to prove on device.
/// </summary>
public sealed class AudioPlayerEngineTests
{
    private const int Rate = 44_100;
    private const int Channels = 2;

    [Fact]
    public async Task Plays_a_source_and_delivers_its_samples()
    {
        var (engine, output, _) = Build(new FakeSourceFactory(new FakeTrack("a.mp3", 1000)));

        Assert.True(await engine.PlayAsync("a.mp3"));
        Assert.Equal(PlaybackState.Playing, engine.State);

        var got = output.DrainAll();
        Assert.Equal(1000 * Channels, got);
    }

    [Fact]
    public async Task Reports_false_when_no_decoder_can_open_the_file()
    {
        var (engine, _, _) = Build(new FakeSourceFactory());

        // The honest answer for a head with no decoders — not an exception, and not a
        // silent no-op that leaves the UI looking broken.
        Assert.False(await engine.PlayAsync("nothing.xyz"));
        Assert.Equal(PlaybackState.Idle, engine.State);
    }

    [Fact]
    public async Task Position_tracks_frames_actually_delivered()
    {
        var (engine, output, _) = Build(new FakeSourceFactory(new FakeTrack("a.mp3", Rate)));
        await engine.PlayAsync("a.mp3");

        output.Pull(Rate / 2 * Channels);      // exactly half a second of frames

        Assert.InRange(engine.PositionMs, 495, 505);
    }

    [Fact]
    public async Task Hands_over_to_the_queued_track_without_a_gap()
    {
        var (engine, output, _) = Build(new FakeSourceFactory(
            new FakeTrack("a.mp3", 100),
            new FakeTrack("b.mp3", 100)));

        await engine.PlayAsync("a.mp3");
        Assert.True(await engine.SetNextAsync("b.mp3"));

        // One pull that straddles the join: 150 frames spans all 100 of A plus 50 of B.
        // Gapless means the SAME buffer carries the tail of A and the head of B, so a full
        // read is the proof — a gap would come back short at A's boundary.
        var got = output.Pull(150 * Channels);

        Assert.Equal(150 * Channels, got);
        Assert.Equal("b.mp3", engine.CurrentSource);   // B is mid-flight, not merely queued
    }

    [Fact]
    public async Task Retires_after_the_queued_track_also_finishes()
    {
        var (engine, output, _) = Build(new FakeSourceFactory(
            new FakeTrack("a.mp3", 100),
            new FakeTrack("b.mp3", 100)));

        await engine.PlayAsync("a.mp3");
        await engine.SetNextAsync("b.mp3");

        // Ask for more than both tracks hold: A and B drain into one buffer and the engine
        // is then genuinely done — nothing left open, nothing pretending to still be playing.
        Assert.Equal(200 * Channels, output.Pull(400 * Channels));
        Assert.Null(engine.CurrentSource);
    }

    [Fact]
    public async Task Stops_at_end_of_stream_when_nothing_is_queued()
    {
        var (engine, output, _) = Build(new FakeSourceFactory(new FakeTrack("a.mp3", 50)));
        await engine.PlayAsync("a.mp3");

        Assert.Equal(50 * Channels, output.Pull(400 * Channels));
        Assert.Equal(0, output.Pull(64));       // 0 = end of stream, stops the device
    }

    [Fact]
    public async Task A_mismatched_follower_does_not_hand_over_on_the_audio_thread()
    {
        // B is mono where A is stereo. The device is open for one format and cannot be
        // reconfigured from the driver callback, so the engine must decline the handover
        // rather than play B at the wrong speed.
        var (engine, output, _) = Build(new FakeSourceFactory(
            new FakeTrack("a.mp3", 100),
            new FakeTrack("b.mp3", 100, Chans: 1)));

        await engine.PlayAsync("a.mp3");
        await engine.SetNextAsync("b.mp3");

        Assert.Equal(100 * Channels, output.Pull(400 * Channels));
        Assert.Null(engine.CurrentSource);       // retired, waiting for the queue to restart it
    }

    [Fact]
    public async Task Runs_every_enabled_effect_over_the_buffer()
    {
        var (engine, output, dsp) = Build(new FakeSourceFactory(new FakeTrack("a.mp3", 64, Value: 0.25f)));
        dsp.Add(new GainEffect(2.0f));
        await engine.PlayAsync("a.mp3");

        var buffer = output.PullInto(64 * Channels);
        Assert.All(buffer, s => Assert.Equal(0.5f, s, 4));
    }

    [Fact]
    public async Task Disabled_effects_are_skipped()
    {
        var (engine, output, dsp) = Build(new FakeSourceFactory(new FakeTrack("a.mp3", 64, Value: 0.25f)));
        dsp.Add(new GainEffect(2.0f) { IsEnabled = false });
        await engine.PlayAsync("a.mp3");

        var buffer = output.PullInto(64 * Channels);
        Assert.All(buffer, s => Assert.Equal(0.25f, s, 4));
    }

    [Fact]
    public async Task Volume_scales_the_output()
    {
        var (engine, output, _) = Build(new FakeSourceFactory(new FakeTrack("a.mp3", 64, Value: 1.0f)));
        await engine.PlayAsync("a.mp3");
        engine.Volume = 0.5f;

        var buffer = output.PullInto(64 * Channels);
        Assert.All(buffer, s => Assert.Equal(0.5f, s, 4));
    }

    [Fact]
    public async Task A_decoder_that_throws_ends_the_track_instead_of_the_device()
    {
        var factory = new FakeSourceFactory(new FakeTrack("bad.mp3", 100));
        factory.ThrowAfter = 32;
        var (engine, output, _) = Build(factory);

        var ended = new TaskCompletionSource<TrackEndedEventArgs>();
        engine.TrackEnded += (_, e) => ended.TrySetResult(e);

        await engine.PlayAsync("bad.mp3");
        output.Pull(400 * Channels);

        var e = await ended.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(TrackEndReason.Failed, e.Reason);
        Assert.Equal("bad.mp3", e.SourcePath);
    }

    [Fact]
    public async Task Pause_and_resume_preserve_position()
    {
        var (engine, output, _) = Build(new FakeSourceFactory(new FakeTrack("a.mp3", Rate)));
        await engine.PlayAsync("a.mp3");
        output.Pull(Rate / 4 * Channels);

        engine.Pause();
        var atPause = engine.PositionMs;
        Assert.Equal(PlaybackState.Paused, engine.State);

        engine.Resume();
        Assert.Equal(PlaybackState.Playing, engine.State);
        Assert.Equal(atPause, engine.PositionMs);
    }

    [Fact]
    public async Task Seek_moves_the_clock_to_where_the_decoder_landed()
    {
        var (engine, _, _) = Build(new FakeSourceFactory(new FakeTrack("a.mp3", Rate * 10)));
        await engine.PlayAsync("a.mp3");

        var landed = await engine.SeekAsync(5_000);

        Assert.NotNull(landed);
        Assert.InRange(engine.PositionMs, 4_990, 5_010);
    }

    [Fact]
    public async Task Seek_on_an_unseekable_source_reports_null_rather_than_failing()
    {
        var factory = new FakeSourceFactory(new FakeTrack("live.aac", Rate)) { Seekable = false };
        var (engine, _, _) = Build(factory);
        await engine.PlayAsync("live.aac");

        // A live stream has nowhere to seek to. That is a fact about the source, and the
        // caller must be able to tell it apart from an error.
        Assert.Null(await engine.SeekAsync(5_000));
    }

    // ── Harness ───────────────────────────────────────────────────────────────────────

    private static (AudioPlayerEngine, FakeOutput, IDspChain) Build(FakeSourceFactory factory)
    {
        var output = new FakeOutput();
        var dsp = new DspChain();
        var engine = new AudioPlayerEngine(
            factory, output, dsp, new CrossfadeController(),
            NullLogger<AudioPlayerEngine>.Instance);
        return (engine, output, dsp);
    }
}
