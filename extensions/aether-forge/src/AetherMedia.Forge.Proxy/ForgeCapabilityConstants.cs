// SPDX-License-Identifier: MIT
namespace AetherMedia.Forge.Proxy;

/// <summary>
/// Capability identifiers exchanged during Aether mesh handshake to advertise
/// Aether Forge support.
/// </summary>
public static class ForgeCapabilityConstants
{
    /// <summary>
    /// Capability string for Aether Forge protocol version 1.
    /// Nodes advertising this capability serve and accept package-cache
    /// entries.
    /// </summary>
    public const string V1 = "aethernet.forge/v1";
}
