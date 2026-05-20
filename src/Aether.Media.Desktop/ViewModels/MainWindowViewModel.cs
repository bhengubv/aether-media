using Aether.Media.AI;
using Aether.Media.Content;
using Aether.Media.Core;
using Aether.Media.Identity;
using Aether.Media.Social;
using Aether.Media.Streaming;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aether.Media.Desktop.ViewModels;

/// <summary>
/// Root view model.  Owns the navigation state and all child VMs.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    // ── Child view models ──────────────────────────────────────────────────

    public HomeViewModel    Home    { get; }
    public NearbyViewModel  Nearby  { get; }
    public LibraryViewModel Library { get; }
    public PlayerViewModel  Player  { get; }
    public ProfileViewModel Profile { get; }

    // ── Observable properties ──────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHomeSelected))]
    [NotifyPropertyChangedFor(nameof(IsNearbySelected))]
    [NotifyPropertyChangedFor(nameof(IsLibrarySelected))]
    [NotifyPropertyChangedFor(nameof(IsPlayerVisible))]
    [NotifyPropertyChangedFor(nameof(IsProfileVisible))]
    private object _currentViewModel;

    public bool IsHomeSelected    => ReferenceEquals(CurrentViewModel, Home);
    public bool IsNearbySelected  => ReferenceEquals(CurrentViewModel, Nearby);
    public bool IsLibrarySelected => ReferenceEquals(CurrentViewModel, Library);
    public bool IsPlayerVisible   => ReferenceEquals(CurrentViewModel, Player);
    public bool IsProfileVisible  => ReferenceEquals(CurrentViewModel, Profile);

    // ── Navigation commands ────────────────────────────────────────────────

    public IRelayCommand NavigateHomeCommand    { get; }
    public IRelayCommand NavigateNearbyCommand  { get; }
    public IRelayCommand NavigateLibraryCommand { get; }
    public IRelayCommand NavigatePlayerCommand  { get; }
    public IRelayCommand NavigateProfileCommand { get; }

    // ── Constructor ────────────────────────────────────────────────────────

    public MainWindowViewModel(
        IMediaFeed feed,
        IContentRanker ranker,
        IDiscoveryService discovery,
        IFeedAggregator aggregator,
        IMediaLibrary library,
        IMediaLibraryScanner scanner,
        IMediaPlayer player,
        IReactionService reactions,
        IWatchPartyCoordinator watchParty,
        IProfileService profileService,
        ICreatorChannel channel,
        ISocialGraph socialGraph)
    {
        Home    = new HomeViewModel(feed, ranker);
        Nearby  = new NearbyViewModel(discovery, aggregator);
        Library = new LibraryViewModel(library, scanner);
        Player  = new PlayerViewModel(player, reactions, watchParty);
        Profile = new ProfileViewModel(profileService, channel, socialGraph);

        // Wire cross-VM navigation
        Home.NavigationRequested    += OnNavigationRequested;
        Nearby.NavigationRequested  += OnNavigationRequested;
        Library.PlayRequested       += (_, vm) => NavigateTo(Player);
        Profile.PlayRequested       += (_, vm) => NavigateTo(Player);

        NavigateHomeCommand    = new RelayCommand(() => NavigateTo(Home));
        NavigateNearbyCommand  = new RelayCommand(() => NavigateTo(Nearby));
        NavigateLibraryCommand = new RelayCommand(() => NavigateTo(Library));
        NavigatePlayerCommand  = new RelayCommand(() => NavigateTo(Player));
        NavigateProfileCommand = new RelayCommand(() => NavigateTo(Profile));

        // Start on the Home screen
        _currentViewModel = Home;
    }

    // Parameterless constructor for design-time support only
    public MainWindowViewModel()
    {
        // Design-time stubs — not used at runtime
        Home    = null!;
        Nearby  = null!;
        Library = null!;
        Player  = null!;
        Profile = null!;
        NavigateHomeCommand    = new RelayCommand(() => { });
        NavigateNearbyCommand  = new RelayCommand(() => { });
        NavigateLibraryCommand = new RelayCommand(() => { });
        NavigatePlayerCommand  = new RelayCommand(() => { });
        NavigateProfileCommand = new RelayCommand(() => { });
        _currentViewModel = new object();
    }

    // ── Navigation ─────────────────────────────────────────────────────────

    public void NavigateTo(object vm)
    {
        CurrentViewModel = vm;
    }

    private void OnNavigationRequested(object? sender, object target)
    {
        // Feed item or stream → go to Player
        if (target is MediaFeedItemViewModel or LiveStreamViewModel)
            NavigateTo(Player);
        // Profile → go to Profile
        else if (target is MediaProfileViewModel)
            NavigateTo(Profile);
    }
}
