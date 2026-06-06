// SPDX-License-Identifier: MIT

using AetherNet.Media.Reel;
using AetherNet.Media.Reel.Interfaces;
// Alias avoids the C# namespace/class ambiguity (AetherNet.Media.Reel namespace vs. Reel record)
using ReelModel = AetherNet.Media.Reel.Reel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AetherNet.Media.UI.Shared.ViewModels;

/// <summary>
/// Wraps a single <see cref="ReelFeedItem"/> for binding in the Reel feed UI.
/// </summary>
public sealed partial class ReelItemViewModel : ObservableObject
{
    private readonly IReelService _service;

    public ReelModel    Source      { get; }
    public float        Score       { get; }

    [ObservableProperty] private bool _isLiked;
    [ObservableProperty] private bool _isBookmarked;
    [ObservableProperty] private bool _isBusy;

    public string HashtagDisplay =>
        Source.Hashtags.Length > 0
            ? string.Join("  ", Source.Hashtags.Select(t => $"#{t}"))
            : string.Empty;

    public ReelItemViewModel(ReelFeedItem item, IReelService service)
    {
        _service    = service;
        Source      = item.Reel;
        Score       = item.Score;
        _isLiked    = item.IsLiked;
        _isBookmarked = item.IsBookmarked;
    }

    [RelayCommand]
    private async Task ToggleLikeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            if (IsLiked)
            {
                await _service.UnlikeAsync(Source.ContentHash);
                IsLiked = false;
            }
            else
            {
                await _service.LikeAsync(Source.ContentHash);
                IsLiked = true;
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ToggleBookmarkAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            if (IsBookmarked)
            {
                await _service.UnbookmarkAsync(Source.ContentHash);
                IsBookmarked = false;
            }
            else
            {
                await _service.BookmarkAsync(Source.ContentHash);
                IsBookmarked = true;
            }
        }
        finally { IsBusy = false; }
    }
}
