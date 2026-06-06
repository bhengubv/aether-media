// SPDX-License-Identifier: MIT
using System.Text.Json;
using System.Text.Json.Serialization;
using AetherMesh.Market.Core;

namespace AetherMesh.Market.Protocol;

/// <summary>
/// Wire representation of a <see cref="PoVToken"/> for transmission over the
/// Aether mesh.  Packet type discriminator is <c>41</c>.
/// </summary>
public readonly struct PoVTokenPacket
{
    /// <summary>
    /// Mesh packet-type discriminator for Aether Market PoV token frames.
    /// </summary>
    public const int PacketType = MarketProtocolConstants.PoVToken;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Universal host ID of the witnessing device.</summary>
    [JsonPropertyName("witness_uhid")]
    public string WitnessUhid { get; init; }

    /// <summary>Universal host ID of the subject device.</summary>
    [JsonPropertyName("subject_uhid")]
    public string SubjectUhid { get; init; }

    /// <summary>UTC timestamp as Unix seconds.</summary>
    [JsonPropertyName("timestamp_unix")]
    public long TimestampUnix { get; init; }

    /// <summary>Numeric transport code (maps to <see cref="PoVTransport"/>).</summary>
    [JsonPropertyName("transport")]
    public int Transport { get; init; }

    /// <summary>Witness Ed25519 signature as Base64.</summary>
    [JsonPropertyName("witness_sig_b64")]
    public string WitnessSigB64 { get; init; }

    /// <summary>Subject Ed25519 signature as Base64.</summary>
    [JsonPropertyName("subject_sig_b64")]
    public string SubjectSigB64 { get; init; }

    // ── Conversion ─────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="PoVTokenPacket"/> from a domain <see cref="PoVToken"/>.
    /// </summary>
    public static PoVTokenPacket FromToken(PoVToken token) => new()
    {
        WitnessUhid    = token.WitnessUhid,
        SubjectUhid    = token.SubjectUhid,
        TimestampUnix  = new DateTimeOffset(token.TimestampUtc, TimeSpan.Zero).ToUnixTimeSeconds(),
        Transport      = (int)token.TransportUsed,
        WitnessSigB64  = Convert.ToBase64String(token.WitnessSignature),
        SubjectSigB64  = Convert.ToBase64String(token.SubjectSignature),
    };

    /// <summary>
    /// Converts this packet back to a domain <see cref="PoVToken"/>.
    /// </summary>
    public PoVToken ToToken() => new(
        WitnessUhid:      WitnessUhid,
        SubjectUhid:      SubjectUhid,
        TimestampUtc:     DateTimeOffset.FromUnixTimeSeconds(TimestampUnix).UtcDateTime,
        TransportUsed:    (PoVTransport)Transport,
        WitnessSignature: Convert.FromBase64String(WitnessSigB64),
        SubjectSignature: Convert.FromBase64String(SubjectSigB64));

    // ── Serialisation ──────────────────────────────────────────────────────

    /// <summary>Serialises this packet to a UTF-8 JSON byte array.</summary>
    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this, SerializerOptions);

    /// <summary>
    /// Deserialises a <see cref="PoVTokenPacket"/> from a UTF-8 JSON byte span.
    /// </summary>
    /// <exception cref="JsonException">Thrown when the bytes are not valid JSON.</exception>
    public static PoVTokenPacket Deserialize(ReadOnlySpan<byte> utf8Json) =>
        JsonSerializer.Deserialize<PoVTokenPacket>(utf8Json, SerializerOptions);
}
