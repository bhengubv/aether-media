// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Playlists;

/// <summary>An ordered list of <see cref="PlaylistItem"/> entries with an optional title.</summary>
public sealed record Playlist(
    string? Title,
    IReadOnlyList<PlaylistItem> Items)
{
    /// <summary>Empty playlist with no title.</summary>
    public static Playlist Empty { get; } = new(null, Array.Empty<PlaylistItem>());
}
