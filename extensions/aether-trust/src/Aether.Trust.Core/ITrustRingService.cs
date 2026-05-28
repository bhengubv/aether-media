// SPDX-License-Identifier: MIT

namespace Aether.Trust.Core;

/// <summary>
/// Local cryptographic attestation engine — the Trust Rings core service.
///
/// <para>
/// A trust ring is a per-<see cref="ContentCategory"/> set of Ed25519 public
/// keys whose signatures are accepted as proof-of-authorship for that category.
/// </para>
///
/// <para><b>Verification pipeline (called after content is assembled):</b></para>
/// <list type="number">
///   <item>SHA-256 the assembled bytes and compare to the declared content hash.
///         Mismatch → <see cref="ContentTrustStatus.HashMismatch"/> + quarantine.</item>
///   <item>If the category ring is empty → <see cref="ContentTrustStatus.NoRingRequired"/>.</item>
///   <item>Validate the caller-supplied detached Ed25519 signature against every key
///         in the ring (specific category keys + <see cref="ContentCategory.Any"/> keys).
///         First match → <see cref="ContentTrustStatus.Verified"/>.
///         No match → <see cref="ContentTrustStatus.SignatureFailed"/> + quarantine.</item>
/// </list>
///
/// <para>
/// Quarantined content is never deleted — it is retained with status
/// <see cref="ContentTrustStatus.Quarantined"/> and excluded from feeds by
/// the consuming layer.
/// </para>
///
/// <para>
/// Node reputation impact is delivered via <see cref="ViolationDetected"/>.
/// Consumers forward this event to the reputation-gossip layer with their own
/// weighted-score policy.  This service does not directly modify the social graph.
/// </para>
/// </summary>
public interface ITrustRingService
{
    // ── Ring management ──────────────────────────────────────────────────────

    /// <summary>
    /// Adds <paramref name="ed25519SubjectPublicKeyInfo"/> (DER-encoded SubjectPublicKeyInfo)
    /// to the ring for <paramref name="category"/>.  Idempotent — adding a key that is
    /// already present is a no-op.
    /// </summary>
    Task AddKeyAsync(byte[] ed25519SubjectPublicKeyInfo, ContentCategory category, string label,
                     CancellationToken ct = default);

    /// <summary>
    /// Removes the key from the ring.  Content already verified by this key retains
    /// its <see cref="ContentTrustStatus.Verified"/> status; future content will fail.
    /// </summary>
    Task RevokeKeyAsync(byte[] ed25519SubjectPublicKeyInfo, ContentCategory category,
                        CancellationToken ct = default);

    /// <summary>
    /// Returns all trusted keys configured for <paramref name="category"/>,
    /// including <see cref="ContentCategory.Any"/> wildcard keys.
    /// </summary>
    IReadOnlyList<TrustedKey> GetRing(ContentCategory category);

    // ── Verification ─────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies assembled content bytes against the trust ring.
    ///
    /// <para>Call this immediately after <c>IContentService.AssembleAsync()</c>,
    /// before delivering content to any consumer.</para>
    /// </summary>
    /// <param name="contentHash">
    /// Declared SHA-256 hex digest (the content-addressed identifier).
    /// </param>
    /// <param name="assembledBytes">Full assembled payload bytes.</param>
    /// <param name="publisherUhid">
    /// UHID of the node that distributed this content.  Used in
    /// <see cref="TrustViolationEvent"/> when verification fails.
    /// </param>
    /// <param name="category">Content category for ring selection.</param>
    /// <param name="detachedSignature">
    /// Optional Ed25519 signature over <paramref name="assembledBytes"/>
    /// (raw 64-byte signature, not DER-wrapped).
    /// Required when the ring for <paramref name="category"/> is non-empty.
    /// </param>
    Task<TrustVerificationResult> VerifyAsync(
        string             contentHash,
        byte[]             assembledBytes,
        string             publisherUhid,
        ContentCategory    category,
        byte[]?            detachedSignature = null,
        CancellationToken  ct               = default);

    // ── Status overlay ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current trust status for a previously verified content hash.
    /// Returns <see cref="ContentTrustStatus.Unknown"/> if the hash has never
    /// been submitted for verification.
    /// </summary>
    ContentTrustStatus GetStatus(string contentHash);

    /// <summary>
    /// Manually quarantines a content hash (e.g. following an out-of-band
    /// fraud report or admin action).
    /// </summary>
    Task QuarantineAsync(string contentHash, string reason, CancellationToken ct = default);

    /// <summary>Returns all content hashes currently in quarantine.</summary>
    IReadOnlyList<string> GetQuarantinedHashes();

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired every time a content block fails attestation.  Subscribe to feed
    /// violation data into a reputation or alerting pipeline.
    /// </summary>
    event EventHandler<TrustViolationEvent>? ViolationDetected;
}
