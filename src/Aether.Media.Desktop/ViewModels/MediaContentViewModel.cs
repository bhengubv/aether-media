using Aether.Media.Core.Models;

namespace Aether.Media.Desktop.ViewModels;

/// <summary>
/// Wraps a <see cref="MediaContent"/> for display in the library view.
/// </summary>
public sealed class MediaContentViewModel : ViewModelBase
{
    public MediaContent Source { get; }

    public string Title => Source.Title;
    public string CreatorUhid => Source.CreatorUhid;
    public string Codec => Source.Codec;
    public string FormattedDuration => Source.FormattedDuration;
    public string ContentType => Source.ContentType;
    public string? ThumbnailHash => Source.ThumbnailHash;

    /// <summary>
    /// Human-readable file size, e.g. "4.2 MB", "1.3 GB".
    /// </summary>
    public string SizeFormatted
    {
        get
        {
            const long kb = 1024;
            const long mb = kb * 1024;
            const long gb = mb * 1024;

            return Source.SizeBytes switch
            {
                >= gb => $"{Source.SizeBytes / (double)gb:F1} GB",
                >= mb => $"{Source.SizeBytes / (double)mb:F1} MB",
                >= kb => $"{Source.SizeBytes / (double)kb:F0} KB",
                _     => $"{Source.SizeBytes} B"
            };
        }
    }

    public MediaContentViewModel(MediaContent source)
    {
        Source = source;
    }
}
