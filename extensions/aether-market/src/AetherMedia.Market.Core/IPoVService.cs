// SPDX-License-Identifier: MIT
namespace AetherMedia.Market.Core;

/// <summary>
/// Provides operations for issuing, accepting, verifying, and scoring
/// Proof-of-Vicinity tokens on the Aether mesh.
/// </summary>
public interface IPoVService
{
    // ── Observable ─────────────────────────────────────────────────────────

    /// <summary>
    /// Hot observable that emits each <see cref="PoVToken"/> as it arrives
    /// from a nearby mesh node.
    /// </summary>
    IObservable<PoVToken> TokenReceived { get; }

    // ── Mutations ──────────────────────────────────────────────────────────

    /// <summary>
    /// Initiates a Proof-of-Vicinity handshake with the device identified by
    /// <paramref name="subjectAetherNetTag"/> and issues a signed
    /// <see cref="PoVToken"/> on successful proximity confirmation.
    /// </summary>
    /// <param name="subjectAetherNetTag">The @tag of the device to witness.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly issued <see cref="PoVToken"/>.</returns>
    Task<PoVToken> IssueTokenAsync(string subjectAetherNetTag, CancellationToken ct = default);

    /// <summary>
    /// Accepts an inbound <see cref="PoVToken"/> from a witnessing node,
    /// verifies both signatures, and incorporates it into the local PoV score.
    /// </summary>
    /// <param name="token">The token to accept.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AcceptTokenAsync(PoVToken token, CancellationToken ct = default);

    /// <summary>
    /// Reports a node that defected from a confirmed trade (e.g. did not
    /// deliver goods after escrow release).  The evidence string should be
    /// a serialised <see cref="TradeEscrow"/> or other signed artefact.
    /// </summary>
    /// <param name="uhid">Universal host ID of the defecting node.</param>
    /// <param name="evidence">Signed evidence of defection.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ReportDefectionAsync(string uhid, string evidence, CancellationToken ct = default);

    // ── Queries ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current decay-adjusted <see cref="PoVScore"/> for the node
    /// identified by <paramref name="uhid"/>.
    /// </summary>
    /// <param name="uhid">Universal host ID of the node to query.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<PoVScore> GetScoreAsync(string uhid, CancellationToken ct = default);

    /// <summary>
    /// Verifies the cryptographic signatures on <paramref name="token"/> and
    /// returns <see langword="true"/> when both the witness and subject
    /// signatures are valid.
    /// </summary>
    /// <param name="token">The token to verify.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> VerifyTokenAsync(PoVToken token, CancellationToken ct = default);
}
