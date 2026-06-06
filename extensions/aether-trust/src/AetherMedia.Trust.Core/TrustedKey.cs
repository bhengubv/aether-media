// SPDX-License-Identifier: MIT

namespace AetherMedia.Trust.Core;

/// <summary>
/// An Ed25519 public key that has been admitted to a trust ring for a
/// specific <see cref="ContentCategory"/>.
/// </summary>
/// <param name="PublicKey">
/// Raw Ed25519 public key bytes in SubjectPublicKeyInfo (DER) format —
/// the same format used throughout the Aether protocol layer.
/// </param>
/// <param name="Category">The content category this key is trusted to sign.</param>
/// <param name="Label">Human-readable identifier, e.g. "bhengubv-release-key".</param>
/// <param name="AddedAtUtc">When this key was admitted to the ring.</param>
public sealed record TrustedKey(
    byte[]          PublicKey,
    ContentCategory Category,
    string          Label,
    DateTime        AddedAtUtc);
