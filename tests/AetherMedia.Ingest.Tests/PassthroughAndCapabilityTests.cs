// SPDX-License-Identifier: MIT

namespace AetherMedia.Ingest.Tests;

public sealed class PassthroughAndCapabilityTests
{
    [Fact]
    public async Task Passthrough_transcoder_returns_the_segment_unchanged()
    {
        var segment = new MediaSegment
        {
            Track = TrackKind.Video,
            Codec = "h264",
            Container = "ts",
            RungBitrateKbps = 0,
            PresentationTimeMs = 0,
            DurationMs = 1000,
            Sequence = 3,
            IsKeyframe = true,
            Payload = new byte[] { 1, 2 },
        };

        var result = await new PassthroughTranscoder().NormalizeAsync(segment, TargetProfile.Passthrough);

        Assert.Single(result);
        Assert.Same(segment, result[0]);
    }

    [Fact]
    public void PassthroughOnly_carries_baseline_codecs_and_never_transcodes()
    {
        var capabilities = NodeCapabilities.PassthroughOnly;

        Assert.True(capabilities.CanPassthrough("h264"));
        Assert.True(capabilities.CanPassthrough("AAC")); // case-insensitive
        Assert.False(capabilities.CanTranscode);
        Assert.False(capabilities.CanPassthrough("prores"));
    }
}
