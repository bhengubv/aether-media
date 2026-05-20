// SPDX-License-Identifier: MIT

using Aether.Media.AI;
using Aether.Media.Content;
using Aether.Media.Core;
using Aether.Media.Distribution;
using Aether.Media.Identity;
using Aether.Media.LocalLibrary;
using Aether.Media.LocalLibrary.Interfaces;
using Aether.Media.Social;
using Aether.Media.Streaming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aether.Media.DependencyInjection;

/// <summary>
/// Fluent builder returned by <see cref="ServiceCollectionExtensions.AddAetherMedia"/>.
/// Each <c>Add*</c> method registers the relevant services into the underlying
/// <see cref="IServiceCollection"/> and returns <c>this</c> for chaining.
///
/// All registrations use <c>TryAddSingleton</c> so that host applications can
/// substitute their own implementations simply by registering them before calling
/// the builder.
/// </summary>
public sealed class AetherMediaBuilder
{
    internal IServiceCollection Services { get; }

    internal AetherMediaBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Registers profile management services:
    /// <see cref="IProfileService"/>, <see cref="IProfileSyncService"/>,
    /// and <see cref="IAvatarService"/>.
    /// </summary>
    public AetherMediaBuilder AddIdentity()
    {
        Services.TryAddSingleton<IProfileService,     ProfileService>();
        Services.TryAddSingleton<IProfileSyncService, ProfileSyncService>();
        Services.TryAddSingleton<IAvatarService,      AvatarService>();
        return this;
    }

    /// <summary>
    /// Registers content store and processing services:
    /// <see cref="IMediaLibrary"/>, <see cref="IContentCache"/>,
    /// <see cref="IMetadataResolver"/>, <see cref="IThumbnailService"/>,
    /// and <see cref="IMediaLibraryScanner"/>.
    /// </summary>
    public AetherMediaBuilder AddContent()
    {
        Services.TryAddSingleton<IMediaLibrary,        InMemoryMediaLibrary>();
        Services.TryAddSingleton<IContentCache,        LruContentCache>();
        Services.TryAddSingleton<IMetadataResolver,    MetadataResolver>();
        Services.TryAddSingleton<IThumbnailService,    ThumbnailService>();
        Services.TryAddSingleton<IMediaLibraryScanner, MediaLibraryScanner>();
        return this;
    }

    /// <summary>
    /// Registers social-graph and feed services:
    /// <see cref="ISocialGraph"/>, <see cref="IFeedAggregator"/>,
    /// <see cref="IReactionService"/>, and <see cref="IDiscoveryService"/>.
    /// </summary>
    public AetherMediaBuilder AddSocial()
    {
        Services.TryAddSingleton<ISocialGraph,      SocialGraph>();
        Services.TryAddSingleton<IFeedAggregator,   FeedAggregator>();
        Services.TryAddSingleton<IReactionService,  ReactionService>();
        Services.TryAddSingleton<IDiscoveryService, DiscoveryService>();
        return this;
    }

    /// <summary>
    /// Registers live-streaming services:
    /// <see cref="ILiveStreamPublisher"/>, <see cref="IWatchPartyCoordinator"/>,
    /// and <see cref="IAbrController"/>.
    /// </summary>
    public AetherMediaBuilder AddStreaming()
    {
        Services.TryAddSingleton<ILiveStreamPublisher,    LiveStreamPublisher>();
        Services.TryAddSingleton<IWatchPartyCoordinator,  WatchPartyCoordinator>();
        Services.TryAddSingleton<IAbrController,          AbrController>();
        return this;
    }

    /// <summary>
    /// Registers AI-powered curation services:
    /// <see cref="IContentRanker"/>, <see cref="ICreatorReputationView"/>,
    /// and <see cref="IContentModerator"/>.
    /// </summary>
    public AetherMediaBuilder AddAI()
    {
        Services.TryAddSingleton<IContentRanker,         ContentRanker>();
        Services.TryAddSingleton<ICreatorReputationView, CreatorReputationView>();
        Services.TryAddSingleton<IContentModerator,      ContentModerator>();
        return this;
    }

    /// <summary>
    /// Registers mesh-first app distribution:
    /// <see cref="IMeshAppDistributor"/> — ecosystem catalogue, Cloudflare update checks,
    /// local HTTP bootstrap server, and QR share flow.
    /// </summary>
    public AetherMediaBuilder AddDistribution()
    {
        // HttpClient for Cloudflare version checks + APK downloads
        Services.TryAddSingleton<HttpClient>();
        Services.TryAddSingleton<IMeshAppDistributor, MeshAppDistributor>();
        return this;
    }

    /// <summary>
    /// Registers privacy-first local media management:
    /// <list type="bullet">
    ///   <item><see cref="IMetadataEditor"/> — read/write embedded tags (TagLibSharp).</item>
    ///   <item><see cref="IMovieMetadataService"/> — Kodi-compatible NFO XML files.</item>
    ///   <item><see cref="ICollectionService"/> — manual playlists + smart collections (JSON).</item>
    ///   <item><see cref="IMovieHasher"/> — OpenSubtitles VLC-compatible file hash.</item>
    ///   <item><see cref="ISubtitleService"/> — OpenSubtitles REST API v1 search + download.</item>
    /// </list>
    ///
    /// The subtitle service is registered with a <c>null</c> API key by default — it degrades
    /// gracefully (returns empty results) until a key is supplied.  Configure the key by
    /// registering your own <see cref="ISubtitleService"/> before calling this method, or by
    /// overriding the <see cref="SubtitleService"/> registration after.
    /// </summary>
    public AetherMediaBuilder AddLocalLibrary()
    {
        Services.TryAddSingleton<IMetadataEditor,       MetadataEditor>();
        Services.TryAddSingleton<IMovieMetadataService, MovieMetadataService>();
        Services.TryAddSingleton<ICollectionService,    CollectionService>();
        Services.TryAddSingleton<IMovieHasher,          MovieHasher>();
        Services.TryAddSingleton<HttpClient>();   // shared with Distribution if both registered
        Services.TryAddSingleton<ISubtitleService>(sp =>
            new SubtitleService(
                sp.GetRequiredService<HttpClient>(),
                sp.GetRequiredService<IMovieHasher>(),
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SubtitleService>>(),
                apiKey: null));   // replace with real key via appsettings / env var
        return this;
    }
}
