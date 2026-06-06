// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Tags;

/// <summary>Reads tags + ReplayGain values out of an audio file.</summary>
public interface IAudioTagReader
{
    /// <summary>Read tags from the given file path.</summary>
    Task<AudioTags?> ReadAsync(string filePath, CancellationToken ct = default);

    /// <summary>Read tags from an in-memory stream (already positioned at file start).</summary>
    Task<AudioTags?> ReadAsync(Stream stream, CancellationToken ct = default);
}
