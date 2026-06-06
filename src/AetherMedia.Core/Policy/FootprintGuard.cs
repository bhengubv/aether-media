// SPDX-License-Identifier: MIT

namespace AetherMedia.Core;

/// <summary>
/// Single call-site for every subsystem that might drain battery or consume data
/// unexpectedly.  Combines <see cref="FootprintOptions"/> configuration with live
/// state from <see cref="INetworkPolicy"/> and <see cref="IPowerPolicy"/> to give
/// a yes/no answer for the three high-impact operations:
///
/// <list type="bullet">
///   <item><see cref="IsPassiveMode"/> — should the node stop all active radio work?</item>
///   <item><see cref="SeedingAllowed"/>  — may this node push content chunks to peers?</item>
///   <item><see cref="MeshScanAllowed"/> — may this node broadcast discovery beacons?</item>
/// </list>
///
/// Inject this into any service that seeds, scans, or gossips, and gate the
/// operation behind the relevant property.  The decisions are deliberately cheap
/// (two integer comparisons + two bool checks) — call them on every packet if needed.
/// </summary>
public sealed class FootprintGuard
{
    private readonly FootprintOptions _options;
    private readonly INetworkPolicy _network;
    private readonly IPowerPolicy _power;

    public FootprintGuard(
        FootprintOptions options,
        INetworkPolicy network,
        IPowerPolicy power)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _network = network ?? throw new ArgumentNullException(nameof(network));
        _power   = power   ?? throw new ArgumentNullException(nameof(power));
    }

    // ── Core decisions ─────────────────────────────────────────────────────────

    /// <summary>
    /// <c>true</c> when the node must stop all active radio operations (scanning,
    /// seeding, gossiping) to preserve the user's battery.
    ///
    /// Triggers when:
    /// <list type="bullet">
    ///   <item>Battery is below <see cref="FootprintOptions.PassiveModeThresholdPercent"/>
    ///         <b>and</b> the device is not charging.</item>
    ///   <item><see cref="FootprintOptions.PassiveModeWhenScreenOff"/> is <c>true</c>
    ///         and the screen is currently off.</item>
    /// </list>
    /// </summary>
    public bool IsPassiveMode =>
        (!_power.IsCharging && _power.BatteryPercent < _options.PassiveModeThresholdPercent)
        || (_options.PassiveModeWhenScreenOff && !_power.IsScreenOn);

    /// <summary>
    /// <c>true</c> when this node may push content chunks to requesting peers.
    ///
    /// Blocked when:
    /// <list type="bullet">
    ///   <item><see cref="IsPassiveMode"/> is <c>true</c>.</item>
    ///   <item>The connection is metered <b>and</b>
    ///         <see cref="FootprintOptions.SeedOnMeteredConnection"/> is <c>false</c>.</item>
    /// </list>
    /// </summary>
    public bool SeedingAllowed =>
        !IsPassiveMode
        && (_options.SeedOnMeteredConnection || !_network.IsMetered);

    /// <summary>
    /// <c>true</c> when this node may broadcast active discovery beacons
    /// (BLE advertisements, Wi-Fi Direct probes, NearLink announcements).
    ///
    /// Blocked when:
    /// <list type="bullet">
    ///   <item><see cref="IsPassiveMode"/> is <c>true</c>.</item>
    ///   <item>The connection is metered <b>and</b>
    ///         <see cref="FootprintOptions.ScanMeshOnMeteredConnection"/> is <c>false</c>.</item>
    /// </list>
    /// </summary>
    public bool MeshScanAllowed =>
        !IsPassiveMode
        && (_options.ScanMeshOnMeteredConnection || !_network.IsMetered);
}
