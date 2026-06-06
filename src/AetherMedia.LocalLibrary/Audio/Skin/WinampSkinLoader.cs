// SPDX-License-Identifier: MIT

using System.IO.Compression;
using System.Text;

namespace AetherMedia.LocalLibrary.Audio.Skin;

/// <summary>
/// Default <see cref="IWinampSkinLoader"/>. Treats a <c>.wsz</c> as a ZIP and
/// extracts:
/// <list type="bullet">
///   <item><description><c>*.bmp</c> sprite sheets (main.bmp, cbuttons.bmp, posbar.bmp, ...) — surfaced as raw bytes for the UI layer to decode.</description></item>
///   <item><description><c>region.txt</c> — non-rectangular window outline definition.</description></item>
///   <item><description><c>viscolor.txt</c> — 24 RGB triplets for the spectrum analyser palette.</description></item>
///   <item><description>Other plain-text config files (pledit.txt, gen.txt, ...) — exposed as <see cref="WinampClassicSkin.ConfigFiles"/>.</description></item>
/// </list>
/// </summary>
public sealed class WinampSkinLoader : IWinampSkinLoader
{
    /// <inheritdoc/>
    public async Task<WinampClassicSkin> LoadAsync(string skinPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(skinPath);
        if (!System.IO.File.Exists(skinPath))
            throw new FileNotFoundException("Skin file not found.", skinPath);

        await using var fs = new FileStream(skinPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return await LoadAsync(fs, Path.GetFileNameWithoutExtension(skinPath), ct).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<WinampClassicSkin> LoadAsync(Stream zipStream, string skinName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(zipStream);
        ArgumentException.ThrowIfNullOrEmpty(skinName);

        var sprites = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var configs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? region = null;
        var viscolor = new List<(byte R, byte G, byte B)>();

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        foreach (var entry in archive.Entries)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(entry.Name)) continue; // directory

            // Skin authors sometimes nest files under a single folder inside the zip.
            var nameWithExt = entry.Name;
            var baseName = Path.GetFileNameWithoutExtension(nameWithExt);
            var ext = Path.GetExtension(nameWithExt).ToLowerInvariant();

            using var s = entry.Open();
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            var bytes = ms.ToArray();

            if (ext == ".bmp")
            {
                sprites[baseName] = bytes;
            }
            else if (ext is ".txt" or ".cur" or ".ini")
            {
                var text = Encoding.UTF8.GetString(bytes);
                configs[nameWithExt] = text;
                if (string.Equals(nameWithExt, "region.txt", StringComparison.OrdinalIgnoreCase))
                    region = text;
                else if (string.Equals(nameWithExt, "viscolor.txt", StringComparison.OrdinalIgnoreCase))
                    viscolor.AddRange(ParseVisColor(text));
            }
        }

        var skin = new WinampClassicSkin(
            Name: skinName,
            Sprites: sprites,
            ConfigFiles: configs,
            RegionDefinition: region,
            VisualizerColors: viscolor);
        return Task.FromResult(skin);
    }

    /// <summary>
    /// Parse the 24 RGB triplets from a Winamp viscolor.txt. Lines look like
    /// <c>72,72,72</c>; comments after <c>//</c> are ignored.
    /// </summary>
    internal static IEnumerable<(byte R, byte G, byte B)> ParseVisColor(string text)
    {
        foreach (var raw in text.Split('\n'))
        {
            var line = raw;
            var slash = line.IndexOf("//", StringComparison.Ordinal);
            if (slash >= 0) line = line[..slash];
            line = line.Trim().TrimEnd(',');
            if (line.Length == 0) continue;
            var parts = line.Split(',');
            if (parts.Length < 3) continue;
            if (byte.TryParse(parts[0].Trim(), out var r)
                && byte.TryParse(parts[1].Trim(), out var g)
                && byte.TryParse(parts[2].Trim(), out var b))
                yield return (r, g, b);
        }
    }
}
