// SPDX-License-Identifier: MIT

using AetherMedia.Distribution.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AetherMedia.Desktop.ViewModels;

/// <summary>View model wrapping a single <see cref="AppPackage"/> in the More Apps list.</summary>
public sealed partial class AppPackageViewModel : ViewModelBase
{
    // ── Immutable identity ─────────────────────────────────────────────────────

    public string AppId       { get; }
    public string Name        { get; }
    public string Description { get; }
    public string Version     { get; }
    public string TagsDisplay { get; }

    // ── Mutable state ──────────────────────────────────────────────────────────

    /// <summary>True once the APK has been downloaded and verified — ready to install.</summary>
    [ObservableProperty] private bool _isReady;

    /// <summary>Local file path after a successful download. Null until ready.</summary>
    [ObservableProperty] private string? _localFilePath;

    // ── Constructor ────────────────────────────────────────────────────────────

    public AppPackageViewModel(AppPackage package)
    {
        AppId       = package.AppId;
        Name        = package.Name;
        Description = package.Description;
        Version     = package.Version;
        TagsDisplay = package.TagsDisplay;
    }
}
