// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using System.Security.Cryptography;
using AetherMesh.Media.Distribution;
using AetherMesh.Media.Distribution.Models;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QRCoder;

namespace AetherMesh.Media.Desktop.ViewModels;

/// <summary>
/// Backs the "More Apps" screen — the ecosystem catalogue and the
/// "Install From a Friend" share-this-app flow.
/// </summary>
public sealed partial class MoreAppsViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly IMeshAppDistributor _distributor;

    // ── Observable state ───────────────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<AppPackageViewModel> _apps = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotDownloading))]
    private bool _isDownloading;

    public bool IsNotDownloading => !IsDownloading;

    [ObservableProperty] private double _downloadProgress;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotHosting))]
    private bool _isHosting;

    public bool IsNotHosting => !IsHosting;

    /// <summary>QR code bitmap — non-null while hosting.</summary>
    [ObservableProperty] private Bitmap? _qrCodeBitmap;

    /// <summary>Plain-text bootstrap URL shown alongside the QR.</summary>
    [ObservableProperty] private string _bootstrapUrl = string.Empty;

    [ObservableProperty] private string _statusMessage = "Get any app from the Aether ecosystem.";

    // ── Commands ───────────────────────────────────────────────────────────────

    public IAsyncRelayCommand<AppPackageViewModel?> GetAppCommand   { get; }
    public IAsyncRelayCommand                       ShareThisApp    { get; }
    public IAsyncRelayCommand                       StopSharing     { get; }

    // ── Constructor ────────────────────────────────────────────────────────────

    public MoreAppsViewModel(IMeshAppDistributor distributor)
    {
        _distributor = distributor;

        foreach (var pkg in distributor.EcosystemCatalogue)
            Apps.Add(new AppPackageViewModel(pkg));

        _distributor.AppDiscovered += OnAppDiscovered;

        GetAppCommand = new AsyncRelayCommand<AppPackageViewModel?>(GetAppAsync);
        ShareThisApp  = new AsyncRelayCommand(ShareThisAppAsync);
        StopSharing   = new AsyncRelayCommand(StopSharingAsync);
    }

    // ── Command implementations ────────────────────────────────────────────────

    private async Task GetAppAsync(AppPackageViewModel? vm)
    {
        if (vm is null || IsDownloading) return;

        IsDownloading   = true;
        DownloadProgress = 0;
        StatusMessage   = $"Checking for {vm.Name}...";

        try
        {
            // Pass "0.0.0" to force a fresh download regardless of local version
            var update = await _distributor.CheckForUpdateAsync(vm.AppId, "0.0.0");

            if (update is null)
            {
                StatusMessage = $"{vm.Name} — no download available right now. " +
                                "Try receiving it from a nearby device on the mesh.";
                return;
            }

            StatusMessage = $"Downloading {vm.Name} {update.NewVersion}…";
            var progress  = new Progress<double>(p => DownloadProgress = p);
            var path      = await _distributor.DownloadAndVerifyAsync(
                update.DownloadUrl, update.Sha256, progress);

            vm.LocalFilePath = path;
            vm.IsReady       = true;
            StatusMessage    = $"{vm.Name} {update.NewVersion} ready — open the file to install.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Download failed: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
        }
    }

    private async Task ShareThisAppAsync()
    {
        if (IsHosting) return;

        StatusMessage    = "Fetching latest Aether Media package from Cloudflare…";
        IsDownloading    = true;
        DownloadProgress = 0;

        try
        {
            var update = await _distributor.CheckForUpdateAsync("aether-media", "0.0.0");

            if (update is null)
            {
                StatusMessage = "Could not reach Cloudflare — check your internet connection.";
                return;
            }

            StatusMessage = $"Downloading {update.NewVersion}…";
            var progress  = new Progress<double>(p => DownloadProgress = p);
            var path      = await _distributor.DownloadAndVerifyAsync(
                update.DownloadUrl, update.Sha256, progress);

            var pkg = new AppPackage
            {
                AppId        = "aether-media",
                Name         = "Aether Media",
                Version      = update.NewVersion,
                ContentHash  = update.Sha256,
                SizeBytes    = update.SizeBytes,
                CloudflareUrl= update.DownloadUrl,
                Description  = "The decentralised media network.",
            };

            await _distributor.StartHostingAsync(path, pkg);

            IsHosting    = true;
            BootstrapUrl = _distributor.BootstrapUri?.ToString() ?? string.Empty;
            QrCodeBitmap = GenerateQr(BootstrapUrl);

            StatusMessage = "Sharing! Point any Android camera at the QR code.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Share failed: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
        }
    }

    private async Task StopSharingAsync()
    {
        await _distributor.StopHostingAsync();
        IsHosting    = false;
        BootstrapUrl = string.Empty;
        QrCodeBitmap?.Dispose();
        QrCodeBitmap = null;
        StatusMessage = "Sharing stopped.";
    }

    // ── Event handlers ─────────────────────────────────────────────────────────

    private void OnAppDiscovered(object? sender, AppPackage pkg)
    {
        // Surface mesh-discovered apps that are not already in the catalogue
        if (Apps.All(a => a.AppId != pkg.AppId))
            Apps.Add(new AppPackageViewModel(pkg));
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Bitmap GenerateQr(string data)
    {
        using var generator = new QRCodeGenerator();
        using var qrData    = generator.CreateQrCode(data, QRCodeGenerator.ECCLevel.M);
        using var qrCode    = new PngByteQRCode(qrData);
        var png             = qrCode.GetGraphic(pixelsPerModule: 8);
        using var ms        = new MemoryStream(png);
        return new Bitmap(ms);
    }

    public async ValueTask DisposeAsync()
    {
        _distributor.AppDiscovered -= OnAppDiscovered;

        if (IsHosting)
            await _distributor.StopHostingAsync();

        QrCodeBitmap?.Dispose();
    }
}
