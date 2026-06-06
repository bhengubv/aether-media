// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Sync;

/// <summary>
/// One-way sync of a curated subset of the library onto a portable device,
/// matching the behaviour Winamp shipped against iPods and generic USB
/// players. Bidirectional sync is intentionally out of scope.
/// </summary>
public interface IPortableDeviceSync
{
    /// <summary>Enumerate filesystem-mounted devices currently visible.</summary>
    Task<IReadOnlyList<PortableDevice>> DiscoverDevicesAsync(CancellationToken ct = default);

    /// <summary>
    /// Plan a sync — compare <paramref name="sourceFiles"/> against the device
    /// without copying anything. Surfaces what would be added / removed and
    /// the byte volume so the UI can confirm with the user.
    /// </summary>
    Task<SyncPlan> PlanAsync(
        PortableDevice device,
        IReadOnlyList<string> sourceFiles,
        CancellationToken ct = default);

    /// <summary>Execute a previously-planned sync.</summary>
    Task ExecuteAsync(
        PortableDevice device,
        IReadOnlyList<string> sourceFiles,
        IProgress<double>? progress = null,
        CancellationToken ct = default);
}
