// SPDX-License-Identifier: MIT

namespace AetherMedia.Distribution.Models;

/// <summary>
/// Returned by <see cref="IMeshAppDistributor.CheckForUpdateAsync"/> when a newer
/// version of an app is available on Cloudflare.
/// </summary>
public sealed record AppUpdateInfo
{
    /// <summary>App identifier — e.g. "aether-media".</summary>
    public required string AppId { get; init; }

    /// <summary>The version currently installed on this device.</summary>
    public required string CurrentVersion { get; init; }

    /// <summary>The newer version available for download.</summary>
    public required string NewVersion { get; init; }

    /// <summary>Direct Cloudflare R2 URL for the installer binary.</summary>
    public required string DownloadUrl { get; init; }

    /// <summary>Expected SHA-256 hex digest of the downloaded file.</summary>
    public required string Sha256 { get; init; }

    /// <summary>Installer size in bytes.</summary>
    public required long SizeBytes { get; init; }

    /// <summary>Human-readable change summary. May be empty.</summary>
    public string ReleaseNotes { get; init; } = string.Empty;
}
