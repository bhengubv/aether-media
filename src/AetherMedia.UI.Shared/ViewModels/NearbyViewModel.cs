// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using AetherMedia.Core.Models;
using AetherMedia.Social;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AetherMedia.UI.Shared.ViewModels;

/// <summary>Drives the "Nearby" screen: mesh-discovered creators and active live streams.</summary>
public sealed partial class NearbyViewModel : ViewModelBase
{
    private readonly IDiscoveryService _discovery;
    private readonly IFeedAggregator   _aggregator;

    // ── Observable properties ──────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<MediaProfileViewModel> _nearbyCreators = [];

    [ObservableProperty]
    private ObservableCollection<LiveStreamViewModel> _activeStreams = [];

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private int _peerCount;

    // ── Commands ───────────────────────────────────────────────────────────

    public IAsyncRelayCommand StartScanCommand { get; }
    public IRelayCommand<MediaProfileViewModel> ViewProfileCommand { get; }
    public IRelayCommand<LiveStreamViewModel>   WatchStreamCommand { get; }

    // ── Events ─────────────────────────────────────────────────────────────

    public event EventHandler<object>? NavigationRequested;

    // ── Constructor ────────────────────────────────────────────────────────

    public NearbyViewModel(IDiscoveryService discovery, IFeedAggregator aggregator)
    {
        _discovery  = discovery  ?? throw new ArgumentNullException(nameof(discovery));
        _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));

        StartScanCommand = new AsyncRelayCommand(ExecuteStartScanAsync);
        ViewProfileCommand = new RelayCommand<MediaProfileViewModel>(
            vm => NavigationRequested?.Invoke(this, vm!),
            vm => vm is not null);
        WatchStreamCommand = new RelayCommand<LiveStreamViewModel>(
            vm => NavigationRequested?.Invoke(this, vm!),
            vm => vm is not null);

        // Subscribe to creator discovery events
        _discovery.CreatorDiscovered += OnCreatorDiscovered;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task ExecuteStartScanAsync()
    {
        IsScanning = true;
        NearbyCreators.Clear();
        ActiveStreams.Clear();
        PeerCount = 0;

        await _discovery.StartAsync();

        var creators = await _discovery.GetNearbyCreatorsAsync();
        foreach (var profile in creators)
        {
            NearbyCreators.Add(new MediaProfileViewModel(profile));
            PeerCount++;
        }

        var streams = await _discovery.GetActiveStreamsAsync();
        foreach (var stream in streams)
            ActiveStreams.Add(new LiveStreamViewModel(stream));
    }

    private void OnCreatorDiscovered(object? sender, MediaProfile profile)
    {
        // Avoid duplicates by Uhid.
        // Blazor component marshals PropertyChanged to the render thread via InvokeAsync.
        if (NearbyCreators.Any(c => c.Source.Uhid == profile.Uhid))
            return;

        NearbyCreators.Add(new MediaProfileViewModel(profile));
        PeerCount = NearbyCreators.Count;
    }
}
