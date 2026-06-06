using AetherMedia.AI;
using AetherMedia.Content;
using AetherMedia.Core;
using AetherMedia.Distribution;
using AetherMedia.Identity;
using AetherMedia.LocalLibrary.Interfaces;
using AetherMedia.Social;
using AetherMedia.Streaming;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AetherMedia.Desktop.ViewModels;

/// <summary>
/// Root view model.  Owns the navigation state and all child VMs.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    // ── Child view models ──────────────────────────────────────────────────

    public HomeViewModel    Home     { get; }
    public NearbyViewModel  Nearby   { get; }
    public LibraryViewModel Library  { get; }
    public PlayerViewModel  Player   { get; }
    public ProfileViewModel Profile  { get; }
    public MoreAppsViewModel MoreApps { get; }

    // ── Observable properties ──────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHomeSelected))]
    [NotifyPropertyChangedFor(nameof(IsNearbySelected))]
    [NotifyPropertyChangedFor(nameof(IsLibrarySelected))]
    [NotifyPropertyChangedFor(nameof(IsPlayerVisible))]
    [NotifyPropertyChangedFor(nameof(IsProfileVisible))]
    [NotifyPropertyChangedFor(nameof(IsMoreAppsSelected))]
    private object _currentViewModel;

    public bool IsHomeSelected     => ReferenceEquals(CurrentViewModel, Home);
    public bool IsNearbySelected   => ReferenceEquals(CurrentViewModel, Nearby);
    public bool IsLibrarySelected  => ReferenceEquals(CurrentViewModel, Library);
    public bool IsPlayerVisible    => ReferenceEquals(CurrentViewModel, Player);
    public bool IsProfileVisible   => ReferenceEquals(CurrentViewModel, Profile);
    public bool IsMoreAppsSelected => ReferenceEquals(CurrentViewModel, MoreApps);

    // ── Navigation commands ────────────────────────────────────────────────

    public IRelayCommand NavigateHomeCommand     { get; }
    public IRelayCommand NavigateNearbyCommand   { get; }
    public IRelayCommand NavigateLibraryCommand  { get; }
    public IRelayCommand NavigatePlayerCommand   { get; }
    public IRelayCommand NavigateProfileCommand  { get; }
    public IRelayCommand NavigateMoreAppsCommand { get; }

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
        ISocialGraph socialGraph,
        IMeshAppDistributor distributor,
        IMetadataEditor metadataEditor,
        IMovieMetadataService movieMetadataService,
        ISubtitleService subtitleService)
    {
        Home     = new HomeViewModel(feed, ranker);
        Nearby   = new NearbyViewModel(discovery, aggregator);
        Library  = new LibraryViewModel(
            library, scanner,
            new MetadataEditorViewModel(metadataEditor, movieMetadataService),
            new SubtitleSearchViewModel(subtitleService));
        Player   = new PlayerViewModel(player, reactions, watchParty);
        Profile  = new ProfileViewModel(profileService, channel, socialGraph);
        MoreApps = new MoreAppsViewModel(distributor);

        // Wire cross-VM navigation
        Home.NavigationRequested   += OnNavigationRequested;
        Nearby.NavigationRequested += OnNavigationRequested;
        Library.PlayRequested      += (_, vm) => NavigateTo(Player);
        Profile.PlayRequested      += (_, vm) => NavigateTo(Player);

        NavigateHomeCommand     = new RelayCommand(() => NavigateTo(Home));
        NavigateNearbyCommand   = new RelayCommand(() => NavigateTo(Nearby));
        NavigateLibraryCommand  = new RelayCommand(() => NavigateTo(Library));
        NavigatePlayerCommand   = new RelayCommand(() => NavigateTo(Player));
        NavigateProfileCommand  = new RelayCommand(() => NavigateTo(Profile));
        NavigateMoreAppsCommand = new RelayCommand(() => NavigateTo(MoreApps));

        // Start on the Home screen
        _currentViewModel = Home;
    }

    // Parameterless constructor for design-time support only
    public MainWindowViewModel()
    {
        // Design-time stubs — not used at runtime
        Home     = null!;
        Nearby   = null!;
        Library  = null!;
        Player   = null!;
        Profile  = null!;
        MoreApps = null!;
        NavigateHomeCommand     = new RelayCommand(() => { });
        NavigateNearbyCommand   = new RelayCommand(() => { });
        NavigateLibraryCommand  = new RelayCommand(() => { });
        NavigatePlayerCommand   = new RelayCommand(() => { });
        NavigateProfileCommand  = new RelayCommand(() => { });
        NavigateMoreAppsCommand = new RelayCommand(() => { });
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
