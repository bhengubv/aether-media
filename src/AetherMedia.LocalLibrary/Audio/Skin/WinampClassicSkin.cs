// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Skin;

/// <summary>
/// A parsed classic-Winamp <c>.wsz</c> skin (which is just a ZIP of BMPs
/// and a couple of plain-text config files). The model is renderer-agnostic
/// — Avalonia / SkiaSharp / WinUI shells consume the sprite atlas and the
/// region map without depending on a specific GPU surface.
/// </summary>
public sealed record WinampClassicSkin(
    string Name,
    IReadOnlyDictionary<string, byte[]> Sprites,
    IReadOnlyDictionary<string, string> ConfigFiles,
    string? RegionDefinition,
    IReadOnlyList<(byte R, byte G, byte B)> VisualizerColors)
{
    /// <summary>Try to get a sprite bitmap by its (case-insensitive) name without extension.</summary>
    public byte[]? TryGetSprite(string nameWithoutExtension)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameWithoutExtension);
        return Sprites.TryGetValue(nameWithoutExtension, out var bytes) ? bytes : null;
    }
}
