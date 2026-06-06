// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Radio;

/// <summary>
/// Search criteria for a radio directory query. All fields are optional —
/// unset properties are not sent to the directory.
/// </summary>
public sealed record RadioStationQuery(
    string? NameContains = null,
    string? CountryCode = null,
    string? Language = null,
    string? Tag = null,
    string? Codec = null,
    int? MinBitrateKbps = null,
    int Limit = 50,
    int Offset = 0,
    RadioStationOrder Order = RadioStationOrder.Votes,
    bool Reverse = true);

/// <summary>Sort dimension for directory results.</summary>
public enum RadioStationOrder
{
    Name,
    Votes,
    ClickCount,
    Bitrate,
    Country,
}
