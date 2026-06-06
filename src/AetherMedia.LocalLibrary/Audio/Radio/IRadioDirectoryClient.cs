// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Radio;

/// <summary>
/// Browseable catalogue of internet radio stations — Winamp's "SHOUTcast
/// Radio" tab replacement. The original directory was retired by SHOUTcast
/// Inc. in 2023; this interface is provider-agnostic so the default impl
/// can target whatever curated source is alive at the time.
/// </summary>
public interface IRadioDirectoryClient
{
    /// <summary>Search the directory.</summary>
    Task<IReadOnlyList<RadioStation>> SearchAsync(RadioStationQuery query, CancellationToken ct = default);

    /// <summary>Top stations by recent click count — the "popular" tab.</summary>
    Task<IReadOnlyList<RadioStation>> TopClickedAsync(int limit = 50, CancellationToken ct = default);

    /// <summary>Resolve a station by its stable ID.</summary>
    Task<RadioStation?> GetByIdAsync(string id, CancellationToken ct = default);

    /// <summary>Increment the directory's click counter for the station — analytics signal upstream.</summary>
    Task RegisterClickAsync(string id, CancellationToken ct = default);
}
