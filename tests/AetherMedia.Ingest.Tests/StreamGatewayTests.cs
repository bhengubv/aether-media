// SPDX-License-Identifier: MIT

namespace AetherMedia.Ingest.Tests;

public sealed class StreamGatewayTests
{
    private static MediaSegment Seg(uint sequence, bool keyframe, byte[] bytes) => new()
    {
        Track = TrackKind.Video,
        Codec = "h264",
        Container = "ts",
        RungBitrateKbps = 0,
        PresentationTimeMs = sequence * 1000L,
        DurationMs = 1000,
        Sequence = sequence,
        IsKeyframe = keyframe,
        Payload = bytes,
    };

    private static SourceDescriptor Source() => new() { Uri = new Uri("hls://example/live.m3u8") };

    [Fact]
    public async Task Passthrough_publishes_every_segment_in_order_with_bytes_intact()
    {
        var segments = new[]
        {
            Seg(0, keyframe: true, new byte[] { 1, 2, 3 }),
            Seg(1, keyframe: false, new byte[] { 4, 5, 6 }),
        };
        var publisher = new FakeLiveStreamPublisher();
        var gateway = new StreamGateway(
            new ISourceAdapter[] { new FakeSourceAdapter(segments) }, publisher, new PassthroughTranscoder());

        await using var session = await gateway.StartAsync(Source(), new GatewayOptions());
        await session.Completion;

        Assert.Equal(2, publisher.Frames.Count);
        Assert.Equal(new byte[] { 1, 2, 3 }, publisher.Frames[0].Bytes);
        Assert.True(publisher.Frames[0].IsKeyframe);
        Assert.Equal(0u, publisher.Frames[0].Sequence);
        Assert.Equal(new byte[] { 4, 5, 6 }, publisher.Frames[1].Bytes);
        Assert.Equal(1u, publisher.Frames[1].Sequence);
        Assert.Equal(session.StreamId, publisher.ActiveStreamId);
        Assert.True(publisher.StopCount >= 1);
    }

    [Fact]
    public async Task StreamId_is_live_immediately_and_matches_publisher()
    {
        var publisher = new FakeLiveStreamPublisher();
        var gateway = new StreamGateway(
            new ISourceAdapter[] { new FakeSourceAdapter(new[] { Seg(0, true, new byte[] { 9 }) }) },
            publisher, new PassthroughTranscoder());

        await using var session = await gateway.StartAsync(Source(), new GatewayOptions());

        Assert.NotEqual(Guid.Empty, session.StreamId);
        Assert.Equal(publisher.ActiveStreamId, session.StreamId);
        await session.Completion;
    }

    [Fact]
    public async Task No_matching_adapter_throws()
    {
        var publisher = new FakeLiveStreamPublisher();
        var gateway = new StreamGateway(
            new ISourceAdapter[] { new FakeSourceAdapter(Array.Empty<MediaSegment>(), canHandle: false) },
            publisher, new PassthroughTranscoder());

        await Assert.ThrowsAsync<NotSupportedException>(
            () => gateway.StartAsync(Source(), new GatewayOptions()));
    }

    [Fact]
    public async Task Floor_passes_native_rendition_when_node_cannot_transcode_even_if_ladder_requested()
    {
        var publisher = new FakeLiveStreamPublisher();
        var gateway = new StreamGateway(
            new ISourceAdapter[] { new FakeSourceAdapter(new[] { Seg(0, true, new byte[] { 7, 7 }) }) },
            publisher, new PassthroughTranscoder());
        var options = new GatewayOptions
        {
            Target = new TargetProfile { RungsKbps = new[] { 400, 800 } },
            Capabilities = NodeCapabilities.PassthroughOnly,
        };

        await using var session = await gateway.StartAsync(Source(), options);
        await session.Completion;

        Assert.Single(publisher.Frames);
        Assert.Equal(new byte[] { 7, 7 }, publisher.Frames[0].Bytes);
    }

    [Fact]
    public async Task Capable_node_transcodes_into_multiple_rungs()
    {
        var publisher = new FakeLiveStreamPublisher();
        var gateway = new StreamGateway(
            new ISourceAdapter[] { new FakeSourceAdapter(new[] { Seg(0, true, new byte[] { 5 }) }) },
            publisher, new DoublingTranscoder());
        var options = new GatewayOptions
        {
            Target = new TargetProfile { RungsKbps = new[] { 400, 800 } },
            Capabilities = new NodeCapabilities { CanTranscode = true },
        };

        await using var session = await gateway.StartAsync(Source(), options);
        await session.Completion;

        Assert.Equal(2, publisher.Frames.Count);
        Assert.Equal(0u, publisher.Frames[0].Sequence);
        Assert.Equal(1u, publisher.Frames[1].Sequence);
    }
}
