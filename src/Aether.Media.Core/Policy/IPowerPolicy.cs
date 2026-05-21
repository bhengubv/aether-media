// SPDX-License-Identifier: MIT

namespace Aether.Media.Core;

/// <summary>
/// Abstracts live battery and screen state from the platform.
///
/// Platform implementations:
/// <list type="bullet">
///   <item>Android — <c>BatteryManager</c> + <c>PowerManager.isInteractive()</c></item>
///   <item>iOS/macOS — <c>UIDevice.batteryLevel</c> + <c>UIApplication.isIdleTimerDisabled</c></item>
///   <item>Windows — <c>SystemInformation.PowerStatus</c></item>
///   <item>Desktop / test — <see cref="NullPowerPolicy"/> (always charging, screen on)</item>
/// </list>
///
/// Register a platform-specific implementation before calling
/// <c>AddFootprintPolicy()</c> to override the null default.
/// </summary>
public interface IPowerPolicy
{
    /// <summary>Current battery charge 0–100. Returns 100 when unknown or plugged in.</summary>
    int BatteryPercent { get; }

    /// <summary><c>true</c> when the device is connected to a charger (or has no battery).</summary>
    bool IsCharging { get; }

    /// <summary><c>true</c> when the screen (or equivalent display) is on and interactive.</summary>
    bool IsScreenOn { get; }
}
