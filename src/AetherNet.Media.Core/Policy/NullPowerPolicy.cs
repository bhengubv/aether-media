// SPDX-License-Identifier: MIT

namespace AetherNet.Media.Core;

/// <summary>
/// <see cref="IPowerPolicy"/> for desktop platforms and unit tests where there is
/// no battery or the screen state is irrelevant.  Always reports 100 % charge,
/// charging, and screen on — so passive mode is never triggered by default on
/// non-mobile targets.
/// </summary>
public sealed class NullPowerPolicy : IPowerPolicy
{
    /// <summary>Shared singleton — allocation-free.</summary>
    public static readonly NullPowerPolicy Instance = new();

    /// <inheritdoc/>
    /// <remarks>Always 100 %.</remarks>
    public int BatteryPercent => 100;

    /// <inheritdoc/>
    /// <remarks>Always <c>true</c>.</remarks>
    public bool IsCharging => true;

    /// <inheritdoc/>
    /// <remarks>Always <c>true</c>.</remarks>
    public bool IsScreenOn => true;
}
