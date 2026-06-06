// SPDX-License-Identifier: MIT

namespace AetherMedia.Trust.Core;

/// <summary>
/// Lifecycle status of a content block's cryptographic attestation.
/// </summary>
public enum ContentTrustStatus
{
    /// <summary>Content has not yet been submitted for verification.</summary>
    Unknown,

    /// <summary>
    /// SHA-256 hash matched and at least one trusted ring key verified the
    /// detached Ed25519 signature (or the ring for this category is empty).
    /// </summary>
    Verified,

    /// <summary>
    /// The SHA-256 digest of the assembled bytes does not match the declared
    /// <c>contentHash</c>. The content is structurally corrupt.
    /// </summary>
    HashMismatch,

    /// <summary>
    /// The hash matched but no trusted key in the ring produced a valid
    /// Ed25519 signature over the content bytes, or no signature was provided
    /// when the ring requires one.
    /// </summary>
    SignatureFailed,

    /// <summary>
    /// Content has been quarantined: it is retained in storage but will not be
    /// served to consumers or surfaced in feeds.  This is set automatically on
    /// <see cref="HashMismatch"/> or <see cref="SignatureFailed"/>, and can
    /// also be set manually via <see cref="ITrustRingService.QuarantineAsync"/>.
    /// </summary>
    Quarantined,

    /// <summary>
    /// The content category has no keys configured in the ring, so signature
    /// attestation is not required. Hash validation still passed.
    /// </summary>
    NoRingRequired,
}
