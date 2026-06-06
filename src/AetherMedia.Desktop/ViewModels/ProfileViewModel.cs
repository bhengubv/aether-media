using System.Collections.ObjectModel;
using AetherMedia.Core;
using AetherMedia.Core.Models;
using AetherMedia.Identity;
using AetherMedia.Social;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AetherMedia.Desktop.ViewModels;

/// <summary>
/// Drives a creator's profile screen: content, live streams, follow state.
/// </summary>
public sealed partial class ProfileViewModel : ViewModelBase
{
    private readonly IProfileService _profileService;
    private readonly ICreatorChannel _channel;
    private readonly ISocialGraph _socialGraph;

    private string? _currentUhid;

    // ── Observable properties ──────────────────────────────────────────────

    [ObservableProperty]
    private MediaProfileViewModel? _profile;

    [ObservableProperty]
    private ObservableCollection<MediaContentViewModel> _creatorContent = [];

    [ObservableProperty]
    private ObservableCollection<LiveStreamViewModel> _creatorStreams = [];

    [ObservableProperty]
    private bool _isFollowing;

    [ObservableProperty]
    private bool _isOwnProfile;

    [ObservableProperty]
    private bool _isLoading;

    // ── Commands ───────────────────────────────────────────────────────────

    public IAsyncRelayCommand FollowCommand { get; }
    public IAsyncRelayCommand UnfollowCommand { get; }
    public IRelayCommand<MediaContentViewModel> PlayContentCommand { get; }

    // ── Events ─────────────────────────────────────────────────────────────

    public event EventHandler<MediaContentViewModel>? PlayRequested;

    // ── Constructor ────────────────────────────────────────────────────────

    public ProfileViewModel(
        IProfileService profileService,
        ICreatorChannel channel,
        ISocialGraph socialGraph)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _channel        = channel        ?? throw new ArgumentNullException(nameof(channel));
        _socialGraph    = socialGraph    ?? throw new ArgumentNullException(nameof(socialGraph));

        FollowCommand = new AsyncRelayCommand(ExecuteFollowAsync, () => !IsFollowing && !IsOwnProfile);
        UnfollowCommand = new AsyncRelayCommand(ExecuteUnfollowAsync, () => IsFollowing && !IsOwnProfile);
        PlayContentCommand = new RelayCommand<MediaContentViewModel>(
            vm =>
            {
                if (vm is not null) PlayRequested?.Invoke(this, vm);
            },
            vm => vm is not null);

        // Keep command CanExecute in sync
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(IsFollowing) or nameof(IsOwnProfile))
            {
                FollowCommand.NotifyCanExecuteChanged();
                UnfollowCommand.NotifyCanExecuteChanged();
            }
        };
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Loads the profile identified by <paramref name="uhid"/> — profile metadata,
    /// content, live streams, and follow state.
    /// </summary>
    public async Task LoadProfileAsync(string uhid)
    {
        if (string.IsNullOrWhiteSpace(uhid))
            throw new ArgumentException("UHID must not be empty.", nameof(uhid));

        _currentUhid = uhid;
        IsLoading    = true;

        try
        {
            // Determine if this is the local user's own profile
            var localProfile = await _profileService.GetLocalProfileAsync();
            IsOwnProfile = localProfile?.Uhid == uhid;

            // Load public profile
            var raw = await _profileService.GetProfileAsync(uhid);
            Profile = raw is not null ? new MediaProfileViewModel(raw) : null;

            // Load content
            var contents = await _channel.GetContentAsync(uhid, limit: 20);
            CreatorContent.Clear();
            foreach (var c in contents)
                CreatorContent.Add(new MediaContentViewModel(c));

            // Load live streams
            var streams = await _channel.GetLiveStreamsAsync(uhid);
            CreatorStreams.Clear();
            foreach (var s in streams)
                CreatorStreams.Add(new LiveStreamViewModel(s));

            // Load follow state (only relevant for other users)
            IsFollowing = !IsOwnProfile && await _socialGraph.IsFollowingAsync(uhid);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task ExecuteFollowAsync()
    {
        if (_currentUhid is null) return;

        await _socialGraph.FollowAsync(_currentUhid);
        IsFollowing = true;
    }

    private async Task ExecuteUnfollowAsync()
    {
        if (_currentUhid is null) return;

        await _socialGraph.UnfollowAsync(_currentUhid);
        IsFollowing = false;
    }
}
