// SPDX-License-Identifier: MIT

using AetherMesh.Extensibility;
using AetherMesh.Media.AI.Tests.Helpers;
using AetherMesh.Media.Core.Models;

namespace AetherMesh.Media.AI.Tests;

/// <summary>Unit tests for <see cref="ContentModerator"/>.</summary>
public sealed class ContentModeratorTests
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private static MediaContent MakeContent(string creatorUhid = "test-creator") =>
        new(ContentHash: "hash-001",
            Title: "Test",
            DurationMs: 60_000,
            Codec: "H264",
            ContentType: "video/mp4",
            CreatorUhid: creatorUhid,
            SizeBytes: 1_000_000,
            CreatedAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ThumbnailHash: null,
            Tags: Array.Empty<string>());

    // ── Constructor ────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullAi_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new ContentModerator(null!));

    // ── AssessSourceAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task AssessSource_AiUnavailable_ReturnsNone()
    {
        var ai = new FakeAiProvider { Available = false };
        var mod = new ContentModerator(ai);

        var result = await mod.AssessSourceAsync("any-uhid");

        Assert.Equal(AiThreatLevel.None, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AssessSource_BlankUhid_ReturnsNone(string uhid)
    {
        var ai  = new FakeAiProvider { Available = true };
        var mod = new ContentModerator(ai);

        var result = await mod.AssessSourceAsync(uhid);

        Assert.Equal(AiThreatLevel.None, result);
    }

    [Theory]
    [InlineData(AiThreatLevel.None)]
    [InlineData(AiThreatLevel.Low)]
    [InlineData(AiThreatLevel.Medium)]
    [InlineData(AiThreatLevel.High)]
    public async Task AssessSource_DelegatesToAiProvider(AiThreatLevel expected)
    {
        var ai = new FakeAiProvider { Available = true, ThreatLevel = expected };
        var mod = new ContentModerator(ai);

        var result = await mod.AssessSourceAsync("creator-uhid");

        Assert.Equal(expected, result);
    }

    // ── IsContentSafeAsync ────────────────────────────────────────────────

    [Fact]
    public async Task IsContentSafe_NullContent_Throws()
    {
        var mod = new ContentModerator(new FakeAiProvider());
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            mod.IsContentSafeAsync(null!));
    }

    [Theory]
    [InlineData(AiThreatLevel.None, true)]
    [InlineData(AiThreatLevel.Low,  true)]
    [InlineData(AiThreatLevel.Medium, false)]
    [InlineData(AiThreatLevel.High,   false)]
    public async Task IsContentSafe_ThreatLevel_MapsToSafety(AiThreatLevel level, bool expectedSafe)
    {
        var ai  = new FakeAiProvider { Available = true, ThreatLevel = level };
        var mod = new ContentModerator(ai);

        var safe = await mod.IsContentSafeAsync(MakeContent());

        Assert.Equal(expectedSafe, safe);
    }

    [Fact]
    public async Task IsContentSafe_AiUnavailable_IsPermissive()
    {
        // When AI is down, content must not be incorrectly hidden.
        var ai  = new FakeAiProvider { Available = false };
        var mod = new ContentModerator(ai);

        var safe = await mod.IsContentSafeAsync(MakeContent());

        Assert.True(safe);
    }
}
