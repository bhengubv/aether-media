// SPDX-License-Identifier: MIT

using System.Collections.ObjectModel;
using AetherMedia.Content;
using AetherMedia.Core;
using AetherMedia.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AetherMedia.UI.Shared.ViewModels;

/// <summary>
/// Drives the local library screen: scan, search, play, remove, edit metadata,
/// and find subtitles.
/// </summary>
public sealed partial class LibraryViewModel : ViewModelBase
{
    private readonly IMediaLibrary        _library;
    private readonly IMediaLibraryScanner _scanner;
    private readonly IFilePicker          _filePicker;

    // Full unfiltered snapshot — used as the source for search filtering.
    private List<MediaContentViewModel> _allContents = [];

    // Maps ContentHash → local file path for items discovered in this session.
    private readonly Dictionary<string, string> _localFilePaths = [];

    // ── Observable properties ──────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<MediaContentViewModel> _contents = [];

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string? _scanPath;

    private string _searchQuery = string.Empty;

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
                ApplySearch(value);
        }
    }

    // ── Panel state ────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPanelOpen))]
    private bool _isEditingMetadata;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPanelOpen))]
    private bool _isSearchingSubtitles;

    /// <summary><c>true</c> when any side-panel is open.</summary>
    public bool IsPanelOpen => IsEditingMetadata || IsSearchingSubtitles;

    // ── Child ViewModels ───────────────────────────────────────────────────

    public MetadataEditorViewModel MetadataEditor { get; }
    public SubtitleSearchViewModel SubtitleSearch { get; }

    // ── Commands ───────────────────────────────────────────────────────────

    public IAsyncRelayCommand                         ScanDirectoryCommand    { get; }
    public IRelayCommand<MediaContentViewModel>       PlayCommand             { get; }
    public IAsyncRelayCommand<MediaContentViewModel>  RemoveCommand           { get; }
    public IAsyncRelayCommand<MediaContentViewModel>  EditMetadataCommand     { get; }
    public IAsyncRelayCommand<MediaContentViewModel>  FindSubtitlesCommand    { get; }
    public IRelayCommand                              ClosePanelCommand       { get; }

    // ── Events ─────────────────────────────────────────────────────────────

    public event EventHandler<MediaContentViewModel>? PlayRequested;

    // ── Constructor ────────────────────────────────────────────────────────

    public LibraryViewModel(
        IMediaLibrary          library,
        IMediaLibraryScanner   scanner,
        MetadataEditorViewModel metadataEditor,
        SubtitleSearchViewModel subtitleSearch,
        IFilePicker             filePicker)
    {
        _library    = library    ?? throw new ArgumentNullException(nameof(library));
        _scanner    = scanner    ?? throw new ArgumentNullException(nameof(scanner));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));

        MetadataEditor = metadataEditor ?? throw new ArgumentNullException(nameof(metadataEditor));
        SubtitleSearch = subtitleSearch ?? throw new ArgumentNullException(nameof(subtitleSearch));

        ScanDirectoryCommand = new AsyncRelayCommand(ExecuteScanDirectoryAsync);
        PlayCommand = new RelayCommand<MediaContentViewModel>(
            vm =>
            {
                if (vm is not null) PlayRequested?.Invoke(this, vm);
            },
            vm => vm is not null);
        RemoveCommand        = new AsyncRelayCommand<MediaContentViewModel>(ExecuteRemoveAsync);
        EditMetadataCommand  = new AsyncRelayCommand<MediaContentViewModel>(ExecuteEditMetadataAsync);
        FindSubtitlesCommand = new AsyncRelayCommand<MediaContentViewModel>(ExecuteFindSubtitlesAsync);
        ClosePanelCommand    = new RelayCommand(() =>
        {
            IsEditingMetadata    = false;
            IsSearchingSubtitles = false;
        });

        _library.ContentAdded   += OnContentAdded;
        _library.ContentRemoved += OnContentRemoved;

        _ = LoadAllAsync();
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task LoadAllAsync()
    {
        var all = await _library.GetAllAsync();
        _allContents = all.Select(c => new MediaContentViewModel(c)
        {
            LocalFilePath = _localFilePaths.GetValueOrDefault(c.ContentHash)
        }).ToList();
        ApplySearch(_searchQuery);
    }

    private async Task ExecuteScanDirectoryAsync()
    {
        var path = await _filePicker.PickFolderAsync();
        if (string.IsNullOrEmpty(path))
            return;

        ScanPath   = path;
        IsScanning = true;
        try
        {
            var discovered = await _scanner.ScanDirectoryAsync(path, recursive: true);
            foreach (var item in discovered)
            {
                _localFilePaths[item.Content.ContentHash] = item.FilePath;
                await _library.AddAsync(item.Content);
            }
        }
        finally
        {
            IsScanning = false;
        }
    }

    private async Task ExecuteRemoveAsync(MediaContentViewModel? vm)
    {
        if (vm is null)
            return;
        await _library.RemoveAsync(vm.Source.ContentHash);
    }

    private async Task ExecuteEditMetadataAsync(MediaContentViewModel? vm)
    {
        if (vm?.LocalFilePath is null) return;
        IsSearchingSubtitles = false;
        IsEditingMetadata    = true;
        await MetadataEditor.LoadAsync(vm.LocalFilePath).ConfigureAwait(false);
    }

    private async Task ExecuteFindSubtitlesAsync(MediaContentViewModel? vm)
    {
        if (vm?.LocalFilePath is null || !vm.Source.IsVideo) return;
        IsEditingMetadata    = false;
        IsSearchingSubtitles = true;
        await SubtitleSearch.OpenForAsync(vm.LocalFilePath).ConfigureAwait(false);
    }

    private void ApplySearch(string query)
    {
        var filtered = string.IsNullOrWhiteSpace(query)
            ? _allContents
            : _allContents
                .Where(c =>
                    c.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    c.ContentType.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    c.Codec.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

        Contents.Clear();
        foreach (var item in filtered)
            Contents.Add(item);
    }

    private void OnContentAdded(object? sender, MediaContent content)
    {
        var vm = new MediaContentViewModel(content)
        {
            LocalFilePath = _localFilePaths.GetValueOrDefault(content.ContentHash)
        };
        _allContents.Insert(0, vm);
        ApplySearch(_searchQuery);
    }

    private void OnContentRemoved(object? sender, string contentHash)
    {
        _allContents.RemoveAll(c => c.Source.ContentHash == contentHash);
        ApplySearch(_searchQuery);
    }
}
