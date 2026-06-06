// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Radio;

/// <summary>
/// A directory entry for one streaming radio station. Maps the radio-browser
/// API's JSON shape to a player-friendly contract — the fields a UI actually
/// surfaces, not the raw upstream payload.
/// </summary>
public sealed record RadioStation(
    string Id,
    string Name,
    Uri StreamUrl,
    Uri? Homepage,
    Uri? FaviconUrl,
    string? Country,
    string? CountryCode,
    string? Language,
    IReadOnlyList<string> Tags,
    string? Codec,
    int? BitrateKbps,
    int Votes,
    int ClickCount);
