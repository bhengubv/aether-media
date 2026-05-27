// SPDX-License-Identifier: MIT
namespace Aether.Space.Protocol;

/// <summary>
/// Capability identifiers exchanged during Aether mesh handshake to advertise
/// Aether Space support.
/// </summary>
public static class SpaceCapabilityConstants
{
    /// <summary>
    /// Capability string for Aether Space protocol version 1.
    /// Nodes advertising this capability can send and receive
    /// <see cref="SpaceBreadcrumbPacket"/> frames.
    /// </summary>
    public const string V1 = "aether.space/v1";
}
