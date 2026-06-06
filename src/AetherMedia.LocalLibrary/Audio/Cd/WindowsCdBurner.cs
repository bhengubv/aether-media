// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;

namespace AetherMedia.LocalLibrary.Audio.Cd;

/// <summary>
/// Windows IMAPI2 audio-CD burner. Uses
/// <c>MsftDiscMaster2</c> to enumerate recorders,
/// <c>MsftDiscRecorder2</c> to bind to a device, and
/// <c>MsftDiscFormat2TrackAtOnce</c> to write one CDDA session containing
/// the requested PCM tracks. Late-bound via <c>dynamic</c> against
/// <c>IDispatch</c> so we don't have to declare every IMAPI2 COM interface
/// just to call <c>PrepareMedia</c> / <c>AddAudioTrack</c> / <c>ReleaseMedia</c>.
///
/// <para>
/// IMAPI2 ships in every Windows version from Vista onward, so the COM
/// type registration is always present on supported hosts. The first-pass
/// implementation does not subscribe to the burn-progress event sink; the
/// caller-provided <see cref="IProgress{T}"/> reports per-track completion,
/// not in-track byte progress.
/// </para>
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsCdBurner : ICdBurner
{
    private const int STREAM_SEEK_SET = 0;
    private const int STGM_READ = 0;

    private static readonly Guid CLSID_MsftDiscMaster2 =
        new("2735412E-7F64-5B0F-8F00-5D77AFBE261E");
    private static readonly Guid CLSID_MsftDiscRecorder2 =
        new("2735412A-7F64-5B0F-8F00-5D77AFBE261E");
    private static readonly Guid CLSID_MsftDiscFormat2TrackAtOnce =
        new("27354123-7F64-5B0F-8F00-5D77AFBE261E");

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
            foreach (string id in dyn)
                ids.Add(id);
        }
        catch (COMException) { /* IMAPI2 service stopped, no media subsystem — degrade gracefully */ }
        finally
        {
            if (master is not null) Marshal.FinalReleaseComObject(master);
        }
        return ids;
    }

    /// <inheritdoc/>
    public Task BurnAsync(CdBurnRequest request, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(request.RecorderId);
        if (request.Tracks.Count == 0)
            throw new InvalidOperationException("At least one PCM track is required.");
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("WindowsCdBurner is Windows-only.");

        return Task.Run(() => BurnSync(request, progress, ct), ct);
    }

    private void BurnSync(CdBurnRequest request, IProgress<double>? progress, CancellationToken ct)
    {
        object? recorder = null, format = null;
        try
        {
            recorder = CreateCom(CLSID_MsftDiscRecorder2)
                ?? throw new InvalidOperationException("IMAPI2 disc recorder not available.");
            dynamic dynRecorder = recorder;
            dynRecorder.InitializeDiscRecorder(request.RecorderId);

            format = CreateCom(CLSID_MsftDiscFormat2TrackAtOnce)
                ?? throw new InvalidOperationException("IMAPI2 track-at-once writer not available.");
            dynamic dynFormat = format;
            dynFormat.Recorder = recorder;
            dynFormat.BufferUnderrunFreeDisabled = false;
            dynFormat.PrepareMedia();

            try
            {
                for (var i = 0; i < request.Tracks.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    using var pcm = request.Tracks[i]()
                        ?? throw new InvalidOperationException($"Track {i} factory returned null stream.");
                    var comStream = CopyToComStream(pcm);
                    try { dynFormat.AddAudioTrack(comStream); }
                    finally { Marshal.ReleaseComObject(comStream); }
                    progress?.Report((double)(i + 1) / request.Tracks.Count);
                }
            }
            finally
            {
                try { dynFormat.ReleaseMedia(); }
                catch (COMException) { /* release-after-cancel — best effort */ }
            }
        }
        finally
        {
            if (format   is not null) Marshal.FinalReleaseComObject(format);
            if (recorder is not null) Marshal.FinalReleaseComObject(recorder);
        }
    }

    /// <summary>
    /// Copy a managed PCM stream into a freshly-created HGLOBAL-backed COM
    /// <see cref="IStream"/>. IMAPI2 takes an IStream pointer for each
    /// audio track; wrapping the managed Stream in a HGLOBAL stream avoids
    /// the cost of writing a custom IStream COM adapter for an interaction
    /// that fits in memory comfortably (~40 MB for a 4-minute track).
    /// </summary>
    private static IStream CopyToComStream(Stream src)
    {
        CreateStreamOnHGlobal(IntPtr.Zero, true, out var dest);
        var buf = new byte[64 * 1024];
        int n;
        while ((n = src.Read(buf, 0, buf.Length)) > 0)
            dest.Write(buf, n, IntPtr.Zero);
        dest.Seek(0L, STREAM_SEEK_SET, IntPtr.Zero);
        return dest;
    }

    private static object? CreateCom(Guid clsid)
    {
        var type = Type.GetTypeFromCLSID(clsid);
        return type is null ? null : Activator.CreateInstance(type);
    }

    [DllImport("ole32.dll", PreserveSig = false)]
    private static extern void CreateStreamOnHGlobal(
        IntPtr hGlobal, bool fDeleteOnRelease,
        [MarshalAs(UnmanagedType.Interface)] out IStream ppstm);
}
