// SPDX-License-Identifier: MIT

namespace Aether.Media.Core;

/// <summary>
/// Tunable limits that keep the Aether Media node invisible to the user.
///
/// Three axes:
///   1. <b>Storage</b>   — hard cap on the LRU content cache (default 500 MiB).
///   2. <b>Network</b>   — opt-in gates for seeding and mesh scanning on metered connections.
///   3. <b>Power</b>     — automatic passive mode below a battery threshold or when screen is off.
///
/// Platform implementations of <see cref="INetworkPolicy"/> and <see cref="IPowerPolicy"/>
/// feed live device state into <see cref="FootprintGuard"/>, which is the single call-site
/// used by every subsystem that might drain battery or consume data unexpectedly.
/// </summary>
public sealed class FootprintOptions
{
    // ── Storage ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Maximum bytes the LRU content cache may hold before evicting least-recently-used entries.
    /// Default: 500 MiB.  Set to 0 to use the implementation's own default.
    /// </summary>
    public long StorageCapBytes { get; set; } = 500L * 1024 * 1024;

    // ── Network ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Allow P2P chunk seeding when the active network connection is metered (mobile data).
    /// Default: <c>false</c> — users are never silently billed for sharing content with peers.
    /// </summary>
    public bool SeedOnMeteredConnection { get; set; } = false;

    /// <summary>
    /// Allow active mesh scanning (BLE, Wi-Fi Direct, NearLink discovery beacons) when
    /// the connection is metered.  Default: <c>false</c>.
    /// </summary>
    public bool ScanMeshOnMeteredConnection { get; set; } = false;

    // ── Power ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Battery percentage below which the node drops to passive-only mode (no active
    /// scanning, no seeding).  Default: 20 %.
    /// </summary>
    public int PassiveModeThresholdPercent { get; set; } = 20;

    /// <summary>
    /// Drop to passive mode when the device screen is off.  Default: <c>true</c>.
    /// Keeps background battery drain negligible while the device is in a pocket or bag.
    /// </summary>
    public bool PassiveModeWhenScreenOff { get; set; } = true;
}
