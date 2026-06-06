// SPDX-License-Identifier: MIT

using System.Buffers.Binary;
using AetherMesh.Media.LocalLibrary.Interfaces;
using Microsoft.Extensions.Logging;

namespace AetherMesh.Media.LocalLibrary;

/// <summary>
/// Computes the OpenSubtitles movie hash (identical to the VLC algorithm).
///
/// Reference: https://trac.opensubtitles.org/projects/opensubtitles/wiki/HashSourceCodes
///
/// Algorithm (in pseudocode):
/// <code>
///   hash = fileSize (uint64)
///   for each int64 (LE) in first 64 KB: hash += value
///   for each int64 (LE) in last  64 KB: hash += value
///   result = hash.ToString("x16")
/// </code>
/// All arithmetic is unchecked (wraps at 2^64).
/// </summary>
public sealed class MovieHasher : IMovieHasher
{
    private const int ChunkSize = 65_536;          // 64 KB
    private const int LongSize  = sizeof(long);    // 8 bytes
    private const int MinFileSize = ChunkSize * 2; // 128 KB minimum

    private readonly ILogger<MovieHasher> _logger;

    public MovieHasher(ILogger<MovieHasher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<string?> ComputeHashAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("MovieHasher: file not found: {Path}", filePath);
            return null;
        }

        try
        {
            return await Task.Run(() => ComputeCore(filePath), ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MovieHasher: failed to hash {Path}", filePath);
            return null;
        }
    }

    private static string? ComputeCore(string filePath)
    {
        var info = new FileInfo(filePath);
        if (info.Length < MinFileSize)
            return null;

        unchecked
        {
            ulong hash = (ulong)info.Length;

            using var stream = new FileStream(
                filePath, FileMode.Open, FileAccess.Read,
                FileShare.Read, bufferSize: ChunkSize, useAsync: false);

            // ── First 64 KB ──────────────────────────────────────────────
            hash = AccumulateChunk(stream, hash);

            // ── Last 64 KB ───────────────────────────────────────────────
            stream.Seek(-ChunkSize, SeekOrigin.End);
            hash = AccumulateChunk(stream, hash);

            return hash.ToString("x16");
        }
    }

    private static ulong AccumulateChunk(Stream stream, ulong hash)
    {
        Span<byte> buffer = stackalloc byte[ChunkSize];
        int        read   = 0;

        while (read < ChunkSize)
        {
            int n = stream.Read(buffer[read..]);
            if (n == 0) break;
            read += n;
        }

        // Process as many complete int64 values as we have
        int longs = read / LongSize;
        for (int i = 0; i < longs; i++)
        {
            var value = BinaryPrimitives.ReadInt64LittleEndian(
                buffer.Slice(i * LongSize, LongSize));
            unchecked { hash += (ulong)value; }
        }

        return hash;
    }
}
