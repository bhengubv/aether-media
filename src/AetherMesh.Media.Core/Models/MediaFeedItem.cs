using System.Text.Json.Serialization;

namespace AetherMesh.Media.Core.Models;

/// <summary>
/// An aggregated feed entry combining a piece of content with engagement
/// counters and the five most recent reactions.
/// </summary>
public sealed record MediaFeedItem(
    [property: JsonPropertyName("content")]          MediaContent Content,
    [property: JsonPropertyName("like_count")]       int LikeCount,
    [property: JsonPropertyName("share_count")]      int ShareCount,
    [property: JsonPropertyName("comment_count")]    int CommentCount,
    [property: JsonPropertyName("watch_count")]      int WatchCount,
    [property: JsonPropertyName("is_live")]          bool IsLive,
    [property: JsonPropertyName("stream_id")]        Guid? StreamId,
    [property: JsonPropertyName("top_reactions")]    IReadOnlyList<MediaReaction> TopReactions,
    [property: JsonPropertyName("published_at_ms")]  long PublishedAtMs)
{
    /// <summary>
    /// <c>true</c> when the item was published within the last 24 hours (UTC).
    /// </summary>
    [JsonIgnore]
    public bool IsNew => (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - PublishedAtMs) < 86_400_000L;

    /// <summary>
    /// Sum of all reaction types: likes + shares + comments.
    /// SuperReacts are not separately counted here because they also
    /// increment the <see cref="LikeCount"/> bucket on the server.
    /// </summary>
    [JsonIgnore]
    public int ReactionTotal => LikeCount + ShareCount + CommentCount;
}
