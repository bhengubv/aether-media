// SPDX-License-Identifier: MIT

using System.Runtime.Versioning;
using Microsoft.Win32;

namespace AetherMedia.LocalLibrary.Audio.Platform;

/// <summary>
/// Windows <see cref="IFileAssociations"/> backed by per-user
/// <c>HKCU\Software\Classes</c> registry keys. Avoids needing
/// administrator privileges (no <c>HKLM</c> writes) and survives across
/// installs that don't run an MSI.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsFileAssociations : IFileAssociations
{
    private const string ProgIdPrefix = "AetherMedia.";

    /// <inheritdoc/>
    public bool IsAssociated(string extension)
    {
        if (!OperatingSystem.IsWindows()) return false;
        ArgumentException.ThrowIfNullOrEmpty(extension);
        using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{Normalise(extension)}");
        return key?.GetValue(null) is string s && s.StartsWith(ProgIdPrefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public void Associate(string extension, string applicationPath, string applicationName)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("WindowsFileAssociations is Windows-only.");
        ArgumentException.ThrowIfNullOrEmpty(extension);
        ArgumentException.ThrowIfNullOrEmpty(applicationPath);
        ArgumentException.ThrowIfNullOrEmpty(applicationName);

        var ext = Normalise(extension);
        var progId = ProgIdPrefix + ext.TrimStart('.').ToLowerInvariant();

        using (var extKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ext}"))
            extKey.SetValue(null, progId);

        using (var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}"))
        {
            progIdKey.SetValue(null, applicationName);
            using var cmdKey = progIdKey.CreateSubKey(@"shell\open\command");
            cmdKey.SetValue(null, $"\"{applicationPath}\" \"%1\"");
        }
    }

    /// <inheritdoc/>
    public void Unassociate(string extension)
    {
        if (!OperatingSystem.IsWindows()) return;
        ArgumentException.ThrowIfNullOrEmpty(extension);
        var ext = Normalise(extension);
        var progId = ProgIdPrefix + ext.TrimStart('.').ToLowerInvariant();
        try { Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ext}", throwOnMissingSubKey: false); }
        catch (ArgumentException) { }
        try { Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{progId}", throwOnMissingSubKey: false); }
        catch (ArgumentException) { }
    }

    private static string Normalise(string extension) =>
        extension.StartsWith('.') ? extension : "." + extension;
}
