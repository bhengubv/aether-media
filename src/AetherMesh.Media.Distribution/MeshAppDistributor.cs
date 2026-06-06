// SPDX-License-Identifier: MIT

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using AetherMesh.Content;
using AetherMesh.Content.Models;
using AetherMesh.Media.Core;
using AetherMesh.Media.Distribution.Models;
using Microsoft.Extensions.Logging;

namespace AetherMesh.Media.Distribution;

/// <summary>
/// Default implementation of <see cref="IMeshAppDistributor"/>.
///
/// Distribution path A — mesh (zero internet):
///   <list type="number">
///     <item>Caller passes a local APK path to <see cref="StartHostingAsync"/>.</item>
///     <item>APK is published to <c>IContentService</c> (chunked, hash-verified).</item>
///     <item>A descriptor is announced to all reachable Aether peers.</item>
///     <item>Nearby devices running any Aether app can download chunk-by-chunk.</item>
///   </list>
///
/// Distribution path B — QR bootstrap (zero Aether on receiver):
///   <list type="number">
///     <item><see cref="StartHostingAsync"/> also starts an <see cref="HttpListener"/> on a random port.</item>
///     <item><see cref="BootstrapUri"/> gives the LAN address — encode as a QR code.</item>
///     <item>Any Android device on the same Wi-Fi scans the code, downloads the APK, installs.</item>
///   </list>
///
/// Update channel — Cloudflare R2:
///   <see cref="CheckForUpdateAsync"/> polls <c>https://cdn.aethermedia.app/apps/{id}/latest.json</c>
///   and <see cref="DownloadAndVerifyAsync"/> fetches and SHA-256 verifies the binary.
/// </summary>
public sealed class MeshAppDistributor : IMeshAppDistributor
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private readonly IContentService _content;
    private readonly HttpClient _http;
    private readonly ILogger<MeshAppDistributor> _logger;
    private readonly FootprintGuard? _guard;
    private readonly string _cacheDir;

    private HttpListener? _listener;
    private string? _servedFilePath;
    private CancellationTokenSource? _listenerCts;
    private Task? _listenerTask;
    private bool _disposed;

    // ── Public surface ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Uri? BootstrapUri { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyList<AppPackage> EcosystemCatalogue => Distribution.EcosystemCatalogue.Apps;

    /// <inheritdoc/>
    public event EventHandler<AppPackage>? AppDiscovered;

    // ── Constructor ────────────────────────────────────────────────────────────

    public MeshAppDistributor(
        IContentService content,
        HttpClient http,
        ILogger<MeshAppDistributor> logger,
        FootprintGuard? guard = null)
    {
        _content  = content  ?? throw new ArgumentNullException(nameof(content));
        _http     = http     ?? throw new ArgumentNullException(nameof(http));
        _logger   = logger   ?? throw new ArgumentNullException(nameof(logger));
        _guard    = guard;
        _cacheDir = Path.Combine(Path.GetTempPath(), "aether-media", "app-cache");

        Directory.CreateDirectory(_cacheDir);

        _content.ContentAnnounced += OnContentAnnounced;
    }

    // ── IMeshAppDistributor ────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task StartHostingAsync(
        string localFilePath,
        AppPackage package,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!File.Exists(localFilePath))
            throw new FileNotFoundException("APK file not found.", localFilePath);

        _servedFilePath = localFilePath;

        // ── 1. Publish to mesh ─────────────────────────────────────────────────
        var apkBytes = await File.ReadAllBytesAsync(localFilePath, ct);

        // Convention: Name encodes "appId|version" so peers can reconstruct AppPackage
        var descriptor = await _content.PublishAsync(
            name             : $"{package.AppId}|{package.Version}",
            data             : apkBytes,
            contentType      : package.ContentType,
            cancellationToken: ct);

        await _content.AnnounceAsync(descriptor, ct);

        _logger.LogInformation(
            "Announced {AppId} v{Version} to mesh (hash={Hash}, {Bytes}B)",
            package.AppId, package.Version, descriptor.RootHash, apkBytes.Length);

        // ── 2. Start HTTP bootstrap server ─────────────────────────────────────
        var port     = FindFreePort();
        var localIp  = GetLocalIpAddress();

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{port}/");

        try
        {
            _listener.Start();
        }
        catch (HttpListenerException)
        {
            // On Windows without admin rights, wildcard binding fails.
            // Fall back to 127.0.0.1 — use the IPv4 loopback explicitly so that
            // HttpListener prefix matching works for requests made to 127.0.0.1.
            // (Binding to "localhost" can route to IPv6 ::1, causing 400s for IPv4.)
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();
            localIp = "127.0.0.1";
            _logger.LogWarning(
                "Wildcard HTTP binding failed — bootstrap server on localhost only. " +
                "Run as administrator for LAN access.");
        }

        BootstrapUri = new Uri($"http://{localIp}:{port}/app");

        _listenerCts  = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _listenerTask = RunListenerAsync(_listenerCts.Token);

        _logger.LogInformation("Bootstrap server running at {Uri}", BootstrapUri);
    }

    /// <inheritdoc/>
    public async Task StopHostingAsync(CancellationToken ct = default)
    {
        _listenerCts?.Cancel();

        if (_listenerTask is not null)
        {
            try { await _listenerTask.WaitAsync(TimeSpan.FromSeconds(2), ct); }
            catch (OperationCanceledException) { }
            catch (TimeoutException) { }
        }

        _listener?.Stop();
        _listener?.Close();
        _listener        = null;
        BootstrapUri     = null;
        _servedFilePath  = null;

        _logger.LogInformation("Bootstrap server stopped.");
    }

    /// <inheritdoc/>
    public async Task<AppUpdateInfo?> CheckForUpdateAsync(
        string appId,
        string currentVersion,
        CancellationToken ct = default)
    {
        var url = $"https://cdn.aethermedia.app/apps/{appId}/latest.json";

        CloudflareVersionManifest manifest;
        try
        {
            var json = await _http.GetStringAsync(url, ct);
            manifest = JsonSerializer.Deserialize<CloudflareVersionManifest>(json, JsonOpts)
                       ?? throw new InvalidOperationException("Null manifest");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Update check unavailable for {AppId}", appId);
            return null;
        }

        if (!IsNewer(manifest.Version, currentVersion))
        {
            _logger.LogDebug("{AppId} is up to date at {Version}", appId, currentVersion);
            return null;
        }

        return new AppUpdateInfo
        {
            AppId          = appId,
            CurrentVersion = currentVersion,
            NewVersion     = manifest.Version,
            DownloadUrl    = manifest.Url,
            Sha256         = manifest.Sha256,
            SizeBytes      = manifest.SizeBytes,
            ReleaseNotes   = manifest.ReleaseNotes ?? string.Empty,
        };
    }

    /// <inheritdoc/>
    public async Task<string> DownloadAndVerifyAsync(
        string url,
        string expectedSha256,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        var fileName  = Path.GetFileName(new Uri(url).LocalPath);
        var localPath = Path.Combine(_cacheDir, fileName);

        // Cache hit: reuse if hash still matches
        if (File.Exists(localPath) && await HashMatchesAsync(localPath, expectedSha256, ct))
        {
            _logger.LogDebug("Cache hit: {File}", fileName);
            progress?.Report(1.0);
            return localPath;
        }

        _logger.LogInformation("Downloading {Url}", url);

        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var total     = response.Content.Headers.ContentLength ?? -1L;
        var received  = 0L;

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        await using var file   = File.Create(localPath);

        var buffer = new byte[81_920];
        int read;

        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, read), ct);
            received += read;

            if (total > 0)
                progress?.Report((double)received / total);
        }

        file.Close();

        if (!await HashMatchesAsync(localPath, expectedSha256, ct))
        {
            File.Delete(localPath);
            throw new InvalidOperationException(
                $"SHA-256 mismatch for '{fileName}'. " +
                $"Expected {expectedSha256}. The file may be corrupt or tampered with.");
        }

        progress?.Report(1.0);
        _logger.LogInformation("Downloaded and verified {File} ({Bytes}B)", fileName, received);
        return localPath;
    }

    // ── IAsyncDisposable ───────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _content.ContentAnnounced -= OnContentAnnounced;
        await StopHostingAsync();
        _listenerCts?.Dispose();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task RunListenerAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener is { IsListening: true })
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await _listener.GetContextAsync().WaitAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (HttpListenerException)       { break; }

            // Handle each request on a thread-pool thread so the loop keeps running
            _ = Task.Run(async () => await ServeRequestAsync(ctx, ct), ct);
        }
    }

    private async Task ServeRequestAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        try
        {
            if (_guard is { SeedingAllowed: false })
            {
                ctx.Response.StatusCode = 503;
                ctx.Response.Headers["Retry-After"] = "60";
                ctx.Response.Close();
                return;
            }

            if (_servedFilePath is null || !File.Exists(_servedFilePath))
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.Close();
                return;
            }

            var name = Path.GetFileName(_servedFilePath);
            ctx.Response.StatusCode  = 200;
            ctx.Response.ContentType = "application/vnd.android.package-archive";
            ctx.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{name}\"";

            await using var fs = File.OpenRead(_servedFilePath);
            ctx.Response.ContentLength64 = fs.Length;
            await fs.CopyToAsync(ctx.Response.OutputStream, ct);
            ctx.Response.Close();

            _logger.LogInformation(
                "Served {Name} ({Bytes}B) to {Remote}",
                name, fs.Length, ctx.Request.RemoteEndPoint);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bootstrap request failed");
            try
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
            }
            catch { /* response already closed */ }
        }
    }

    private void OnContentAnnounced(object? sender, ContentDescriptor descriptor)
    {
        if (_guard is { SeedingAllowed: false })
            return;

        // We only care about app packages
        if (!string.Equals(
                descriptor.ContentType,
                "application/vnd.android.package-archive",
                StringComparison.OrdinalIgnoreCase))
            return;

        // Name convention: "appId|version"
        var parts = descriptor.Name.Split('|', 2);
        if (parts.Length < 2) return;

        var pkg = new AppPackage
        {
            AppId        = parts[0],
            Name         = parts[0],
            Version      = parts[1],
            ContentHash  = descriptor.RootHash,
            SizeBytes    = descriptor.TotalBytes,
            CloudflareUrl= string.Empty,
            Description  = "Discovered from a nearby device.",
        };

        _logger.LogInformation(
            "Discovered app {AppId} v{Version} from mesh (hash={Hash})",
            pkg.AppId, pkg.Version, pkg.ContentHash);

        AppDiscovered?.Invoke(this, pkg);
    }

    private static async Task<bool> HashMatchesAsync(
        string filePath,
        string expectedHex,
        CancellationToken ct)
    {
        await using var fs   = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(fs, ct);
        return string.Equals(
            Convert.ToHexString(hash).ToLowerInvariant(),
            expectedHex.ToLowerInvariant(),
            StringComparison.Ordinal);
    }

    private static int FindFreePort()
    {
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            return Dns.GetHostAddresses(Dns.GetHostName())
                      .FirstOrDefault(a =>
                          a.AddressFamily == AddressFamily.InterNetwork &&
                          !IPAddress.IsLoopback(a))
                      ?.ToString()
                   ?? "127.0.0.1";
        }
        catch { return "127.0.0.1"; }
    }

    private static bool IsNewer(string candidate, string current) =>
        Version.TryParse(candidate, out var c) &&
        Version.TryParse(current,   out var v) &&
        c > v;
}
