// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using AetherNet.Media.Reel;
using AetherNet.Media.Reel.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
// Alias avoids the C# namespace/class ambiguity (AetherNet.Media.Reel namespace vs. Reel record)
using ReelModel = AetherNet.Media.Reel.Reel;

namespace AetherNet.Media.UI.Shared.ViewModels;

/// <summary>
/// ViewModel for the Reel short-video feed page.
/// Drives the For You / Following tabs, trending hashtags, and trending sounds.
/// </summary>
public sealed partial class ReelFeedViewModel : ViewModelBase
{
    private readonly IReelFeed      _feed;
    private readonly IReelService   _service;
    private readonly IReelDiscovery _discovery;

    // ── Feed ──────────────────────────────────────────────────────────────
    public ObservableCollection<ReelItemViewModel> Items { get; } = [];

    [ObservableProperty] private ReelItemViewModel? _currentItem;
    [ObservableProperty] private bool   _isLoading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool   _showForYou = true;   // false = Following

    // ── Trending ──────────────────────────────────────────────────────────
    public ObservableCollection<TrendingHashtag> TrendingHashtags { get; } = [];
    public ObservableCollection<TrendingSound>   TrendingSounds   { get; } = [];

    [ObservableProperty] private string _activeHashtag = string.Empty;

    public ReelFeedViewModel(
        IReelFeed      feed,
        IReelService   service,
        IReelDiscovery discovery)
    {
        _feed      = feed;
        _service   = service;
        _discovery = discovery;

        _service.ReelReceived += OnReelReceived;
    }

    // ── Commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadFeedAsync()
    {
        IsLoading     = true;
        StatusMessage = "Loading…";
        Items.Clear();

        try
        {
            IReadOnlyList<ReelFeedItem> page = ShowForYou
                ? await _feed.GetForYouAsync(count: 20)
                : await _feed.GetFollowingAsync(count: 20);

            foreach (var item in page)
                Items.Add(new ReelItemViewModel(item, _service));

            if (Items.Count > 0)
                CurrentItem = Items[0];

            StatusMessage = Items.Count == 0
                ? "No reels yet. Follow creators or go nearby to discover content."
                : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not load feed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadByHashtagAsync(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;
        IsLoading     = true;
        ActiveHashtag = tag;
        StatusMessage = $"#{tag}";
        Items.Clear();

        try
        {
            var page = await _feed.GetByHashtagAsync(tag, count: 20);
            foreach (var item in page)
                Items.Add(new ReelItemViewModel(item, _service));

            StatusMessage = Items.Count == 0 ? $"No reels tagged #{tag} yet." : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not load #{tag}: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void ClearHashtagFilter()
    {
        ActiveHashtag = string.Empty;
        LoadFeedCommand.Execute(null);
    }

    [RelayCommand]
    private async Task LoadTrendingAsync()
    {
        try
        {
            var hashtags = await _discovery.GetTrendingHashtagsAsync(count: 10);
            TrendingHashtags.Clear();
            foreach (var h in hashtags) TrendingHashtags.Add(h);

            var sounds = await _discovery.GetTrendingSoundsAsync(count: 5);
            TrendingSounds.Clear();
            foreach (var s in sounds) TrendingSounds.Add(s);
        }
        catch { /* non-critical — trending panel stays empty */ }
    }

    [RelayCommand]
    private void SwitchTab(bool forYou)
    {
        ShowForYou = forYou;
        ActiveHashtag = string.Empty;
        LoadFeedCommand.Execute(null);
    }

    [RelayCommand]
    private void SetCurrentItem(ReelItemViewModel item)
        => CurrentItem = item;

    // ── Event handlers ────────────────────────────────────────────────────

    private void OnReelReceived(object? sender, ReelModel reel)
    {
        // Prepend freshly received reels at the top of the For You feed
        if (!ShowForYou) return;
        var feedItem = new ReelFeedItem(reel, 1.0f, false, false);
        Items.Insert(0, new ReelItemViewModel(feedItem, _service));
    }

    // ── Cleanup ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        _service.ReelReceived -= OnReelReceived;
        GC.SuppressFinalize(this);
    }
}
