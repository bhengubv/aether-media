// SPDX-License-Identifier: MIT
using Aether.Vault.ErasureCoding;

namespace Aether.Vault.Tests;

public sealed class ErasureCodingTests
{
    private readonly IErasureCoder _coder = new ReedSolomonEncoder();
    private const int K = 10;
    private const int M = 4;

    // ── Encode ─────────────────────────────────────────────────────────────

    [Fact]
    public void Encode_OneMb_ProducesFourteenShards()
    {
        byte[] data = new byte[1024 * 1024]; // 1 MB
        new Random(42).NextBytes(data);

        byte[][] shards = _coder.Encode(data, K, M);

        Assert.Equal(K + M, shards.Length);
        Assert.All(shards, s => Assert.NotNull(s));
    }

    [Fact]
    public void Encode_OneMb_AllShardsHaveEqualLength()
    {
        byte[] data = new byte[1024 * 1024];
        new Random(42).NextBytes(data);

        byte[][] shards = _coder.Encode(data, K, M);

        int expected = shards[0].Length;
        Assert.All(shards, s => Assert.Equal(expected, s.Length));
    }

    // ── Decode — all shards present ────────────────────────────────────────

    [Fact]
    public void Decode_WithAllFourteenShards_ReturnsOriginalData()
    {
        byte[] data = new byte[1024 * 1024];
        new Random(99).NextBytes(data);

        byte[][] encoded = _coder.Encode(data, K, M);
        // byte[]?[] — array where each slot is either a byte[] or null.
        byte[]?[] nullable = encoded.Select(s => (byte[]?)s).ToArray();

        byte[] recovered = _coder.Decode(nullable, K, M);

        Assert.Equal(data, recovered);
    }

    // ── Decode — 4 shards missing (exactly M) ─────────────────────────────

    [Fact]
    public void Decode_WithTenOfFourteenShards_DropFour_ReturnsOriginalData()
    {
        byte[] data = new byte[1024 * 1024];
        new Random(7).NextBytes(data);

        byte[][] encoded = _coder.Encode(data, K, M);
        byte[]?[] nullable = encoded.Select(s => (byte[]?)s).ToArray();

        // Null out the last 4 shards (the parity shards).
        for (int i = K; i < K + M; i++) nullable[i] = null;

        byte[] recovered = _coder.Decode(nullable, K, M);

        Assert.Equal(data, recovered);
    }

    [Fact]
    public void Decode_WithTenOfFourteenShards_DropFourMixed_ReturnsOriginalData()
    {
        byte[] data = new byte[512 * 1024]; // 512 KB — faster
        new Random(13).NextBytes(data);

        byte[][] encoded = _coder.Encode(data, K, M);
        byte[]?[] nullable = encoded.Select(s => (byte[]?)s).ToArray();

        // Null out 2 data shards and 2 parity shards.
        nullable[3] = null;
        nullable[7] = null;
        nullable[K + 1] = null;
        nullable[K + 3] = null;

        byte[] recovered = _coder.Decode(nullable, K, M);

        Assert.Equal(data, recovered);
    }

    // ── Decode — insufficient shards throws ───────────────────────────────

    [Fact]
    public void Decode_WithNineOfFourteenShards_Throws()
    {
        byte[] data = new byte[1024 * 1024];
        new Random(5).NextBytes(data);

        byte[][] encoded = _coder.Encode(data, K, M);
        byte[]?[] nullable = encoded.Select(s => (byte[]?)s).ToArray();

        // Null out 5 shards, leaving only 9 (one fewer than K=10).
        nullable[0] = null;
        nullable[2] = null;
        nullable[4] = null;
        nullable[6] = null;
        nullable[8] = null;

        Assert.Throws<InvalidOperationException>(() => _coder.Decode(nullable, K, M));
    }
}
