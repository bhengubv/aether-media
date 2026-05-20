// SPDX-License-Identifier: MIT
// Aether Media — Interactive Console Demo
//
// Demonstrates all five subsystems registered through AddAetherMedia():
//   1. AddIdentity()   — profile management
//   2. AddContent()    — media library (in-memory)
//   3. AddSocial()     — social graph + feed aggregator
//   4. AddStreaming()  — live stream publisher + ABR controller
//   5. AddAI()         — content ranker + moderator
//
// The social layer (SocialGraph) depends on the Aether protocol's IDtnService and
// IMeshSender.  This demo provides no-op stubs for both so the sample runs without
// a live mesh.  Real deployments wire these from the full aether-protocol DI stack.

using Aether.Dtn;
using Aether.Media.Core;
using Aether.Media.Core.Models;
using Aether.Media.DependencyInjection;
using Aether.Media.Social;
using Aether.Models;
using Aether.Protocol;
using Aether.Routing;
using Microsoft.Extensions.DependencyInjection;

// ── 1. Build the service container ────────────────────────────────────────────

var services = new ServiceCollection();

// Register the no-op Aether protocol stubs so SocialGraph can be resolved.
services.AddSingleton<IDtnService,  NoOpDtnService>();
services.AddSingleton<IMeshSender,  NoOpMeshSender>();

// Register all Aether Media subsystems using the fluent builder.
services.AddAetherMedia(media =>
    media.AddIdentity()
         .AddContent()
         .AddSocial()
         .AddStreaming()
         .AddAI());

await using var provider = services.BuildServiceProvider();

// ── 2. Resolve the key services ────────────────────────────────────────────────

var library = provider.GetRequiredService<IMediaLibrary>();
var graph   = provider.GetRequiredService<ISocialGraph>();
var feed    = provider.GetRequiredService<IFeedAggregator>();

// ── 3. Wire feed events ────────────────────────────────────────────────────────

feed.ItemArrived += (_, item) =>
    Console.WriteLine($"  [feed] new item: \"{item.Content.Title}\"  live={item.IsLive}");

// ── 4. Start the feed aggregator ──────────────────────────────────────────────

await feed.StartAsync();

// ── 5. Add a media item to the library ────────────────────────────────────────

const string CreatorUhid = "KXJB7-MN2P4";

var content = new MediaContent(
    ContentHash:  "a3f9ee2c84b1d6f4230c5d987654e32a1b0c9d8e7f6a5b4c3d2e1f0a9b8c7d6",
    Title:        "Sample Video",
    DurationMs:   125_000,          // 2:05
    Codec:        "H.264",
    ContentType:  "video/mp4",
    CreatorUhid:  CreatorUhid,
    SizeBytes:    15_728_640,        // ~15 MB
    CreatedAt:    DateTime.UtcNow,
    ThumbnailHash: null,
    Tags:         ["demo", "sample", "aether"]);

await library.AddAsync(content);

Console.WriteLine("────────────────────────────────────────────────────────────");
Console.WriteLine("  Aether Media — Console Demo");
Console.WriteLine("────────────────────────────────────────────────────────────");
Console.WriteLine();

// ── 6. Follow a creator ────────────────────────────────────────────────────────

await graph.FollowAsync(CreatorUhid);

// ── 7. Query the library and social graph ─────────────────────────────────────

var stored   = await library.GetAsync(content.ContentHash);
var count    = await library.CountAsync();
var following = await graph.GetFollowingAsync();
var isFollow  = await graph.IsFollowingAsync(CreatorUhid);

// ── 8. Print formatted summary ────────────────────────────────────────────────

Console.WriteLine("  Library");
Console.WriteLine($"    Items in library:  {count}");

if (stored is not null)
{
    Console.WriteLine($"    Title:            {stored.Title}");
    Console.WriteLine($"    Duration:         {stored.FormattedDuration}");
    Console.WriteLine($"    Codec / MIME:     {stored.Codec}  ({stored.ContentType})");
    Console.WriteLine($"    Creator UHID:     {stored.CreatorUhid}");
    Console.WriteLine($"    Is video:         {stored.IsVideo}");
    Console.WriteLine($"    Tags:             [{string.Join(", ", stored.Tags)}]");
}

