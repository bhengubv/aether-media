// SPDX-License-Identifier: MIT

namespace AetherMesh.Media.LocalLibrary.Interfaces;

/// <summary>
/// Computes the OpenSubtitles movie hash (the same algorithm used by VLC) for a local
/// video file.  The hash uniquely identifies a specific encode/release and is used to
/// match subtitles precisely before falling back to a title search.
/// </summary>
public interface IMovieHasher
{
    /// <summary>
    /// Computes the 16-character lowercase hex hash for <paramref name="filePath"/>.
    ///
    /// Algorithm: <c>hash = fileSize</c>, then add every <c>int64</c> (little-endian)
    /// in the first 64 KB, then add every <c>int64</c> in the last 64 KB,
    /// all under unchecked 64-bit arithmetic.
    ///
    /// Returns <c>null</c> if:
    /// <list type="bullet">
    ///   <item>the file does not exist,</item>
    ///   <item>the file is smaller than 128 KB (hash undefined for short files), or</item>
    ///   <item>an I/O error occurs.</item>
    /// </list>
    /// </summary>
    Task<string?> ComputeHashAsync(string filePath, CancellationToken ct = default);
}
