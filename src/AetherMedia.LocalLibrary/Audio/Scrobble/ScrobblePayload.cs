// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AetherMedia.LocalLibrary.Audio.Scrobble;

/// <summary>
/// Wire format for a scrobble event carried inside a DTN bundle. Serialised
/// as small JSON so any other device on the user's mesh (running a future
/// non-.NET scrobbler) can deserialise it without protobuf or Avro.
/// </summary>
public sealed record ScrobblePayload(
    [property: JsonPropertyName("artist")]    string Artist,
    [property: JsonPropertyName("title")]     string Title,
    [property: JsonPropertyName("album")]     string? Album,
    [property: JsonPropertyName("startUtc")]  long   StartedAtUnixMs,
    [property: JsonPropertyName("duration")]  long   DurationMs)
{
    /// <summary>Serialise to a UTF-8 JSON byte buffer.</summary>
    public byte[] ToBytes()
    {
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, this);
        return ms.ToArray();
    }

    /// <summary>Deserialise from a UTF-8 JSON byte buffer. Throws on malformed input.</summary>
    public static ScrobblePayload FromBytes(ReadOnlySpan<byte> bytes)
    {
        var p = JsonSerializer.Deserialize<ScrobblePayload>(bytes)
            ?? throw new FormatException("ScrobblePayload deserialised to null.");
        return p;
    }

    /// <summary>Build a payload from an in-memory <see cref="ScrobbleEvent"/>.</summary>
    public static ScrobblePayload FromEvent(ScrobbleEvent ev)
    {
        ArgumentNullException.ThrowIfNull(ev);
        return new ScrobblePayload(
            Artist: ev.Artist,
            Title: ev.Title,
            Album: ev.Album,
            StartedAtUnixMs: ev.StartedAtUtc.ToUnixTimeMilliseconds(),
            DurationMs: (long)ev.Duration.TotalMilliseconds);
    }

    /// <summary>Convert back to a <see cref="ScrobbleEvent"/>.</summary>
    public ScrobbleEvent ToEvent() =>
        new(Artist, Title, Album,
            DateTimeOffset.FromUnixTimeMilliseconds(StartedAtUnixMs),
            TimeSpan.FromMilliseconds(DurationMs));
}
