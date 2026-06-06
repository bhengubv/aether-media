// SPDX-License-Identifier: MIT

using AetherMedia.Distribution.Models;

namespace AetherMedia.Distribution;

/// <summary>
/// Hard-coded list of apps in the Aether ecosystem.
/// Each entry carries a Cloudflare <c>latest.json</c> URL so the runtime can
/// fetch the actual download link, hash, and size without hard-coding them here.
///
/// New apps are added to this list on each SDK release; older clients will simply
/// not show apps they were compiled without knowing about.
/// </summary>
public static class EcosystemCatalogue
{
    /// <summary>All known ecosystem apps, in recommended display order.</summary>
    public static readonly IReadOnlyList<AppPackage> Apps = new AppPackage[]
    {
        new()
        {
            AppId        = "aether-media",
            Name         = "Aether Media",
            Version      = "1.0.0",
            ContentHash  = string.Empty,   // populated at runtime from Cloudflare
            SizeBytes    = 0,
            CloudflareUrl= "https://cdn.aethermedia.app/apps/aether-media/latest.json",
            Description  = "Watch, stream and share — no internet required.",
            Tags         = ["media", "streaming", "social"],
        },
        new()
        {
            AppId        = "slepton",
            Name         = "SleptOn",
            Version      = "1.0.0",
            ContentHash  = string.Empty,
            SizeBytes    = 0,
            CloudflareUrl= "https://cdn.aethermedia.app/apps/slepton/latest.json",
            Description  = "The app store for apps the big stores won't show you.",
            Tags         = ["store", "apps", "discovery"],
        },
        new()
        {
            AppId        = "sdpkt",
            Name         = "SDPKT",
            Version      = "1.0.0",
            ContentHash  = string.Empty,
            SizeBytes    = 0,
            CloudflareUrl= "https://cdn.aethermedia.app/apps/sdpkt/latest.json",
            Description  = "Send and receive without banks.",
            Tags         = ["wallet", "payments", "finance"],
        },
        new()
        {
            AppId        = "bidbaas",
            Name         = "BidBaas",
            Version      = "1.0.0",
            ContentHash  = string.Empty,
            SizeBytes    = 0,
            CloudflareUrl= "https://cdn.aethermedia.app/apps/bidbaas/latest.json",
            Description  = "Real-time auctions, no middleman.",
            Tags         = ["marketplace", "auctions", "commerce"],
        },
        new()
        {
            AppId        = "txtme",
            Name         = "TxTMe",
            Version      = "1.0.0",
            ContentHash  = string.Empty,
            SizeBytes    = 0,
            CloudflareUrl= "https://cdn.aethermedia.app/apps/txtme/latest.json",
            Description  = "Messaging that works when nothing else does.",
            Tags         = ["messaging", "chat", "communication"],
        },
        new()
        {
            AppId        = "panik",
            Name         = "Panik",
            Version      = "1.0.0",
            ContentHash  = string.Empty,
            SizeBytes    = 0,
            CloudflareUrl= "https://cdn.aethermedia.app/apps/panik/latest.json",
            Description  = "Emergency SOS that works off-grid.",
            Tags         = ["safety", "emergency", "sos"],
        },
        new()
        {
            AppId        = "tagme",
            Name         = "TagMe",
            Version      = "1.0.0",
            ContentHash  = string.Empty,
            SizeBytes    = 0,
            CloudflareUrl= "https://cdn.aethermedia.app/apps/tagme/latest.json",
            Description  = "Location sharing without GPS dependency.",
            Tags         = ["location", "maps", "sharing"],
        },
    };
}
