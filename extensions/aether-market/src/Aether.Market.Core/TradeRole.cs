// SPDX-License-Identifier: MIT
namespace Aether.Market.Core;

/// <summary>
/// The role a participant plays in a <see cref="TradeEscrow"/>.
/// </summary>
public enum TradeRole
{
    /// <summary>The party purchasing the item or service.</summary>
    Buyer  = 0,

    /// <summary>The party offering the item or service for sale.</summary>
    Seller = 1,
}
