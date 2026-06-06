// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Skin;

/// <summary>Loads a classic-Winamp <c>.wsz</c> skin file into a <see cref="WinampClassicSkin"/>.</summary>
public interface IWinampSkinLoader
{
    /// <summary>Load from a .wsz file on disk.</summary>
    Task<WinampClassicSkin> LoadAsync(string skinPath, CancellationToken ct = default);

    /// <summary>Load from an in-memory .wsz / .zip stream.</summary>
    Task<WinampClassicSkin> LoadAsync(Stream zipStream, string skinName, CancellationToken ct = default);
}
