// SPDX-License-Identifier: MIT

using AetherNet.Media.AI;
using AetherNet.Media.Content;
using AetherNet.Media.Core;
using AetherNet.Media.Distribution;
using AetherNet.Media.Identity;
using AetherNet.Media.LocalLibrary.Interfaces;
using AetherNet.Media.Social;
using AetherNet.Media.Streaming;

namespace AetherNet.Media.UI.Shared.ViewModels;

/// <summary>
/// Root shell view model.  Owns and wires all child VMs.
/// Navigation is handled by the Blazor Router in each host; this class
/// provides cross-VM communication (e.g. Library → Player hand-off).
/// </summary>
public sealed class ShellViewModel
{
    // ── Child view models ──────────────────────────────────────────────────

    public HomeViewModel    Home     { get; }
    public NearbyViewModel  Nearby   { get; }
    public LibraryViewModel Library  { get; }
    public PlayerViewModel  Player   { get; }
    public ProfileViewModel Profile  { get; }
    public MoreAppsViewModel MoreApps { get; }

    // ── Constructor ────────────────────────────────────────────────────────

    public ShellViewModel(
        IMediaFeed            feed,
        IContentRanker        ranker,
        IDiscoveryService     discovery,
        IFeedAggregator       aggregator,
        IMediaLibrary         library,
        IMediaLibraryScanner  scanner,
        IMediaPlayer          player,
        IReactionService      reactions,
        IWatchPartyCoordinator watchParty,
        IProfileService       profileService,
        ICreatorChannel       channel,
        ISocialGraph          socialGraph,
        IMeshAppDistributor   distributor,
        IMetadataEditor       metadataEditor,
        IMovieMetadataService movieMetadataService,
        ISubtitleService      subtitleService,
        IFilePicker           filePicker)
    {
        Home     = new HomeViewModel(feed, ranker);
        Nearby   = new NearbyViewModel(discovery, aggregator);
        Library  = new LibraryViewModel(
            library, scanner,
            new MetadataEditorViewModel(metadataEditor, movieMetadataService),
            new SubtitleSearchViewModel(subtitleService),
            filePicker);
        Player   = new PlayerViewModel(player, reactions, watchParty);
        Profile  = new ProfileViewModel(profileService, channel, socialGraph);
        MoreApps = new MoreAppsViewModel(distributor);
    }
}
