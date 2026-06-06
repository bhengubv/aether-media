// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Tags;

/// <summary>
/// Writes <see cref="AudioTags"/> back into a media file's metadata block.
/// Format selection (ID3v1, ID3v2.3/2.4, Vorbis comments, APE, MP4 atoms) is
/// inferred from the file extension by the implementation.
/// </summary>
public interface IAudioTagWriter
{
    /// <summary>Write tags to the file at <paramref name="filePath"/>, preserving any fields not present in <paramref name="tags"/>.</summary>
    Task WriteAsync(string filePath, AudioTags tags, CancellationToken ct = default);
}
