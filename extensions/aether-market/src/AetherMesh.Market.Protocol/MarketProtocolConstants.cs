// SPDX-License-Identifier: MIT
namespace AetherMesh.Market.Protocol;

/// <summary>
/// Packet-type discriminators for Aether Market wire frames.
/// These values are carried in the packet header and must not conflict with
/// other protocol layers.  Aether Market occupies the range 41–43.
/// </summary>
public static class MarketProtocolConstants
{
    /// <summary>
    /// Packet type for <see cref="PoVTokenPacket"/> frames.
    /// Value: <c>41</c>.
    /// </summary>
    public const int PoVToken       = 41;

    /// <summary>
    /// Packet type for <see cref="MarketListingPacket"/> frames.
    /// Value: <c>42</c>.
    /// </summary>
    public const int MarketListing  = 42;

    /// <summary>
    /// Packet type for TradeEscrow frames.
    /// Value: <c>43</c>.
    /// </summary>
    public const int TradeEscrow    = 43;
}
