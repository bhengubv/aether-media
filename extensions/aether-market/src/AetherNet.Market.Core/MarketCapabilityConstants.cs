// SPDX-License-Identifier: MIT
namespace AetherNet.Market.Core;

/// <summary>
/// Capability identifiers exchanged during Aether mesh handshake to advertise
/// Aether Market support.
/// </summary>
public static class MarketCapabilityConstants
{
    /// <summary>
    /// Capability string for Aether Market protocol version 1.
    /// Nodes advertising this capability can publish, receive, and participate
    /// in peer-to-peer market listings and escrow trades.
    /// </summary>
    public const string V1 = "aethernet.market/v1";
}
