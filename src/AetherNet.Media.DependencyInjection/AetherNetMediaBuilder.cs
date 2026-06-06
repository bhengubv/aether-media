// SPDX-License-Identifier: MIT

using AetherNet.Media.AI;
using AetherNet.Media.Content;
using AetherNet.Media.Core;
using AetherNet.Media.Distribution;
using AetherNet.Media.Identity;
using AetherNet.Media.LocalLibrary;
using AetherNet.Media.LocalLibrary.Interfaces;
using AetherNet.Media.Reel;
using AetherNet.Media.Reel.Interfaces;
using AetherNet.Media.Social;
using AetherNet.Media.Streaming;
using AetherNet.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AetherNet.Media.DependencyInjection;

/// <summary>
/// Fluent builder returned by <see cref="ServiceCollectionExtensions.AddAetherNetMedia"/>.
/// Each <c>Add*</c> method registers the relevant services into the underlying
/// <see cref="IServiceCollection"/> and returns <c>this</c> for chaining.
///
/// All registrations use <c>TryAddSingleton</c> so that host applications can
/// substitute their own implementations simply by registering them before calling
/// the builder.
/// </summary>
public sealed class AetherNetMediaBuilder
{
    public IServiceCollection Services { get; }

    internal AetherNetMediaBuilder(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Registers profile management services:
    /// <see cref="IProfileService"/>, <see cref="IProfileSyncService"/>,
    /// and <see cref="IAvatarService"/>.
    /// </summary>
    public AetherNetMediaBuilder AddIdentity()
    {
        Services.TryAddSingleton<IProfileService,     ProfileService>();
        Services.TryAddSingleton<IProfileSyncService>(sp => new ProfileSyncService(
            sp.GetRequiredService<IProfileService>(),
            sp.GetRequiredService<IMeshSender>(),
            sp.GetService<Microsoft.Extensions.Logging.ILogger<ProfileSyncService>>(),
            sp.GetService<FootprintGuard>()));
        Services.TryAddSingleton<IAvatarService,      AvatarService>();
        return this;
    }

    /// <summary>
    /// Registers content store and processing services:
    /// <see cref="IMediaLibrary"/>, <see cref="IContentCache"/>,
    /// <see cref="IMetadataResolver"/>, <see cref="IThumbnailService"/>,
    /// and <see cref="IMediaLibraryScanner"/>.
    ///
    /// If <see cref="AddFootprintPolicy"/> has been called (or a <see cref="FootprintOptions"/>
    /// is already registered), the cache capacity is taken from
    /// <see cref="FootprintOptions.StorageCapBytes"/>; otherwise the 500 MiB default applies.
    /// </summary>
    public AetherNetMediaBuilder AddContent()
    {
        Services.TryAddSingleton<IMediaLibrary, InMemoryMediaLibrary>();

        // Respect FootprintOptions.StorageCapBytes when it has been registered;
        // fall back to LruContentCache.DefaultCapacityBytes (500 MiB) otherwise.
        Services.TryAddSingleton<IContentCache>(sp =>
        {
            var opts = sp.GetService<FootprintOptions>();
            return new LruContentCache(opts?.StorageCapBytes ?? 0);
        });

        Services.TryAddSingleton<IMetadataResolver,    MetadataResolver>();
        Services.TryAddSingleton<IThumbnailService,    ThumbnailService>();
        Services.TryAddSingleton<IMediaLibraryScanner, MediaLibraryScanner>();
        return this;
    }

    /// <summary>
    /// Registers the three-axis device footprint policy.
    ///
    /// <list type="bullet">
    ///   <item><b>Storage</b> — <see cref="FootprintOptions.StorageCapBytes"/> caps the LRU content
    ///         cache (default 500 MiB; picked up automatically by <see cref="AddContent"/>).</item>
    ///   <item><b>Network</b> — <see cref="INetworkPolicy"/> gates seeding and mesh scanning on
    ///         metered connections (mobile data, tethered hotspot).</item>
    ///   <item><b>Power</b>   — <see cref="IPowerPolicy"/> drops the node to passive mode below
    ///         the configured battery threshold or when the screen is off.</item>
    /// </list>
    ///
    /// The null implementations (<see cref="NullNetworkPolicy"/>, <see cref="NullPowerPolicy"/>)
    /// are registered as defaults and are safe for desktop and test use.  Override them by
    /// registering platform-specific implementations <em>before</em> calling this method.
    ///
    /// <see cref="FootprintGuard"/> is registered as a singleton and is the single call-site
    /// that all subsystems use to check whether seeding or scanning is currently permitted.
    /// </summary>
    /// <param name="configure">Optional delegate to override default option values.</param>
    public AetherNetMediaBuilder AddFootprintPolicy(Action<FootprintOptions>? configure = null)
    {
        var options = new FootprintOptions();
        configure?.Invoke(options);

        // Options — singleton so IContentCache and FootprintGuard both resolve the same instance.
        Services.TryAddSingleton(options);

        // Null defaults — overridden by platform implementations if already registered.
        Services.TryAddSingleton<INetworkPolicy>(NullNetworkPolicy.Instance);
        Services.TryAddSingleton<IPowerPolicy>(NullPowerPolicy.Instance);

        // Guard — the single boolean oracle injected into seeding / scanning services.
        Services.TryAddSingleton<FootprintGuard>();

        return this;
    }

    /// <summary>
    /// Registers social-graph and feed services:
    /// <see cref="ISocialGraph"/>, <see cref="IFeedAggregator"/>,
    /// <see cref="IReactionService"/>, and <see cref="IDiscoveryService"/>.
    /// </summary>
    public AetherNetMediaBuilder AddSocial()
    {
        Services.TryAddSingleton<ISocialGraph,      SocialGraph>();
        Services.TryAddSingleton<IFeedAggregator,   FeedAggregator>();
        Services.TryAddSingleton<IReactionService,  ReactionService>();
        Services.TryAddSingleton<IDiscoveryService>(sp => new DiscoveryService(
            sp.GetRequiredService<AetherNet.Handshake.IHandshakeService>(),
            sp.GetRequiredService<AetherNet.Streaming.IStreamingService>(),
            sp.GetRequiredService<IProfileService>(),
            sp.GetService<FootprintGuard>()));
        return this;
    }

    /// <summary>
    /// Registers live-streaming services:
    /// <see cref="ILiveStreamPublisher"/>, <see cref="IWatchPartyCoordinator"/>,
    /// and <see cref="IAbrController"/>.
    ///
    /// <para>
    /// When <see cref="AetherNet.Extensibility.IAetherNetAiProvider"/> is registered in the
    /// container, <see cref="AbrController"/> receives it as an optional dependency and
    /// applies AI transport-bias signals to its EMA bandwidth estimation. If no provider
    /// is registered the controller operates as a pure-EMA ABR controller.
    /// </para>
    /// </summary>
    public AetherNetMediaBuilder AddStreaming()
    {
        Services.TryAddSingleton<ILiveStreamPublisher,   LiveStreamPublisher>();
        Services.TryAddSingleton<IWatchPartyCoordinator, WatchPartyCoordinator>();
        Services.TryAddSingleton<IAbrController>(sp => new AbrController(
            sp.GetRequiredService<AetherNet.Streaming.IStreamingService>(),
            ai: sp.GetService<AetherNet.Extensibility.IAetherNetAiProvider>()));
        return this;
    }

    /// <summary>
    /// Registers AI-powered curation services:
    /// <see cref="IContentRanker"/>, <see cref="ICreatorReputationView"/>,
    /// <see cref="IContentModerator"/>, <see cref="IWatchHistoryStore"/>,
    /// and <see cref="IRoutePreseeder"/>.
    ///
    /// <para>
    /// <see cref="InMemoryWatchHistoryStore"/> is registered as the default
    /// <see cref="IWatchHistoryStore"/>. Replace it before calling this method to
    /// use a persistent store (e.g. SQLite-backed implementation).
    /// </para>
    ///
    /// <para>
    /// <see cref="RoutePreseeder"/> is registered with
    /// <see cref="AetherNet.Routing.IRoutingService"/> as an optional dependency.
    /// Route pre-warming is silently skipped when the routing service is not
    /// registered in the container (e.g. in lightweight or test setups).
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Prerequisite:</b> <see cref="AetherNet.Extensibility.IAetherNetAiProvider"/>
    /// must be present in the container before this method's factory lambdas are
    /// resolved (i.e. before the first service is requested from the built
    /// <see cref="IServiceProvider"/>).
    /// </para>
    /// <para>
    /// This requirement is automatically satisfied by
    /// <c>services.AddAetherNetProtocol()</c>, which registers
    /// <c>NullAetherNetAiProvider</c> as the fallback when no CircleAI SDK is
    /// installed. When CircleAI is present it registers its own implementation
    /// first; the protocol builder's <c>TryAddSingleton</c> call then becomes a
    /// no-op, so there is always exactly one <see cref="AetherNet.Extensibility.IAetherNetAiProvider"/>
    /// singleton in the container — shared by both the protocol layer and every
    /// Aether Media service registered here.
    /// </para>
    /// <para>
    /// If you call <c>AddAetherNetMedia().AddAI()</c> on a bare
    /// <see cref="IServiceCollection"/> that has no
    /// <see cref="AetherNet.Extensibility.IAetherNetAiProvider"/> registration, an
    /// <see cref="InvalidOperationException"/> will be thrown at the point the
    /// first AI-dependent service (e.g. <see cref="IContentRanker"/>) is resolved.
    /// </para>
    /// </remarks>
    public AetherNetMediaBuilder AddAI()
    {
        Services.TryAddSingleton<IWatchHistoryStore,     InMemoryWatchHistoryStore>();
        Services.TryAddSingleton<IContentRanker>(sp => new ContentRanker(
            sp.GetRequiredService<AetherNet.Reputation.INodeReputationService>(),
            sp.GetRequiredService<AetherNet.Extensibility.IAetherNetAiProvider>(),
            sp.GetRequiredService<IContentModerator>(),
            sp.GetRequiredService<IWatchHistoryStore>()));
        Services.TryAddSingleton<ICreatorReputationView, CreatorReputationView>();
        Services.TryAddSingleton<IContentModerator,      ContentModerator>();
        Services.TryAddSingleton<IRoutePreseeder>(sp => new RoutePreseeder(
            sp.GetRequiredService<AetherNet.Extensibility.IAetherNetAiProvider>(),
            sp.GetService<AetherNet.Routing.IRoutingService>()));
        return this;
    }

    /// <summary>
    /// Registers mesh-first app distribution:
    /// <see cref="IMeshAppDistributor"/> — ecosystem catalogue, Cloudflare update checks,
    /// local HTTP bootstrap server, and QR share flow.
    /// </summary>
    public AetherNetMediaBuilder AddDistribution()
    {
        // HttpClient for Cloudflare version checks + APK downloads
        Services.TryAddSingleton<HttpClient>();
        Services.TryAddSingleton<IMeshAppDistributor>(sp => new MeshAppDistributor(
            sp.GetRequiredService<AetherNet.Content.IContentService>(),
            sp.GetRequiredService<HttpClient>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MeshAppDistributor>>(),
            sp.GetService<FootprintGuard>()));
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
    public AetherNetMediaBuilder AddLocalLibrary()
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

    /// <summary>
    /// Registers the decentralised Reel (short-video) platform:
    /// <list type="bullet">
    ///   <item><see cref="IReelEngagementTracker"/> — on-device engagement signals (never leaves device).</item>
    ///   <item><see cref="IReelDiscovery"/> — mesh-gossipped trending + local Reel index.</item>
    ///   <item><see cref="IReelService"/> — publish, interact, comment, duet, stitch.</item>
    ///   <item><see cref="IReelFeed"/> — on-device For You algorithm with tunable weights.</item>
    ///   <item><see cref="ISoundLibrary"/> — content-addressed sound library.</item>
    /// </list>
    ///
    /// Requires <c>AddContent()</c> to be called first (or an <c>IContentService</c>
    /// to be registered) — Reels are distributed as Aether content chunks.
    /// </summary>
    /// <param name="localUhid">
    /// UHID of the local node — set as the <c>CreatorUhid</c> on published Reels.
    /// </param>
    public AetherNetMediaBuilder AddReel(string localUhid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localUhid);

        Services.TryAddSingleton<IReelEngagementTracker, ReelEngagementTracker>();
        Services.TryAddSingleton<IReelDiscovery,         ReelDiscovery>();

        Services.TryAddSingleton<IReelService>(sp => new ReelService(
            sp.GetRequiredService<AetherNet.Content.IContentService>(),
            sp.GetRequiredService<IReelDiscovery>(),
            localUhid,
            sp.GetService<Microsoft.Extensions.Logging.ILogger<ReelService>>()));

        Services.TryAddSingleton<IReelFeed>(sp => new ReelFeed(
            sp.GetRequiredService<IReelService>(),
            sp.GetRequiredService<IReelEngagementTracker>(),
            sp.GetRequiredService<IReelDiscovery>(),
            sp.GetService<Microsoft.Extensions.Logging.ILogger<ReelFeed>>()));

        Services.TryAddSingleton<ISoundLibrary>(sp => new SoundLibrary(
            sp.GetRequiredService<AetherNet.Content.IContentService>(),
            localUhid,
            sp.GetService<Microsoft.Extensions.Logging.ILogger<SoundLibrary>>()));

        return this;
    }
}
