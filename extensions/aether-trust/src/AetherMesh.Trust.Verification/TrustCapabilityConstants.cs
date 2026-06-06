// SPDX-License-Identifier: MIT

namespace AetherMesh.Trust.Verification;

/// <summary>
/// Capability string announced during Aether handshake when the local node
/// has Trust Rings enabled.  A peer that also advertises this capability will
/// attach detached Ed25519 signatures to content it distributes, allowing the
/// receiving node to run attestation automatically.
/// </summary>
public static class TrustCapabilityConstants
{
    /// <summary><c>aethermesh.trust/v1</c></summary>
    public const string V1 = "aethermesh.trust/v1";
}
