// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using AetherNet.Media.LocalLibrary.Interfaces;
using AetherNet.Media.LocalLibrary.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AetherNet.Media.Desktop.ViewModels;

/// <summary>
/// Backs the subtitle search panel.
/// Searches OpenSubtitles for a given video file and lets the user pick and download
/// a result.
/// </summary>
public sealed partial class SubtitleSearchViewModel : ViewModelBase
{
    private readonly ISubtitleService _subtitleService;

    // ── State ──────────────────────────────────────────────────────────────

    [ObservableProperty] private string _videoFilePath = string.Empty;
    [ObservableProperty] private bool   _isSearching;
    [ObservableProperty] private bool   _isDownloading;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _languageCode  = "en";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDownload))]
    private SubtitleSearchResultViewModel? _selectedResult;

    public bool CanDownload =>
        SelectedResult is not null && !IsDownloading;

    public ObservableCollection<SubtitleSearchResultViewModel> Results { get; } = [];

    /// <summary>True while the results list is empty (drives the empty-state label).</summary>
    public bool HasNoResults => Results.Count == 0;

    // ── Commands ───────────────────────────────────────────────────────────

    public IAsyncRelayCommand SearchCommand   { get; }
    public IAsyncRelayCommand DownloadCommand { get; }

    // ── Constructor ────────────────────────────────────────────────────────

    public SubtitleSearchViewModel(ISubtitleService subtitleService)
    {
        _subtitleService = subtitleService
            ?? throw new ArgumentNullException(nameof(subtitleService));

        SearchCommand   = new AsyncRelayCommand(SearchAsync);
        DownloadCommand = new AsyncRelayCommand(DownloadAsync,
            () => CanDownload);

        Results.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoResults));
    }

    // ── Trigger from parent ────────────────────────────────────────────────

    /// <summary>
    /// Opens the panel for <paramref name="videoFilePath"/> and immediately kicks off
    /// a search so the user sees results without having to press Search.
    /// </summary>
    public async Task OpenForAsync(string videoFilePath)
    {
        VideoFilePath = videoFilePath;
        Results.Clear();
        SelectedResult = null;
        StatusMessage  = string.Empty;

        await SearchAsync().ConfigureAwait(false);
    }

    // ── Search ─────────────────────────────────────────────────────────────

    private async Task SearchAsync()
    {
        if (string.IsNullOrEmpty(VideoFilePath))
        {
            StatusMessage = "No video file selected.";
            return;
        }

        IsSearching   = true;
        StatusMessage = "Searching OpenSubtitles…";
        Results.Clear();
        SelectedResult = null;

        try
        {
            var found = await _subtitleService
                .SearchAsync(VideoFilePath, language: LanguageCode)
                .ConfigureAwait(false);

            foreach (var r in found)
                Results.Add(new SubtitleSearchResultViewModel(r));

            StatusMessage = found.Count > 0
                ? $"Found {found.Count} subtitle{(found.Count == 1 ? "" : "s")}."
                : "No subtitles found. Try a different language or check the file.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Search error: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    // ── Download ───────────────────────────────────────────────────────────

    private async Task DownloadAsync()
    {
        if (SelectedResult is null || string.IsNullOrEmpty(VideoFilePath))
            return;

        IsDownloading = true;
        StatusMessage = $"Downloading \"{SelectedResult.ReleaseName}\"…";

        try
        {
            var srtPath = await _subtitleService
                .DownloadAsync(VideoFilePath, SelectedResult.FileId)
                .ConfigureAwait(false);

            StatusMessage = $"Saved: {Path.GetFileName(srtPath)}";
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
}

// ── Result row VM ──────────────────────────────────────────────────────────────

/// <summary>Thin wrapper exposing <see cref="SubtitleSearchResult"/> to the view.</summary>
public sealed class SubtitleSearchResultViewModel
{
    private readonly SubtitleSearchResult _model;

    public SubtitleSearchResultViewModel(SubtitleSearchResult model) => _model = model;

    public string FileId        => _model.FileId;
    public string MovieTitle    => _model.MovieTitle;
    public string Language      => _model.Language.ToUpperInvariant();
    public string ReleaseName   => _model.ReleaseName;
    public int    DownloadCount => _model.DownloadCount;
    public string RatingText    => $"{_model.Rating:F1}";
    public bool   HashMatch     => _model.HashMatch;

    /// <summary>Downloads badge: "✓ Hash match" or download count.</summary>
    public string BadgeText =>
        HashMatch
            ? "✓ hash match"
            : $"{DownloadCount:N0} downloads";
}
