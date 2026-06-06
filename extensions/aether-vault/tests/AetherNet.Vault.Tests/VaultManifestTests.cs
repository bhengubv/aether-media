// SPDX-License-Identifier: MIT
using AetherMedia.Vault.Core;

namespace AetherNet.Vault.Tests;

public sealed class VaultManifestTests
{
    // ── Helper ─────────────────────────────────────────────────────────────

    private static VaultManifest MakeManifest(int k = 10, int m = 4) => new(
        FileId:         Guid.NewGuid(),
        ContentHash:    "abc123",
        EncryptionSalt: new byte[] { 0x01, 0x02 },
        ShardHashes:    Enumerable.Range(0, k + m).Select(i => $"hash-{i}").ToArray(),
        K:              k,
        M:              m,
        CreatedAtUtc:   DateTime.UtcNow,
        SizeBytes:      1024,
        Label:          "test-file");

    private static VaultHealth MakeHealth(int reachable, int k = 10, int m = 4) => new(
        TotalShards:    k + m,
        ReachableShards: reachable,
        IsRecoverable:  reachable >= k,
        RedundancyScore: (k + m) == 0 ? 0.0 : (double)reachable / (k + m));

    // ── Shard count ────────────────────────────────────────────────────────

    [Fact]
    public void Manifest_DefaultKAndM_GivesFourteenTotalShards()
    {
        var manifest = MakeManifest(k: 10, m: 4);

        Assert.Equal(10, manifest.K);
        Assert.Equal(4,  manifest.M);
        Assert.Equal(14, manifest.ShardHashes.Length);
    }

    // ── IsRecoverable ──────────────────────────────────────────────────────

    [Fact]
    public void Health_IsRecoverable_WhenReachableShardsEqualsK()
    {
        var health = MakeHealth(reachable: 10);
        Assert.True(health.IsRecoverable);
    }

    [Fact]
    public void Health_IsRecoverable_WhenReachableShardsExceedsK()
    {
        var health = MakeHealth(reachable: 14);
        Assert.True(health.IsRecoverable);
    }

    [Fact]
    public void Health_IsNotRecoverable_WhenReachableShardsLessThanK()
    {
        var health = MakeHealth(reachable: 9);
        Assert.False(health.IsRecoverable);
    }

    [Fact]
    public void Health_IsNotRecoverable_WhenNoShardsReachable()
    {
        var health = MakeHealth(reachable: 0);
        Assert.False(health.IsRecoverable);
    }

    // ── RedundancyScore ────────────────────────────────────────────────────

    [Fact]
    public void Health_RedundancyScore_IsZeroWhenNoShardsReachable()
    {
        var health = MakeHealth(reachable: 0);
        Assert.Equal(0.0, health.RedundancyScore);
    }

    [Fact]
    public void Health_RedundancyScore_IsOneWhenAllShardsReachable()
    {
        var health = MakeHealth(reachable: 14);
        Assert.Equal(1.0, health.RedundancyScore, precision: 10);
    }

    [Fact]
    public void Health_RedundancyScore_IsPartialWhenSomeShardsReachable()
    {
        // 7 of 14 reachable → 0.5
        var health = MakeHealth(reachable: 7);
        Assert.Equal(0.5, health.RedundancyScore, precision: 10);
    }
}
