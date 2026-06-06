// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Cd;

/// <summary>A data-CD burn job (MP3 / FLAC / mixed-media CD).</summary>
/// <param name="RecorderId">Recorder ID from <see cref="IDataCdBurner.EnumerateRecorders"/>.</param>
/// <param name="VolumeLabel">Disc label written to the ISO 9660 / UDF volume.</param>
/// <param name="Files">Source paths to include. Folder layout under the disc root is preserved relative to the longest common parent.</param>
public sealed record DataCdRequest(
    string RecorderId,
    string VolumeLabel,
    IReadOnlyList<string> Files);
