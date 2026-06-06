// SPDX-License-Identifier: MIT
// Aether Media — Interactive Console Demo
//
// Demonstrates all five subsystems registered through AddAetherNetMedia():
//   1. AddIdentity()   — profile management
//   2. AddContent()    — media library (in-memory)
//   3. AddSocial()     — social graph + feed aggregator
//   4. AddStreaming()  — live stream publisher + ABR controller
//   5. AddAI()         — content ranker + moderator
//
// The media subsystems resolve real Aether-protocol services (IStreamingService,
// IContentService, IDtnService, IMeshSender, IHandshakeService, …). This demo wires
// them from the protocol's own DI stack with an in-process transport, so the whole
// sample runs end-to-end fully offline — no live mesh required.

using AetherNet.Media.Core;
using AetherNet.Media.Core.Models;
using AetherNet.Media.DependencyInjection;
using AetherNet.Media.Social;
using AetherNet.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

// ── 1. Build the service container ────────────────────────────────────────────

var services = new ServiceCollection();

// Register the Aether protocol stack with real in-memory, offline implementations.
// AddInProcessTransport wires IMeshSender via an in-process bridge so routing, DTN,
// streaming and handshake run end-to-end with no external network. The media
// subsystems below resolve these protocol services at construction time.
const string LocalUhid = "DEMO-LOCAL-NODE";
services.AddAetherNetProtocol(opts => opts.LocalUhid = LocalUhid)
        .AddInProcessTransport(LocalUhid)
        .AddRouting()
        .AddDtn()
        .AddReputation()
        .AddHandshake()
        .AddContent()
        .AddStreaming();

// Register all Aether Media subsystems using the fluent builder.
services.AddAetherNetMedia(media =>
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
    CreatedAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
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
