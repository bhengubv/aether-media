// SPDX-License-Identifier: MIT

namespace AetherNet.Trust.Core;

/// <summary>
/// The outcome of a single content-block verification pass.
/// </summary>
/// <param name="ContentHash">The SHA-256 hex digest that was checked.</param>
/// <param name="Status">Resulting trust status.</param>
/// <param name="FailureReason">
/// Human-readable explanation when <paramref name="Status"/> is
/// <see cref="ContentTrustStatus.HashMismatch"/> or
/// <see cref="ContentTrustStatus.SignatureFailed"/>; <c>null</c> on success.
/// </param>
/// <param name="VerifiedAtUtc">When the check was performed.</param>
public sealed record TrustVerificationResult(
    string              ContentHash,
    ContentTrustStatus  Status,
    string?             FailureReason,
    DateTime            VerifiedAtUtc)
{
    /// <summary><c>true</c> when no cryptographic failure was detected.</summary>
    public bool IsClean =>
        Status is ContentTrustStatus.Verified or ContentTrustStatus.NoRingRequired;
}
