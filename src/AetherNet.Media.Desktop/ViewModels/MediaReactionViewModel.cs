using AetherNet.Media.Core.Models;

namespace AetherNet.Media.Desktop.ViewModels;

/// <summary>
/// Wraps a <see cref="MediaReaction"/> for display in the player's live reaction feed.
/// </summary>
public sealed class MediaReactionViewModel : ViewModelBase
{
    public MediaReaction Source { get; }

    public string FromUhid => Source.FromUhid;
    public string? Message => Source.Message;

    /// <summary>
    /// Returns an emoji representing the reaction type:
    /// Like → "❤️", Share → "🔁", Comment → "💬", SuperReact → "⭐".
    /// </summary>
    public string TypeEmoji => Source.Type switch
    {
        MediaReactionType.Like       => "❤️",
        MediaReactionType.Share      => "🔁",
        MediaReactionType.Comment    => "💬",
        MediaReactionType.SuperReact => "⭐",
        _                            => "❤️"
    };

    /// <summary>
    /// Playback position formatted as "M:SS" or "H:MM:SS".
    /// Returns "0:00" when the reaction is not anchored to a position.
    /// </summary>
    public string PositionFormatted
    {
        get
        {
            var totalSeconds = Source.PositionMs / 1000L;
            var hours   = totalSeconds / 3600;
            var minutes = (totalSeconds % 3600) / 60;
            var seconds = totalSeconds % 60;

            return hours > 0
                ? $"{hours}:{minutes:D2}:{seconds:D2}"
                : $"{minutes}:{seconds:D2}";
        }
    }

    public MediaReactionViewModel(MediaReaction source)
    {
        Source = source;
    }
}
