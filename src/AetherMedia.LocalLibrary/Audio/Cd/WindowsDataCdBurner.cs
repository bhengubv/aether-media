// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace AetherMedia.LocalLibrary.Audio.Cd;

/// <summary>
/// Windows data-CD burner. Builds an ISO 9660 / UDF image using
/// <c>MsftFileSystemImage</c> (IMAPI2FS) and writes it to the disc with
/// <c>MsftDiscFormat2Data</c> (IMAPI2). Late-bound through
/// <c>dynamic</c> against IDispatch — same pattern as
/// <see cref="WindowsCdBurner"/> — so we don't have to declare every IMAPI
/// COM interface up front.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsDataCdBurner : IDataCdBurner
{
    private static readonly Guid CLSID_MsftDiscMaster2 =
        new("2735412E-7F64-5B0F-8F00-5D77AFBE261E");
    private static readonly Guid CLSID_MsftDiscRecorder2 =
        new("2735412A-7F64-5B0F-8F00-5D77AFBE261E");
    private static readonly Guid CLSID_MsftDiscFormat2Data =
        new("2735412B-7F64-5B0F-8F00-5D77AFBE261E");
    private static readonly Guid CLSID_MsftFileSystemImage =
        new("2C941FE5-975B-59BE-A960-9A2A262853A5");

    /// <inheritdoc/>
    public IReadOnlyList<string> EnumerateRecorders()
    {
        if (!OperatingSystem.IsWindows()) return Array.Empty<string>();
        var ids = new List<string>();
        object? master = null;
        try
        {
            master = CreateCom(CLSID_MsftDiscMaster2);
            if (master is null) return ids;
            dynamic dyn = master;
            foreach (string id in dyn) ids.Add(id);
        }
        catch (COMException) { }
        finally
        {
            if (master is not null) Marshal.FinalReleaseComObject(master);
        }
        return ids;
    }

    /// <inheritdoc/>
    public Task BurnAsync(DataCdRequest request, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.RecorderId);
        if (request.Files.Count == 0)
            throw new InvalidOperationException("At least one file is required.");
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("WindowsDataCdBurner is Windows-only.");

        return Task.Run(() => BurnSync(request, progress, ct), ct);
    }

    private void BurnSync(DataCdRequest request, IProgress<double>? progress, CancellationToken ct)
    {
        object? recorder = null, format = null, fsi = null, fsiResult = null;
        try
        {
            recorder = CreateCom(CLSID_MsftDiscRecorder2)
                ?? throw new InvalidOperationException("MsftDiscRecorder2 unavailable.");
            dynamic dynRecorder = recorder;
            dynRecorder.InitializeDiscRecorder(request.RecorderId);

            // 1. Build the file-system image.
            fsi = CreateCom(CLSID_MsftFileSystemImage)
                ?? throw new InvalidOperationException("MsftFileSystemImage unavailable.");
            dynamic dynFsi = fsi;
            dynFsi.ChooseImageDefaults(recorder);
            dynFsi.VolumeName = request.VolumeLabel ?? "AETHERMEDIA";

            var root = dynFsi.Root;
            var rootCommon = LongestCommonDirectory(request.Files);

            foreach (var path in request.Files)
            {
                ct.ThrowIfCancellationRequested();
                if (!System.IO.File.Exists(path)) continue;
                AddFileToRoot(root, path, rootCommon);
            }

            fsiResult = dynFsi.CreateResultImage();
            dynamic dynResult = fsiResult;
            object imageStream = dynResult.ImageStream;

            // 2. Write the image to the disc.
            format = CreateCom(CLSID_MsftDiscFormat2Data)
                ?? throw new InvalidOperationException("MsftDiscFormat2Data unavailable.");
            dynamic dynFormat = format;
            dynFormat.Recorder = recorder;
            dynFormat.ForceMediaToBeClosed = true;
            dynFormat.ClientName = "AetherMedia";

            progress?.Report(0.05);
            try
            {
                dynFormat.Write(imageStream);
                progress?.Report(1.0);
            }
            finally
            {
                Marshal.ReleaseComObject(imageStream);
            }
        }
        finally
        {
            if (fsiResult is not null) Marshal.FinalReleaseComObject(fsiResult);
            if (fsi       is not null) Marshal.FinalReleaseComObject(fsi);
            if (format    is not null) Marshal.FinalReleaseComObject(format);
            if (recorder  is not null) Marshal.FinalReleaseComObject(recorder);
        }
    }

    /// <summary>
    /// Walk into the IFileSystemImage root and add <paramref name="path"/>
    /// at its relative position under <paramref name="rootCommon"/>. The
    /// directory tree is created on the fly via AddTree.
    /// </summary>
    private static void AddFileToRoot(dynamic root, string path, string rootCommon)
    {
        var rel = System.IO.Path.GetRelativePath(rootCommon, path);
        var segments = rel.Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        dynamic node = root;
        for (var i = 0; i < segments.Length - 1; i++)
        {
            var name = segments[i];
            try { node = node.GetItem(name); }
            catch (COMException) { node.AddTree(System.IO.Path.GetDirectoryName(path)!, true); return; }
        }
        node.AddFile(segments[^1], OpenStream(path));
    }

    private static object OpenStream(string path)
    {
        // SHCreateStreamOnFileEx is the standard way to wrap a path in an
        // IStream. Use FileMode.Read.
        const int STGM_READ = 0;
        const int STGM_SHARE_DENY_WRITE = 0x00000020;
        SHCreateStreamOnFileEx(path, STGM_READ | STGM_SHARE_DENY_WRITE, 0, false, IntPtr.Zero, out var stream);
        return stream;
    }

    private static string LongestCommonDirectory(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0) return "";
        var first = System.IO.Path.GetDirectoryName(paths[0]) ?? "";
        var common = first;
        foreach (var p in paths.Skip(1))
        {
            var dir = System.IO.Path.GetDirectoryName(p) ?? "";
            while (!dir.StartsWith(common, StringComparison.OrdinalIgnoreCase))
            {
                common = System.IO.Path.GetDirectoryName(common) ?? "";
                if (common.Length == 0) return "";
            }
        }
        return common;
    }

    private static object? CreateCom(Guid clsid)
    {
        var type = Type.GetTypeFromCLSID(clsid);
        return type is null ? null : Activator.CreateInstance(type);
    }

    [DllImport("shlwapi.dll", PreserveSig = false, CharSet = CharSet.Unicode)]
    private static extern void SHCreateStreamOnFileEx(
        string pszFile, uint grfMode, uint dwAttributes, bool fCreate, IntPtr pstmTemplate,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppstm);
}