Console.WriteLine();
Console.WriteLine("  Social Graph");
Console.WriteLine($"    Following \"{CreatorUhid}\": {isFollow}");
Console.WriteLine($"    Following list ({following.Count}):");
foreach (var uhid in following)
    Console.WriteLine($"      • {uhid}");

Console.WriteLine();
Console.WriteLine("  Feed");
var feedItems = await feed.GetFeedAsync(limit: 10);
Console.WriteLine($"    Items in feed:    {feedItems.Count}");

Console.WriteLine();
Console.WriteLine("  Subsystems registered");
Console.WriteLine("    Identity:   IProfileService, IProfileSyncService, IAvatarService");
Console.WriteLine("    Content:    IMediaLibrary, IContentCache, IMetadataResolver, IThumbnailService, IMediaLibraryScanner");
Console.WriteLine("    Social:     ISocialGraph, IFeedAggregator, IReactionService, IDiscoveryService");
Console.WriteLine("    Streaming:  ILiveStreamPublisher, IWatchPartyCoordinator, IAbrController");
Console.WriteLine("    AI:         IContentRanker, ICreatorReputationView, IContentModerator");

Console.WriteLine();
Console.WriteLine("────────────────────────────────────────────────────────────");
Console.WriteLine("  Done. Mesh not required — demo ran fully offline.");
Console.WriteLine("────────────────────────────────────────────────────────────");

// ── 9. Stop the feed aggregator and exit cleanly ──────────────────────────────

await feed.StopAsync();

return 0;

// ─────────────────────────────────────────────────────────────────────────────
// No-op stubs for the Aether protocol dependencies
//
// SocialGraph requires IDtnService and IMeshSender from the mesh protocol layer.
// These stubs satisfy the DI container for demo purposes.  In production, replace
// them with real implementations from Aether.DependencyInjection.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// No-op <see cref="IDtnService"/> that accepts bundle creation calls and
/// immediately returns a dummy receipt without network delivery.
/// </summary>
internal sealed class NoOpDtnService : IDtnService
{
    public event EventHandler<DtnDeliveryReceipt>? BundleDelivered;

    public Task<DtnBundle> CreateBundleAsync(
        string recipientUhid,
        byte[] encryptedPayload,
        BundlePriority priority = BundlePriority.Normal,
        string? recipientLastGeohash = null,
        CancellationToken cancellationToken = default)
    {
        var bundle = new DtnBundle
        {
            Id               = Guid.NewGuid(),
            RecipientUhid    = recipientUhid,
            EncryptedPayload = encryptedPayload,
            Priority         = priority,
            CreatedAt        = DateTime.UtcNow,
            ExpiresAt        = DateTime.UtcNow.AddHours(72),
            Status           = BundleStatus.Delivered,
        };

        // No-op transport delivers immediately — fire the event so callers
        // that await delivery confirmation are not left waiting.
        BundleDelivered?.Invoke(this, new DtnDeliveryReceipt
        {
            BundleId      = bundle.Id,
            RecipientUhid = bundle.RecipientUhid,
            DeliveredAt   = DateTime.UtcNow,
        });

        return Task.FromResult(bundle);
    }

    public Task HandleAsync(MeshPacket packet, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RunDeliveryScanAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<int> ExpireStaleAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0);

    public Task<IReadOnlyList<DtnBundle>> GetActiveBundlesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DtnBundle>>(Array.Empty<DtnBundle>());
}

/// <summary>
/// No-op <see cref="IMeshSender"/> with a fixed local UHID.  All send and
/// broadcast calls are accepted and silently discarded.
/// </summary>
internal sealed class NoOpMeshSender : IMeshSender
{
    public string LocalUhid => "DEMO-LOCAL-NODE";

    public string? LocalGeohash => null;

    public IReadOnlyList<PeerInfo> GetConnectedPeers() => Array.Empty<PeerInfo>();

    public Task<bool> SendAsync(
        MeshPacket packet,
        string nextHopUhid,
        CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<int> BroadcastAsync(
        MeshPacket packet,
        CancellationToken cancellationToken = default)
        => Task.FromResult(0);
}
