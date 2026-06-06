// SPDX-License-Identifier: MIT

using AetherMedia.LocalLibrary;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherMedia.LocalLibrary.Tests;

public sealed class MovieHasherTests
{
    private readonly MovieHasher _hasher =
        new(NullLogger<MovieHasher>.Instance);

    // ── Null / edge cases ──────────────────────────────────────────────────

    [Fact]
    public async Task ComputeHashAsync_ReturnsNull_WhenFileDoesNotExist()
    {
        var result = await _hasher.ComputeHashAsync("/nonexistent/video.mkv");
        Assert.Null(result);
    }

    [Fact]
    public async Task ComputeHashAsync_ReturnsNull_WhenFileSmallerThan128KB()
    {
        var path = CreateTempFile(sizeBytes: 1024);  // 1 KB

        try
        {
            var result = await _hasher.ComputeHashAsync(path);
            Assert.Null(result);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ── Hash format ────────────────────────────────────────────────────────

    [Fact]
    public async Task ComputeHashAsync_Returns16CharLowercaseHex()
    {
        var path = CreateTempFile(sizeBytes: 200 * 1024);  // 200 KB

        try
        {
            var hash = await _hasher.ComputeHashAsync(path);

            Assert.NotNull(hash);
            Assert.Equal(16, hash!.Length);
            Assert.All(hash, c => Assert.True(
                (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'),
                $"Non-hex character: {c}"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ── Determinism ────────────────────────────────────────────────────────

    [Fact]
    public async Task ComputeHashAsync_IsDeterministic()
    {
        var path = CreateTempFile(sizeBytes: 256 * 1024);  // 256 KB

        try
        {
            var hash1 = await _hasher.ComputeHashAsync(path);
            var hash2 = await _hasher.ComputeHashAsync(path);

            Assert.Equal(hash1, hash2);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ── Sensitivity ────────────────────────────────────────────────────────

    [Fact]
    public async Task ComputeHashAsync_DiffersForDifferentFiles()
    {
        // Two files of the same size but different content must produce different hashes
        var path1 = CreateTempFile(sizeBytes: 200 * 1024, seed: 0);
        var path2 = CreateTempFile(sizeBytes: 200 * 1024, seed: 99);

        try
        {
            var hash1 = await _hasher.ComputeHashAsync(path1);
            var hash2 = await _hasher.ComputeHashAsync(path2);

            Assert.NotNull(hash1);
            Assert.NotNull(hash2);
            Assert.NotEqual(hash1, hash2);
        }
        finally
        {
            File.Delete(path1);
            File.Delete(path2);
        }
    }

    // ── Known-value test ───────────────────────────────────────────────────
    // Validates against the reference Python implementation output.

    [Fact]
    public async Task ComputeHashAsync_MatchesReferenceAlgorithm()
    {
        // Build a 131 072-byte (128 KB) file with all-zero content
        // Reference hash (computed manually):
        //   fileSize = 131072 = 0x0000000000020000
        //   first 64 KB = 8192 × 0 → contribution = 0
        //   last  64 KB = 8192 × 0 → contribution = 0
        //   hash = 0x0000000000020000 → "0000000000020000"
        var path = CreateTempFile(sizeBytes: 128 * 1024, allZero: true);

        try
        {
            var hash = await _hasher.ComputeHashAsync(path);
            Assert.Equal("0000000000020000", hash);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static string CreateTempFile(int sizeBytes, int seed = 42, bool allZero = false)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test-hash-{Guid.NewGuid():N}.bin");
        var data = new byte[sizeBytes];

        if (!allZero)
        {
            var rng = new Random(seed);
            rng.NextBytes(data);
        }

        File.WriteAllBytes(path, data);
        return path;
    }
}
