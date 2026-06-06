// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary.Audio.Tags;

namespace AetherMedia.LocalLibrary.Audio.Library;

/// <summary>
/// Suggests tag corrections / fills for a file. Modelled after Winamp's
/// <c>Auto-Tag</c> menu — the host applies the returned suggestion only
/// when the user confirms.
/// </summary>
public interface IAutoTagger
{
    /// <summary>
    /// Compute the most likely <see cref="AudioTags"/> for the given file.
    /// Returns null when no high-confidence match is found.
    /// </summary>
    Task<AudioTags?> SuggestAsync(string filePath, AudioTags currentTags, CancellationToken ct = default);
}
