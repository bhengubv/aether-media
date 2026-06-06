// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace AetherMedia.LocalLibrary.Audio.Cd;

/// <summary>
/// Windows <see cref="ICdRipper"/> implementation. Uses
/// <c>CreateFile("\\\\.\\D:")</c> to open the CD device and
/// <c>DeviceIoControl</c> with <c>IOCTL_CDROM_READ_TOC</c> /
/// <c>IOCTL_CDROM_RAW_READ</c> to read the TOC and raw audio sectors.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsCdRipper : ICdRipper
{
    private const uint GENERIC_READ           = 0x80000000;
    private const uint FILE_SHARE_READ        = 0x00000001;
    private const uint FILE_SHARE_WRITE       = 0x00000002;
    private const uint OPEN_EXISTING          = 3;
    private const uint IOCTL_CDROM_READ_TOC   = 0x00024000;
    private const uint IOCTL_CDROM_RAW_READ   = 0x0002403E;
    private const int  CD_RAW_SECTOR_SIZE     = 2352;
    private const int  SECTORS_PER_READ       = 27;       // 27*2352 ≈ 64KB — friendly for I/O

    /// <inheritdoc/>
    public IReadOnlyList<string> EnumerateDrives()
    {
        if (!OperatingSystem.IsWindows()) return Array.Empty<string>();
        var list = new List<string>();
        foreach (var d in DriveInfo.GetDrives())
        {
            try { if (d.DriveType == DriveType.CDRom) list.Add(d.Name.TrimEnd('\\')); }
            catch (IOException) { /* drive not ready */ }
        }
        return list;
    }

    /// <inheritdoc/>
    public Task<CdToc> ReadTocAsync(string drivePath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(drivePath);
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("WindowsCdRipper is Windows-only.");

        using var handle = OpenDevice(drivePath);
        var toc = new CDROM_TOC();
        var size = Marshal.SizeOf<CDROM_TOC>();
        if (!DeviceIoControl(handle, IOCTL_CDROM_READ_TOC, IntPtr.Zero, 0, ref toc, (uint)size, out _, IntPtr.Zero))
            throw new IOException($"IOCTL_CDROM_READ_TOC failed: {Marshal.GetLastWin32Error()}");

        var tracks = new List<CdTrack>();
        // toc.FirstTrack/LastTrack are 1-based; toc.TrackData has FirstTrack..LastTrack+lead-out entries.
        var first = toc.FirstTrack;
        var last  = toc.LastTrack;
        for (var i = first; i <= last; i++)
        {
            var idx = i - first;
            var startLba = MsfToLba(toc.TrackData[idx].Address);
            var nextLba  = MsfToLba(toc.TrackData[idx + 1].Address);
            var isAudio  = (toc.TrackData[idx].Control & 0x04) == 0; // data bit clear → audio
            tracks.Add(new CdTrack(
                Number: i,
                StartLba: startLba,
                SectorCount: nextLba - startLba,
                IsAudio: isAudio));
        }
        return Task.FromResult(new CdToc(tracks));
    }

    /// <inheritdoc/>
    public async Task RipTrackAsync(
        string drivePath,
        CdTrack track,
        Stream destination,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(drivePath);
        ArgumentNullException.ThrowIfNull(track);
        ArgumentNullException.ThrowIfNull(destination);
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("WindowsCdRipper is Windows-only.");
        if (!track.IsAudio)
            throw new InvalidOperationException("Track is not an audio track.");

        using var handle = OpenDevice(drivePath);
        var buffer = new byte[CD_RAW_SECTOR_SIZE * SECTORS_PER_READ];
        var remaining = track.SectorCount;
        var lba = track.StartLba;
        var total = track.SectorCount;
        var done = 0;

        while (remaining > 0)
        {
            ct.ThrowIfCancellationRequested();
            var batch = Math.Min(SECTORS_PER_READ, remaining);
            var req = new RAW_READ_INFO
            {
                DiskOffset = (long)lba * 2048, // documented as 2048-byte units
                SectorCount = (uint)batch,
                TrackMode = TRACK_MODE_TYPE.CDDA,
            };
            uint bytesRead;
            if (!DeviceIoControl(
                    handle, IOCTL_CDROM_RAW_READ,
                    ref req, (uint)Marshal.SizeOf<RAW_READ_INFO>(),
                    buffer, (uint)(batch * CD_RAW_SECTOR_SIZE),
                    out bytesRead, IntPtr.Zero))
                throw new IOException($"IOCTL_CDROM_RAW_READ failed at LBA {lba}: {Marshal.GetLastWin32Error()}");

            await destination.WriteAsync(buffer.AsMemory(0, (int)bytesRead), ct).ConfigureAwait(false);
            lba += batch;
            remaining -= batch;
            done += batch;
            progress?.Report((double)done / total);
        }
        await destination.FlushAsync(ct).ConfigureAwait(false);
    }

    private static SafeFileHandle OpenDevice(string drivePath)
    {
        // Accept "D:", "D:\", "\\.\D:" → normalize to "\\.\D:".
        var letter = drivePath.TrimEnd('\\', '/').TrimStart('\\').TrimStart('.').TrimStart('\\');
        var path = $@"\\.\{letter}";
        var handle = CreateFile(path,
            GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
        if (handle.IsInvalid)
            throw new IOException($"Cannot open CD device {path}: {Marshal.GetLastWin32Error()}");
        return handle;
    }

    private static int MsfToLba(in TRACK_DATA.MsfAddress addr) =>
        ((addr.M * 60) + addr.S) * 75 + addr.F - 150;

    // ── P/Invoke ────────────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TRACK_DATA
    {
        public byte Reserved;
        public byte ControlAdr;   // Adr (high nibble) + Control (low nibble)
        public byte TrackNumber;
        public byte Reserved2;
        public MsfAddress Address;

        public byte Control => (byte)(ControlAdr & 0x0F);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MsfAddress
        {
            public byte Reserved;
            public byte M;
            public byte S;
            public byte F;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct CDROM_TOC
    {
        public ushort Length;
        public byte FirstTrack;
        public byte LastTrack;
        // up to 100 tracks + lead-out
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public TRACK_DATA[] TrackData;

        public CDROM_TOC() { TrackData = new TRACK_DATA[100]; Length = 0; FirstTrack = 0; LastTrack = 0; }
    }

    private enum TRACK_MODE_TYPE : uint
    {
        YellowMode2 = 0,
        XAForm2     = 1,
        CDDA        = 2,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct RAW_READ_INFO
    {
        public long DiskOffset;
        public uint SectorCount;
        public TRACK_MODE_TYPE TrackMode;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition,
        uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice, uint dwIoControlCode,
        IntPtr lpInBuffer, uint nInBufferSize,
        ref CDROM_TOC lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice, uint dwIoControlCode,
        ref RAW_READ_INFO lpInBuffer, uint nInBufferSize,
        byte[] lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);
}
