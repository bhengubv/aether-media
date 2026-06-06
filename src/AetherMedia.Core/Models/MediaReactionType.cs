namespace AetherMedia.Core.Models;

/// <summary>
/// Classifies the kind of reaction a viewer sends to a creator at a specific
/// point in a piece of media content.
/// </summary>
public enum MediaReactionType
{
    /// <summary>A simple thumbs-up / heart acknowledgement.</summary>
    Like = 1,

    /// <summary>The viewer forwarded the content to their own followers.</summary>
    Share = 2,

    /// <summary>
    /// A timestamped text comment attached to a specific playback position.
    /// A <see cref="MediaReaction.Message"/> value is required for this type.
    /// </summary>
    Comment = 3,

    /// <summary>
    /// A premium reaction (e.g. animated sticker or monetised highlight) that
    /// surfaces prominently in the creator's reaction feed.
    /// </summary>
    SuperReact = 4,
}
