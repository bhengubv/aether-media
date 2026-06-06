// SPDX-License-Identifier: MIT
using System.Text.Json.Serialization;

namespace AetherNet.Market.Core;

/// <summary>
/// A cryptographically signed token proving that two devices were physically
/// co-located via a short-range transport at a specific moment in time.
/// Both parties sign the same canonical payload so neither can forge a
/// co-location event without the other's private key.
/// </summary>
/// <param name="WitnessUhid">Universal host ID of the witnessing device (the one that issued the token).</param>
/// <param name="SubjectUhid">Universal host ID of the device whose proximity is being attested.</param>
/// <param name="TimestampUtc">UTC timestamp at which the proximity event was observed.</param>
/// <param name="TransportUsed">Short-range transport protocol used for the proximity handshake.</param>
/// <param name="WitnessSignature">Ed25519 signature of the canonical payload produced by the witness.</param>
/// <param name="SubjectSignature">Ed25519 signature of the canonical payload produced by the subject.</param>
public sealed record PoVToken(
    [property: JsonPropertyName("witness_uhid")]        string       WitnessUhid,
    [property: JsonPropertyName("subject_uhid")]        string       SubjectUhid,
    [property: JsonPropertyName("timestamp_utc")]       DateTime     TimestampUtc,
    [property: JsonPropertyName("transport_used")]      PoVTransport TransportUsed,
    [property: JsonPropertyName("witness_signature")]   byte[]       WitnessSignature,
    [property: JsonPropertyName("subject_signature")]   byte[]       SubjectSignature);
