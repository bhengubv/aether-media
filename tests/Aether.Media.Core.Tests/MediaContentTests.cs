// SPDX-License-Identifier: MIT

using Aether.Media.Core.Models;

namespace Aether.Media.Core.Tests;

/// <summary>
/// Unit tests for the computed properties on <see cref="MediaContent"/>:
/// <see cref="MediaContent.FormattedDuration"/>, <see cref="MediaContent.IsVideo"/>,
/// and <see cref="MediaContent.IsAudio"/>.
/// </summary>
public sealed class MediaContentTests
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private static MediaContent Build(long durationMs, string contentType = "video/mp4") =>
        new(
            ContentHash:   "abc123",
            Title:         "Test",
            DurationMs:    durationMs,
            Codec:         "h264",
            ContentType:   contentType,
            CreatorUhid:   "uhid-creator",
            SizeBytes:     1024,
            CreatedAt:     DateTime.UtcNow,
            ThumbnailHash: null,
            Tags:          Array.Empty<string>());

    // ── FormattedDuration ──────────────────────────────────────────────────

    [Fact]
    public void FormattedDuration_Zero_ReturnsLive()
    {
        var content = Build(durationMs: 0);
        Assert.Equal("Live", content.FormattedDuration);
    }

    [Fact]
    public void FormattedDuration_SubHour_ReturnsMMSS()
    {
        // 4 minutes 32 seconds = 272 000 ms
        var content = Build(durationMs: 272_000);
        Assert.Equal("4:32", content.FormattedDuration);
    }

    [Fact]
    public void FormattedDuration_OverHour_ReturnsHHMMSS()
    {
        // 1 hour 23 minutes 45 seconds = 5 025 000 ms
        var content = Build(durationMs: 5_025_000);
        Assert.Equal("1:23:45", content.FormattedDuration);
    }

    // ── IsVideo ────────────────────────────────────────────────────────────

    [Fact]
    public void IsVideo_VideoMimeType_ReturnsTrue()
    {
        var content = Build(durationMs: 60_000, contentType: "video/mp4");
        Assert.True(content.IsVideo);
        Assert.False(content.IsAudio);
    }

    // ── IsAudio ────────────────────────────────────────────────────────────

    [Fact]
    public void IsAudio_AudioMimeType_ReturnsTrue()
    {
        var content = Build(durationMs: 180_000, contentType: "audio/mpeg");
        Assert.True(content.IsAudio);
        Assert.False(content.IsVideo);
    }
}
