// SPDX-License-Identifier: MIT
namespace Aether.Market.Core;

/// <summary>
/// Classifies what is being offered in a <see cref="MarketListing"/>.
/// </summary>
public enum MarketCategory
{
    /// <summary>Physical or digital goods.</summary>
    Goods     = 0,

    /// <summary>Services offered by a person or business.</summary>
    Services  = 1,

    /// <summary>Labour — time-and-skills engagements (e.g. day labour, gig work).</summary>
    Labour    = 2,

    /// <summary>Land — real-estate or agricultural land listings.</summary>
    Land      = 3,

    /// <summary>
    /// Documents — title deeds, certificates, contracts and other formal documents.
    /// Listings in this category <b>must</b> include an
    /// <see cref="MarketListing.EscrowManifest"/>.
    /// </summary>
    Documents = 4,
}
