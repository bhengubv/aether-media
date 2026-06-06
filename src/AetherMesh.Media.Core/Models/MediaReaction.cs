using System.Text.Json.Serialization;

namespace AetherMesh.Media.Core.Models;

/// <summary>
/// A timestamped reaction sent by a viewer to the creator of a piece of content.
/// </summary>
public sealed record MediaReaction
{
    /// <summary>Unique identifier for this reaction event.</summary>
    [JsonPropertyName("reaction_id")]
    public Guid ReactionId { get; init; }

    /// <summary>SHA-256 hex hash of the content being reacted to.</summary>
    [JsonPropertyName("content_hash")]
    public string ContentHash { get; init; }

    /// <summary>Uhid of the viewer sending the reaction.</summary>
    [JsonPropertyName("from_uhid")]
    public string FromUhid { get; init; }

    /// <summary>The kind of reaction.</summary>
    [JsonPropertyName("type")]
    public MediaReactionType Type { get; init; }

    /// <summary>
    /// Position within the media (in milliseconds) where the reaction occurred.
    /// A value of 0 means the reaction was not anchored to a specific position.
    /// </summary>
    [JsonPropertyName("position_ms")]
    public long PositionMs { get; init; }

    /// <summary>
    /// Free-text message body.  Required when <see cref="Type"/> is
    /// <see cref="MediaReactionType.Comment"/>; must be <c>null</c> for all
    /// other reaction types.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>Wall-clock unix milliseconds at which the reaction was sent.</summary>
    [JsonPropertyName("sent_at_ms")]
    public long SentAtMs { get; init; }

    /// <summary>
    /// Initialises and validates a <see cref="MediaReaction"/>.
    /// </summary>
    /// <param name="reactionId">Unique reaction identifier.</param>
    /// <param name="contentHash">SHA-256 hex of the target content.</param>
    /// <param name="fromUhid">Sender's Uhid.</param>
    /// <param name="type">Reaction type.</param>
    /// <param name="positionMs">Playback position in milliseconds.</param>
    /// <param name="message">Comment text (only for <see cref="MediaReactionType.Comment"/>).</param>
    /// <param name="sentAtMs">Unix millisecond timestamp of the reaction.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when a <see cref="MediaReactionType.Comment"/> is missing its
    /// message, or when a non-comment reaction carries a message.
    /// </exception>
    public MediaReaction(
        Guid reactionId,
        string contentHash,
        string fromUhid,
        MediaReactionType type,
        long positionMs,
        string? message,
        long sentAtMs)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
            throw new ArgumentException("ContentHash must not be empty.", nameof(contentHash));
        if (string.IsNullOrWhiteSpace(fromUhid))
            throw new ArgumentException("FromUhid must not be empty.", nameof(fromUhid));
        if (positionMs < 0)
            throw new ArgumentOutOfRangeException(nameof(positionMs), "PositionMs must be >= 0.");

        if (type == MediaReactionType.Comment)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException(
                    "A Message is required for Comment reactions.", nameof(message));
        }
        else
        {
            if (message is not null)
                throw new ArgumentException(
                    $"Message must be null for {type} reactions.", nameof(message));
        }

        ReactionId  = reactionId;
        ContentHash = contentHash;
        FromUhid    = fromUhid;
        Type        = type;
        PositionMs  = positionMs;
        Message     = message;
        SentAtMs    = sentAtMs;
    }
}
