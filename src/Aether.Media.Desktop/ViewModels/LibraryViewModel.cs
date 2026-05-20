using System.Collections.ObjectModel;
using Aether.Media.Content;
using Aether.Media.Core;
using Aether.Media.Core.Models;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aether.Media.Desktop.ViewModels;

/// <summary>
/// Drives the local library screen: scan, search, play and remove content.
/// </summary>
public sealed partial class LibraryViewModel : ViewModelBase
{
    private readonly IMediaLibrary _library;
    private readonly IMediaLibraryScanner _scanner;

    // Full unfiltered snapshot — used as the source for search filtering
    private List<MediaContentViewModel> _allContents = [];

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

    // ── Commands ───────────────────────────────────────────────────────────

    public IAsyncRelayCommand ScanDirectoryCommand { get; }
    public IRelayCommand<MediaContentViewModel> PlayCommand { get; }
    public IAsyncRelayCommand<MediaContentViewModel> RemoveCommand { get; }

    // ── Events ─────────────────────────────────────────────────────────────

    public event EventHandler<MediaContentViewModel>? PlayRequested;

    // ── Constructor ────────────────────────────────────────────────────────

    public LibraryViewModel(IMediaLibrary library, IMediaLibraryScanner scanner)
    {
        _library = library  ?? throw new ArgumentNullException(nameof(library));
        _scanner = scanner  ?? throw new ArgumentNullException(nameof(scanner));

        ScanDirectoryCommand = new AsyncRelayCommand(ExecuteScanDirectoryAsync);
        PlayCommand = new RelayCommand<MediaContentViewModel>(
            vm =>
            {
                if (vm is not null) PlayRequested?.Invoke(this, vm);
            },
            vm => vm is not null);
        RemoveCommand = new AsyncRelayCommand<MediaContentViewModel>(ExecuteRemoveAsync);

        // Subscribe to library events for cross-VM additions
        _library.ContentAdded   += OnContentAdded;
        _library.ContentRemoved += OnContentRemoved;

        // Load existing library entries
        _ = LoadAllAsync();
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task LoadAllAsync()
    {
        var all = await _library.GetAllAsync();
        _allContents = all.Select(c => new MediaContentViewModel(c)).ToList();
        ApplySearch(_searchQuery);
    }

    private async Task ExecuteScanDirectoryAsync()
    {
        // Resolve the StorageProvider from the top-level window
        var topLevel = Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime dt
            ? dt.MainWindow
            : null;

        if (topLevel is null)
            return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title          = "Select media folder",
                AllowMultiple  = false
            });

        if (folders.Count == 0)
            return;

        var path = folders[0].TryGetLocalPath();
        if (string.IsNullOrEmpty(path))
            return;

        ScanPath   = path;
        IsScanning = true;
        try
        {
            var discovered = await _scanner.ScanDirectoryAsync(path, recursive: true);
            foreach (var content in discovered)
                await _library.AddAsync(content);
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
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var vm = new MediaContentViewModel(content);
            _allContents.Insert(0, vm);
            ApplySearch(_searchQuery);
        });
    }

    private void OnContentRemoved(object? sender, string contentHash)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _allContents.RemoveAll(c => c.Source.ContentHash == contentHash);
            ApplySearch(_searchQuery);
        });
    }
}
