// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Sync;

/// <summary>
/// A portable music device that exposes a filesystem mount point — USB
/// drives, SD cards, Android-MTP folders, classic iPod disk-mode mounts,
/// or any path the user designates as a sync target.
/// </summary>
/// <param name="Id">Stable identifier (volume label, device serial, etc.).</param>
/// <param name="Label">Human-readable name.</param>
/// <param name="MountPath">Root filesystem path where files are copied.</param>
/// <param name="FreeSpaceBytes">Bytes available, or null when not measurable.</param>
/// <param name="CapacityBytes">Total capacity, or null when not measurable.</param>
public sealed record PortableDevice(
    string Id,
    string Label,
    string MountPath,
    long? FreeSpaceBytes,
    long? CapacityBytes);
