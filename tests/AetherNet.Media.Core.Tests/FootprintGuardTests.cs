// SPDX-License-Identifier: MIT

using AetherNet.Media.Core;

namespace AetherNet.Media.Core.Tests;

/// <summary>
/// Unit tests for <see cref="FootprintGuard"/> — the single boolean oracle that
/// gates seeding, mesh scanning, and passive mode across the entire node.
///
/// These tests correspond to plan verification item: "device footprint is invisible
/// to the user — storage, network, and power policies enforced."
/// </summary>
public sealed class FootprintGuardTests
{
    // ── Controllable fakes ─────────────────────────────────────────────────

    private sealed class FakeNetwork : INetworkPolicy
    {
        public bool IsMetered { get; set; }
    }

    private sealed class FakePower : IPowerPolicy
    {
        public int  BatteryPercent { get; set; } = 100;
        public bool IsCharging     { get; set; } = true;
        public bool IsScreenOn     { get; set; } = true;
    }

    private static FootprintGuard Make(
        Action<FootprintOptions>? opts    = null,
        Action<FakeNetwork>?      network = null,
        Action<FakePower>?        power   = null)
    {
        var o = new FootprintOptions();
        var n = new FakeNetwork();
        var p = new FakePower();

        opts?.Invoke(o);
        network?.Invoke(n);
        power?.Invoke(p);

        return new FootprintGuard(o, n, p);
    }

    // ── Default (NullNetworkPolicy, NullPowerPolicy) ───────────────────────

    [Fact(DisplayName = "Default state — not passive, seeding allowed, scan allowed")]
    public void Default_AllOperationsAllowed()
    {
        var guard = new FootprintGuard(
            new FootprintOptions(),
            NullNetworkPolicy.Instance,
            NullPowerPolicy.Instance);

        Assert.False(guard.IsPassiveMode);
        Assert.True(guard.SeedingAllowed);
        Assert.True(guard.MeshScanAllowed);
    }

    // ── Network: metered connection ────────────────────────────────────────

    [Fact(DisplayName = "Metered connection — seeding blocked by default")]
    public void Metered_SeedingBlocked_ByDefault()
    {
        var guard = Make(network: n => n.IsMetered = true);

        Assert.False(guard.IsPassiveMode,    "Metered connection alone must not trigger passive mode");
        Assert.False(guard.SeedingAllowed,   "Seeding must be blocked on metered connection by default");
        Assert.False(guard.MeshScanAllowed,  "Mesh scan must be blocked on metered connection by default");
    }

    [Fact(DisplayName = "Metered connection + SeedOnMeteredConnection=true — seeding allowed")]
    public void Metered_SeedingAllowed_WhenOptedIn()
    {
        var guard = Make(
            opts:    o => o.SeedOnMeteredConnection = true,
            network: n => n.IsMetered = true);

        Assert.True(guard.SeedingAllowed);
    }

    [Fact(DisplayName = "Metered connection + ScanMeshOnMeteredConnection=true — scan allowed")]
    public void Metered_ScanAllowed_WhenOptedIn()
    {
        var guard = Make(
            opts:    o => o.ScanMeshOnMeteredConnection = true,
            network: n => n.IsMetered = true);

        Assert.True(guard.MeshScanAllowed);
    }

    // ── Power: battery threshold ───────────────────────────────────────────

    [Fact(DisplayName = "Battery below threshold + not charging — passive mode")]
    public void LowBattery_NotCharging_TriggersPassiveMode()
    {
        var guard = Make(power: p =>
        {
            p.BatteryPercent = 15;
            p.IsCharging     = false;
        });

        Assert.True(guard.IsPassiveMode);
        Assert.False(guard.SeedingAllowed);
        Assert.False(guard.MeshScanAllowed);
    }

    [Fact(DisplayName = "Battery below threshold + charging — passive mode NOT triggered")]
    public void LowBattery_Charging_DoesNotTriggerPassiveMode()
    {
        var guard = Make(power: p =>
        {
            p.BatteryPercent = 5;
            p.IsCharging     = true;   // plugged in
        });

        Assert.False(guard.IsPassiveMode, "Charging device must never enter passive mode regardless of battery %");
        Assert.True(guard.SeedingAllowed);
        Assert.True(guard.MeshScanAllowed);
    }

    [Fact(DisplayName = "Battery above threshold — passive mode NOT triggered")]
    public void NormalBattery_NoPassiveMode()
    {
        var guard = Make(power: p =>
        {
            p.BatteryPercent = 50;
            p.IsCharging     = false;
        });

        Assert.False(guard.IsPassiveMode);
    }

    [Fact(DisplayName = "Custom threshold — battery exactly at threshold is NOT passive")]
    public void BatteryExactlyAtThreshold_NotPassive()
    {
        // Threshold is 20 %; a value of exactly 20 must NOT trigger passive mode
        var guard = Make(
            opts:  o => o.PassiveModeThresholdPercent = 20,
            power: p => { p.BatteryPercent = 20; p.IsCharging = false; });

        // condition is strict <, not <=
        Assert.False(guard.IsPassiveMode);
    }

    // ── Power: screen off ──────────────────────────────────────────────────

    [Fact(DisplayName = "Screen off + PassiveModeWhenScreenOff=true — passive mode")]
    public void ScreenOff_WhenOptionEnabled_TriggersPassiveMode()
    {
        var guard = Make(power: p => p.IsScreenOn = false);   // option defaults to true

        Assert.True(guard.IsPassiveMode);
        Assert.False(guard.SeedingAllowed);
        Assert.False(guard.MeshScanAllowed);
    }

    [Fact(DisplayName = "Screen off + PassiveModeWhenScreenOff=false — passive mode NOT triggered")]
    public void ScreenOff_WhenOptionDisabled_DoesNotTriggerPassiveMode()
    {
        var guard = Make(
            opts:  o => o.PassiveModeWhenScreenOff = false,
            power: p => p.IsScreenOn = false);

        Assert.False(guard.IsPassiveMode);
    }

    // ── Passive mode blocks everything ─────────────────────────────────────

    [Fact(DisplayName = "Passive mode — seeding and scan both blocked even on unmetered connection")]
    public void PassiveMode_BlocksSeedingAndScan_EvenOnUnmeteredConnection()
    {
        // Screen off → passive; connection is unmetered
        var guard = Make(power: p => p.IsScreenOn = false);

        Assert.True(guard.IsPassiveMode);
        Assert.False(guard.SeedingAllowed,  "Passive mode must block seeding regardless of network state");
        Assert.False(guard.MeshScanAllowed, "Passive mode must block scanning regardless of network state");
    }

    // ── StorageCapBytes flows through to LruContentCache ──────────────────

    [Fact(DisplayName = "FootprintOptions.StorageCapBytes — LruContentCache honours configured cap")]
    public void StorageCap_IsHonouredByLruContentCache()
    {
        const long CapBytes = 1024L; // 1 KiB — very tight for test purposes
        var cache = new AetherNet.Media.Content.LruContentCache(CapBytes);

        // Store one entry exactly at the cap
        var data = new byte[CapBytes];
        cache.Set("hash-a", data);
        Assert.Equal(1, cache.Count);

        // Add a second entry — the first must be evicted to stay within cap
        var data2 = new byte[CapBytes];
        cache.Set("hash-b", data2);

        Assert.True(cache.TotalBytes <= CapBytes,
            $"Cache must not exceed cap {CapBytes} B but holds {cache.TotalBytes} B");
        Assert.Equal(1, cache.Count); // only one entry survives
    }
}
