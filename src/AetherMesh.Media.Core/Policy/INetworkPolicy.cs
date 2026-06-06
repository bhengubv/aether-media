// SPDX-License-Identifier: MIT

namespace AetherMesh.Media.Core;

/// <summary>
/// Abstracts live network metering state from the platform.
///
/// Platform implementations:
/// <list type="bullet">
///   <item>Android — <c>ConnectivityManager.isActiveNetworkMetered()</c></item>
///   <item>iOS/macOS — <c>NWPath.isExpensive</c></item>
///   <item>Windows — <c>NetworkInformation.GetInternetConnectionProfile().GetConnectionCost()</c></item>
///   <item>Desktop / test — <see cref="NullNetworkPolicy"/> (always unmetered)</item>
/// </list>
///
/// Register a platform-specific implementation before calling
/// <c>AddFootprintPolicy()</c> to override the null default.
/// </summary>
public interface INetworkPolicy
{
    /// <summary>
    /// <c>true</c> when the active network connection charges per byte
    /// (mobile data, tethered hotspot, etc.).
    /// </summary>
    bool IsMetered { get; }
}
