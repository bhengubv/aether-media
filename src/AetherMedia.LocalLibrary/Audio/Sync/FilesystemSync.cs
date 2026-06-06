// SPDX-License-Identifier: MIT

namespace AetherMedia.LocalLibrary.Audio.Sync;

/// <summary>
/// Filesystem-based <see cref="IPortableDeviceSync"/>. Treats any folder as a
/// "device" — covers USB sticks, SD cards, disk-mode iPods, and Android MTP
/// mount points equally. Files inside <see cref="SyncFolderName"/> on the
/// device that are not in the source set are deleted; new files are copied
/// preserving relative folder layout.
///
/// <para>
/// Native MTP / Apple Mobile Device Service drivers belong in the desktop
/// shell, not in the audio library — they pull in platform-specific COM /
/// IOKit dependencies. The filesystem path is the universal denominator.
/// </para>
/// </summary>
public sealed class FilesystemSync : IPortableDeviceSync
{
    /// <summary>Subfolder created on the device for synced music.</summary>
    public string SyncFolderName { get; init; } = "Music";

    /// <summary>
    /// Optional explicit device list — when null, the implementation enumerates
    /// fixed and removable drives. Useful in tests + on systems where the
    /// user explicitly configures sync targets.
    /// </summary>
    public IReadOnlyList<PortableDevice>? ConfiguredDevices { get; init; }

    /// <inheritdoc/>
    public Task<IReadOnlyList<PortableDevice>> DiscoverDevicesAsync(CancellationToken ct = default)
    {
        if (ConfiguredDevices is not null)
            return Task.FromResult(ConfiguredDevices);

        var found = new List<PortableDevice>();
        foreach (var d in DriveInfo.GetDrives())
        {
            try
            {
                if (!d.IsReady) continue;
                // Removable drives are the obvious candidates; allow fixed too so
                // the user can target an external SSD.
                if (d.DriveType is not (DriveType.Removable or DriveType.Fixed)) continue;
                found.Add(new PortableDevice(
                    Id: d.Name,
                    Label: string.IsNullOrWhiteSpace(d.VolumeLabel) ? d.Name : d.VolumeLabel,
                    MountPath: d.RootDirectory.FullName,
                    FreeSpaceBytes: d.AvailableFreeSpace,
                    CapacityBytes: d.TotalSize));
            }
            catch (IOException) { /* drive unmounted while iterating */ }
            catch (UnauthorizedAccessException) { }
        }
        return Task.FromResult<IReadOnlyList<PortableDevice>>(found);
    }

    /// <inheritdoc/>
    public Task<SyncPlan> PlanAsync(
        PortableDevice device,
        IReadOnlyList<string> sourceFiles,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(sourceFiles);

        var syncRoot = Path.Combine(device.MountPath, SyncFolderName);

        // Map source → relative path on device.
        var wanted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        long bytes = 0;
        foreach (var src in sourceFiles)
        {
            ct.ThrowIfCancellationRequested();
            if (!System.IO.File.Exists(src)) continue;
            var rel = SanitiseName(Path.GetFileName(src));
            var dest = Path.Combine(syncRoot, rel);
            wanted[dest] = src;

            // Bytes only counted for files that are missing or stale on device.
            if (NeedsCopy(src, dest)) bytes += new FileInfo(src).Length;
        }

        var existing = Directory.Exists(syncRoot)
            ? Directory.EnumerateFiles(syncRoot, "*", SearchOption.AllDirectories).ToList()
            : new List<string>();

        var toCopy = wanted.Keys.Where(d => NeedsCopy(wanted[d], d)).ToList();
        var toDelete = existing.Where(e => !wanted.ContainsKey(e)).ToList();

        return Task.FromResult(new SyncPlan(toCopy, toDelete, bytes));
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(
        PortableDevice device,
        IReadOnlyList<string> sourceFiles,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(sourceFiles);

        var plan = await PlanAsync(device, sourceFiles, ct).ConfigureAwait(false);
        var syncRoot = Path.Combine(device.MountPath, SyncFolderName);
        Directory.CreateDirectory(syncRoot);

        var work = plan.ToCopy.Count + plan.ToDelete.Count;
        var done = 0;

        // Deletions first — free space before copies.
        foreach (var f in plan.ToDelete)
        {
            ct.ThrowIfCancellationRequested();
            try { System.IO.File.Delete(f); } catch (IOException) { }
            progress?.Report((double)++done / Math.Max(1, work));
        }

        foreach (var src in sourceFiles)
        {
            ct.ThrowIfCancellationRequested();
            if (!System.IO.File.Exists(src)) continue;
            var dest = Path.Combine(syncRoot, SanitiseName(Path.GetFileName(src)));
            if (!NeedsCopy(src, dest)) continue;

            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            await using (var s = System.IO.File.OpenRead(src))
            await using (var d = System.IO.File.Create(dest))
                await s.CopyToAsync(d, ct).ConfigureAwait(false);
            progress?.Report((double)++done / Math.Max(1, work));
        }
    }

    private static bool NeedsCopy(string src, string dest)
    {
        if (!System.IO.File.Exists(dest)) return true;
        var s = new FileInfo(src);
        var d = new FileInfo(dest);
        return s.Length != d.Length || s.LastWriteTimeUtc > d.LastWriteTimeUtc;
    }

    /// <summary>Strip filesystem-illegal characters for FAT32 compatibility.</summary>
    private static string SanitiseName(string name)
    {
        Span<char> buf = stackalloc char[name.Length];
        for (var i = 0; i < name.Length; i++)
            buf[i] = "*?<>|:\"\\/".Contains(name[i]) ? '_' : name[i];
        return new string(buf);
    }
}
