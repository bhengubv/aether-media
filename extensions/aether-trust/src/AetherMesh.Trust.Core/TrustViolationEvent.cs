// SPDX-License-Identifier: MIT

namespace AetherMesh.Trust.Core;

/// <summary>
/// Raised when a content block fails attestation.  Consumers — such as a
/// reputation-gossip integration — subscribe to
/// <see cref="ITrustRingService.ViolationDetected"/> and apply a weighted
/// reputation penalty to <see cref="PublisherUhid"/>.
///
/// <para>
/// Do NOT use this event to hard-ban a node.  One violation should decrement
/// the publisher's reputation score; the decision to isolate is left to the
/// reputation layer with its own threshold and decay logic.
/// </para>
/// </summary>
/// <param name="ContentHash">Hash of the content that failed verification.</param>
/// <param name="PublisherUhid">UHID of the node that distributed the content.</param>
/// <param name="FailureStatus">The specific failure mode detected.</param>
/// <param name="DetectedAtUtc">Timestamp of detection.</param>
public sealed record TrustViolationEvent(
    string             ContentHash,
    string             PublisherUhid,
    ContentTrustStatus FailureStatus,
    DateTime           DetectedAtUtc);
