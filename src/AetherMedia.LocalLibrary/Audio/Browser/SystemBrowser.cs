// SPDX-License-Identifier: MIT

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AetherMedia.LocalLibrary.Audio.Browser;

/// <summary>
/// Default <see cref="IInternalBrowser"/> implementation — hands the URL to
/// the OS so it opens in the user's default browser. Works cross-platform
/// (Windows / macOS / Linux) without any GUI dependency.
/// </summary>
public sealed class SystemBrowser : IInternalBrowser
{
    /// <inheritdoc/>
    public bool Open(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);
        if (!url.IsAbsoluteUri) return false;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url.ToString(),
                    UseShellExecute = true,
                });
                return true;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url.ToString());
                return true;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url.ToString());
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
