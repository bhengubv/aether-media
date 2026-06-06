// SPDX-License-Identifier: MIT

using AetherNet.Media.Core.Models;

namespace AetherNet.Media.Core.Tests;

/// <summary>
/// Unit tests for <see cref="MediaReaction"/> constructor validation rules:
/// <list type="bullet">
///   <item><description>Comment reactions require a non-empty message.</description></item>
///   <item><description>Non-comment reactions must have a null message.</description></item>
/// </list>
/// </summary>
public sealed class MediaReactionTests
{
    private static readonly Guid   ReactionId   = Guid.NewGuid();
    private const           string ContentHash  = "deadbeefdeadbeef";
    private const           string FromUhid     = "uhid-viewer";

    // ── Comment reactions ──────────────────────────────────────────────────

    [Fact]
    public void Reaction_Comment_RequiresMessage_ThrowsWhenMissing()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new MediaReaction(
                reactionId:  ReactionId,
                contentHash: ContentHash,
                fromUhid:    FromUhid,
                type:        MediaReactionType.Comment,
                positionMs:  0,
                message:     null,          // missing — should throw
                sentAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));

        Assert.Equal("message", ex.ParamName);
    }

    [Fact]
    public void Reaction_Comment_WithMessage_Succeeds()
    {
        var reaction = new MediaReaction(
            reactionId:  ReactionId,
            contentHash: ContentHash,
            fromUhid:    FromUhid,
            type:        MediaReactionType.Comment,
            positionMs:  12_000,
            message:     "Great content!",
            sentAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        Assert.Equal(MediaReactionType.Comment, reaction.Type);
        Assert.Equal("Great content!", reaction.Message);
    }

    // ── Non-comment reactions ──────────────────────────────────────────────

    [Fact]
    public void Reaction_Like_ForbidsMessage_ThrowsWhenPresent()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new MediaReaction(
                reactionId:  ReactionId,
                contentHash: ContentHash,
                fromUhid:    FromUhid,
                type:        MediaReactionType.Like,
                positionMs:  0,
                message:     "unexpected message",   // must be null — should throw
                sentAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));

        Assert.Equal("message", ex.ParamName);
    }

    [Fact]
    public void Reaction_Like_WithoutMessage_Succeeds()
    {
        var reaction = new MediaReaction(
            reactionId:  ReactionId,
            contentHash: ContentHash,
            fromUhid:    FromUhid,
            type:        MediaReactionType.Like,
            positionMs:  0,
            message:     null,
            sentAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        Assert.Equal(MediaReactionType.Like, reaction.Type);
        Assert.Null(reaction.Message);
    }
}
