// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using Aether.Media.AI;
using Aether.Media.Core;
using Aether.Media.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aether.Media.UI.Shared.ViewModels;

/// <summary>
/// Drives the home feed screen: ranked content items plus nearby live streams.
/// </summary>
public sealed partial class HomeViewModel : ViewModelBase
{
    private readonly IMediaFeed    _feed;
    private readonly IContentRanker _ranker;

    // ── Observable properties ──────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoContent))]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoContent))]
    private ObservableCollection<MediaFeedItemViewModel> _feedItems = [];

    [ObservableProperty]
    private ObservableCollection<LiveStreamViewModel> _nearbyStreams = [];

    /// <summary>True when the feed is empty and not currently loading.</summary>
    public bool HasNoContent => FeedItems.Count == 0 && !IsLoading;

    // ── Commands ───────────────────────────────────────────────────────────

    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand<MediaFeedItemViewModel> OpenItemCommand { get; }
    public IRelayCommand<LiveStreamViewModel> JoinStreamCommand { get; }

    // ── Events ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Raised when the user opens a feed item or joins a stream.
    /// The argument is either a <see cref="MediaFeedItemViewModel"/> or
    /// a <see cref="LiveStreamViewModel"/>.
    /// </summary>
    public event EventHandler<object>? NavigationRequested;

    // ── Constructor ────────────────────────────────────────────────────────

    public HomeViewModel(IMediaFeed feed, IContentRanker ranker)
    {
        _feed   = feed   ?? throw new ArgumentNullException(nameof(feed));
        _ranker = ranker ?? throw new ArgumentNullException(nameof(ranker));

        RefreshCommand = new AsyncRelayCommand(ExecuteRefreshAsync);
        OpenItemCommand = new RelayCommand<MediaFeedItemViewModel>(
            vm => NavigationRequested?.Invoke(this, vm!),
            vm => vm is not null);
        JoinStreamCommand = new RelayCommand<LiveStreamViewModel>(
            vm => NavigationRequested?.Invoke(this, vm!),
            vm => vm is not null);

        // Subscribe to push-arrived items
        _feed.ItemAdded += OnFeedItemAdded;

        // Kick off initial load
        _ = ExecuteRefreshAsync();
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task ExecuteRefreshAsync()
    {
        IsLoading = true;
        try
        {
            await _feed.RefreshAsync();

            var raw    = await _feed.GetFeedAsync(limit: 50);
            var ranked = await _ranker.RankFeedAsync(raw, viewerUhid: string.Empty);

            FeedItems.Clear();
            foreach (var item in ranked)
                FeedItems.Add(new MediaFeedItemViewModel(item));

            var streams = await _feed.GetNearbyLiveStreamsAsync();
            NearbyStreams.Clear();
            foreach (var stream in streams)
                NearbyStreams.Add(new LiveStreamViewModel(stream));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnFeedItemAdded(object? sender, MediaFeedItem item)
    {
        // Direct update — Blazor component subscribes to PropertyChanged and
        // calls InvokeAsync(StateHasChanged) to marshal to the render thread.
        FeedItems.Insert(0, new MediaFeedItemViewModel(item));
        OnPropertyChanged(nameof(HasNoContent));
    }

    partial void OnIsLoadingChanged(bool value) =>
        OnPropertyChanged(nameof(HasNoContent));
}
