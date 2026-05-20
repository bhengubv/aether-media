// SPDX-License-Identifier: MIT

using Aether.Media.Distribution.Models;

namespace Aether.Media.Distribution;

/// <summary>
/// Mesh-first app distribution. Hosts a local HTTP server so any Android device
/// on the same Wi-Fi/Wi-Fi Direct network can download and install the app via a
/// QR code scan. Also announces the package over the Aether mesh so nearby nodes
/// that already have the app can discover it without the QR.
///
/// Update channel: polls Cloudflare R2 <c>latest.json</c> on demand and downloads
/// new releases directly — no app store involved.
/// </summary>
public interface IMeshAppDistributor : IAsyncDisposable
{
    /// <summary>
    /// The HTTP URI currently being served, e.g. <c>http://192.168.49.1:54321/app</c>.
    /// Encode this as a QR code and show it to nearby Android devices.
    /// <c>null</c> when not hosting.
    /// </summary>
    Uri? BootstrapUri { get; }

    /// <summary>All apps available in the Aether ecosystem catalogue.</summary>
    IReadOnlyList<AppPackage> EcosystemCatalogue { get; }

    /// <summary>
    /// Start serving <paramref name="localFilePath"/> over a local HTTP endpoint and
    /// announce the package to the Aether mesh via <c>IContentService</c>.
    /// </summary>
    /// <param name="localFilePath">Full path to the APK/installer to serve.</param>
    /// <param name="package">Metadata describing the package being served.</param>
    Task StartHostingAsync(string localFilePath, AppPackage package, CancellationToken ct = default);

    /// <summary>Stop the bootstrap HTTP server and cease mesh announcements.</summary>
    Task StopHostingAsync(CancellationToken ct = default);

    /// <summary>
    /// Query Cloudflare for the latest version of <paramref name="appId"/>.
    /// Returns <c>null</c> if the installed version is already current or if the
    /// network is unavailable (never throws).
    /// </summary>
    Task<AppUpdateInfo?> CheckForUpdateAsync(string appId, string currentVersion, CancellationToken ct = default);

    /// <summary>
    /// Download <paramref name="url"/> to the local app-cache directory, verify the
    /// SHA-256 digest, and return the local file path. Reuses a cached copy if the
    /// hash matches. Throws <see cref="InvalidOperationException"/> on hash mismatch.
    /// </summary>
    Task<string> DownloadAndVerifyAsync(
        string url,
        string expectedSha256,
        IProgress<double>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Fired when a nearby mesh peer announces an app package via
    /// <c>IContentService.ContentAnnounced</c>.
    /// </summary>
    event EventHandler<AppPackage>? AppDiscovered;
}
