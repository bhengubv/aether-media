using AetherMesh.Media.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AetherMesh.Media.Desktop.ViewModels;

/// <summary>
/// Wraps a <see cref="MediaFeedItem"/> for display in the home feed.
/// </summary>
public sealed partial class MediaFeedItemViewModel : ViewModelBase
{
    public MediaFeedItem Source { get; }

    public string Title => Source.Content.Title;
    public string CreatorUhid => Source.Content.CreatorUhid;
    public bool IsLive => Source.IsLive;
    public int LikeCount => Source.LikeCount;
    public int ShareCount => Source.ShareCount;
    public int CommentCount => Source.CommentCount;
    public int WatchCount => Source.WatchCount;
    public string? ThumbnailHash => Source.Content.ThumbnailHash;

    public string FormattedDuration => Source.Content.FormattedDuration;

    /// <summary>
    /// Human-readable relative time, e.g. "just now", "3 minutes ago",
    /// "2 hours ago", "yesterday", or an absolute date for older items.
    /// </summary>
    public string PublishedAgo
    {
        get
        {
            var elapsed = DateTime.UtcNow - Source.PublishedAtMs.ToUniversalTime();

            if (elapsed.TotalSeconds < 60)
                return "just now";
            if (elapsed.TotalMinutes < 60)
            {
                var m = (int)elapsed.TotalMinutes;
                return m == 1 ? "1 minute ago" : $"{m} minutes ago";
            }
            if (elapsed.TotalHours < 24)
            {
                var h = (int)elapsed.TotalHours;
                return h == 1 ? "1 hour ago" : $"{h} hours ago";
            }
            if (elapsed.TotalDays < 2)
                return "yesterday";
            if (elapsed.TotalDays < 7)
            {
                var d = (int)elapsed.TotalDays;
                return $"{d} days ago";
            }
            return Source.PublishedAtMs.ToString("d MMM yyyy");
        }
    }

    public MediaFeedItemViewModel(MediaFeedItem source)
    {
        Source = source;
    }
}
