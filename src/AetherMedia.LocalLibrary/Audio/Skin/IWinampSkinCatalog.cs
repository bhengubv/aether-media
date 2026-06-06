// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Skin;

/// <summary>One catalogue entry from an online Winamp skin directory.</summary>
public sealed record WinampSkinCatalogEntry(
    string Id,
    string Name,
    Uri DownloadUrl,
    Uri? ScreenshotUrl,
    string? Author,
    bool IsNsfw);

/// <summary>
/// Browseable directory of classic Winamp skins. Default impl
/// <see cref="WebampSkinMuseumClient"/> hits the open Webamp Skin Museum
/// API (~80k preserved skins from the classic Winamp Skin Archive).
/// </summary>
public interface IWinampSkinCatalog
{
    Task<IReadOnlyList<WinampSkinCatalogEntry>> SearchAsync(string? query, int limit = 50, int offset = 0, bool includeNsfw = false, CancellationToken ct = default);
    Task<Stream> OpenSkinAsync(WinampSkinCatalogEntry entry, CancellationToken ct = default);
}
