// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Radio;

/// <summary>Metadata announced by a Shoutcast/Icecast HTTP stream.</summary>
/// <param name="StationName">Value of the icy-name HTTP header (or null).</param>
/// <param name="Genre">Value of the icy-genre HTTP header.</param>
/// <param name="BitrateKbps">Value of the icy-br HTTP header.</param>
/// <param name="ContentType">MIME type of the audio payload.</param>
/// <param name="MetadataIntervalBytes">Bytes between inline icy metadata blocks (icy-metaint), or 0 if not announced.</param>
/// <param name="CurrentTitle">The most recently announced StreamTitle (updated as the stream plays).</param>
public sealed record ShoutcastStreamMetadata(
    string? StationName,
    string? Genre,
    int? BitrateKbps,
    string? ContentType,
    int MetadataIntervalBytes,
    string? CurrentTitle);
