namespace Aether.Media.Core.Models;

/// <summary>
/// An aggregated feed entry combining a piece of content with engagement
/// counters and the five most recent reactions.
/// </summary>
public sealed record MediaFeedItem(
    MediaContent Content,
    int LikeCount,
    int ShareCount,
    int CommentCount,
    int WatchCount,
    bool IsLive,
    Guid? StreamId,
    IReadOnlyList<MediaReaction> TopReactions,
    DateTime PublishedAt)
{
    /// <summary>
    /// <c>true</c> when the item was published within the last 24 hours (UTC).
    /// </summary>
    public bool IsNew => (DateTime.UtcNow - PublishedAt.ToUniversalTime()).TotalHours < 24.0;

    /// <summary>
    /// Sum of all reaction types: likes + shares + comments.
    /// SuperReacts are not separately counted here because they also
    /// increment the <see cref="LikeCount"/> bucket on the server.
    /// </summary>
    public int ReactionTotal => LikeCount + ShareCount + CommentCount;
}
