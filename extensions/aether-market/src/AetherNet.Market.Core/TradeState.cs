// SPDX-License-Identifier: MIT
namespace AetherNet.Market.Core;

/// <summary>
/// State machine for a <see cref="TradeEscrow"/>.
///
/// <para>Valid forward transitions:</para>
/// <list type="bullet">
///   <item><see cref="Initiated"/> → <see cref="BuyerConfirmed"/></item>
///   <item><see cref="BuyerConfirmed"/> → <see cref="SellerConfirmed"/></item>
///   <item><see cref="SellerConfirmed"/> → <see cref="Complete"/></item>
///   <item>Any state except <see cref="Complete"/> → <see cref="Disputed"/></item>
/// </list>
/// </summary>
public enum TradeState
{
    /// <summary>The trade has been initiated by the buyer.</summary>
    Initiated      = 0,

    /// <summary>The buyer has confirmed receipt intent and locked funds.</summary>
    BuyerConfirmed = 1,

    /// <summary>The seller has confirmed delivery and released the escrow document.</summary>
    SellerConfirmed = 2,

    /// <summary>Both parties confirmed — trade complete and funds released.</summary>
    Complete       = 3,

    /// <summary>The trade is in dispute; a mediator must resolve it.</summary>
    Disputed       = 4,
}
