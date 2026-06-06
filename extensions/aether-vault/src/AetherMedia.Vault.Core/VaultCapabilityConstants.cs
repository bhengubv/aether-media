// SPDX-License-Identifier: MIT
namespace AetherMedia.Vault.Core;

/// <summary>
/// Capability identifiers exchanged during Aether mesh handshake to advertise
/// Aether Vault support.
/// </summary>
public static class VaultCapabilityConstants
{
    /// <summary>
    /// Capability string for Aether Vault protocol version 1.
    /// Nodes advertising this capability can store, retrieve, and serve
    /// encrypted vault shards.
    /// </summary>
    public const string V1 = "aethernet.vault/v1";
}
