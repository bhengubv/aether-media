// SPDX-License-Identifier: MIT

using AetherNet.Media.LocalLibrary.Interfaces;
using AetherNet.Media.LocalLibrary.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AetherNet.Media.UI.Shared.ViewModels;

/// <summary>
/// Backs the metadata editor panel.
/// Handles both audio (ID3/FLAC/OGG tags via <see cref="IMetadataEditor"/>)
/// and video (Kodi NFO via <see cref="IMovieMetadataService"/>).
/// </summary>
public sealed partial class MetadataEditorViewModel : ViewModelBase
{
    private readonly IMetadataEditor       _audioEditor;
    private readonly IMovieMetadataService _movieService;

    // ── State ──────────────────────────────────────────────────────────────

    [ObservableProperty] private string _filePath      = string.Empty;
    [ObservableProperty] private bool   _isLoading;
    [ObservableProperty] private bool   _isSaving;
    [ObservableProperty] private string _statusMessage = string.Empty;

    // ── Audio fields ───────────────────────────────────────────────────────

    [ObservableProperty] private string _title       = string.Empty;
    [ObservableProperty] private string _artist      = string.Empty;
    [ObservableProperty] private string _albumArtist = string.Empty;
    [ObservableProperty] private string _album       = string.Empty;
    [ObservableProperty] private uint   _track;
    [ObservableProperty] private uint   _trackCount;
    [ObservableProperty] private uint   _disc;
    [ObservableProperty] private uint   _discCount;
    [ObservableProperty] private uint   _year;
    [ObservableProperty] private string _genresText  = string.Empty;
    [ObservableProperty] private string _comment     = string.Empty;

    // ── Video fields ───────────────────────────────────────────────────────

    [ObservableProperty] private string _plot          = string.Empty;
    [ObservableProperty] private string _tagline       = string.Empty;
    [ObservableProperty] private float  _rating;
    [ObservableProperty] private int    _runtime;
    [ObservableProperty] private string _directorsText = string.Empty;
    [ObservableProperty] private string _castText      = string.Empty;
    [ObservableProperty] private string _imdbId        = string.Empty;
    [ObservableProperty] private string _tmdbId        = string.Empty;
    [ObservableProperty] private bool   _watched;

    // ── Discriminator ─────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVideoMode))]
    private bool _isAudioMode;

    public bool IsVideoMode => !IsAudioMode;

    // ── Commands ───────────────────────────────────────────────────────────

    public IAsyncRelayCommand SaveCommand { get; }

    // ── Constructor ────────────────────────────────────────────────────────

    public MetadataEditorViewModel(
        IMetadataEditor       audioEditor,
        IMovieMetadataService movieService)
    {
        _audioEditor  = audioEditor  ?? throw new ArgumentNullException(nameof(audioEditor));
        _movieService = movieService ?? throw new ArgumentNullException(nameof(movieService));

        SaveCommand = new AsyncRelayCommand(SaveAsync);
    }

    // ── Load ───────────────────────────────────────────────────────────────

    /// <summary>Loads metadata from <paramref name="filePath"/> into the observable properties.</summary>
    public async Task LoadAsync(string filePath)
    {
        FilePath      = filePath;
        IsLoading     = true;
        StatusMessage = string.Empty;

        try
        {
            if (_audioEditor.CanEdit(filePath))
            {
                IsAudioMode = true;
                var meta    = await _audioEditor.ReadAsync(filePath).ConfigureAwait(false);
                if (meta is not null)
                    PopulateAudio(meta);
            }
            else
            {
                IsAudioMode = false;
                var meta    = await _movieService.ReadAsync(filePath).ConfigureAwait(false);
                if (meta is not null)
                    PopulateVideo(meta);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not load metadata: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Save ───────────────────────────────────────────────────────────────

    private async Task SaveAsync()
    {
        if (string.IsNullOrEmpty(FilePath))
            return;

        IsSaving      = true;
        StatusMessage = string.Empty;

        try
        {
            if (IsAudioMode)
            {
                var meta = BuildAudioMetadata();
                await _audioEditor.WriteAsync(meta).ConfigureAwait(false);
            }
            else
            {
                var meta = BuildVideoMetadata();
                await _movieService.WriteAsync(meta).ConfigureAwait(false);
            }

            StatusMessage = "Saved.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    // ── Populate helpers ───────────────────────────────────────────────────

    private void PopulateAudio(TrackMetadata m)
    {
        Title       = m.Title;
        Artist      = m.Artist;
        AlbumArtist = m.AlbumArtist;
        Album       = m.Album;
        Track       = m.Track;
        TrackCount  = m.TrackCount;
        Disc        = m.Disc;
        DiscCount   = m.DiscCount;
        Year        = m.Year;
        Comment     = m.Comment;
        GenresText  = string.Join(", ", m.Genres);
    }

    private void PopulateVideo(MovieMetadata m)
    {
        Title         = m.Title;
        Year          = (uint)Math.Clamp(m.Year, 0, 9999);
        Plot          = m.Plot;
        Tagline       = m.Tagline;
        Rating        = m.Rating;
        Runtime       = m.RuntimeMinutes;
        GenresText    = string.Join(", ", m.Genres);
        DirectorsText = string.Join("\n", m.Directors);
        CastText      = string.Join("\n", m.Cast);
        ImdbId        = m.ImdbId ?? string.Empty;
        TmdbId        = m.TmdbId ?? string.Empty;
        Watched       = m.Watched;
    }

    // ── Build helpers ──────────────────────────────────────────────────────

    private TrackMetadata BuildAudioMetadata() => new()
    {
        FilePath    = FilePath,
        Title       = Title,
        Artist      = Artist,
        AlbumArtist = AlbumArtist,
        Album       = Album,
        Track       = Track,
        TrackCount  = TrackCount,
        Disc        = Disc,
        DiscCount   = DiscCount,
        Year        = Year,
        Comment     = Comment,
        Genres      = SplitCsv(GenresText)
    };

    private MovieMetadata BuildVideoMetadata() => new()
    {
        FilePath       = FilePath,
        Title          = Title,
        Year           = (int)Year,
        Plot           = Plot,
        Tagline        = Tagline,
        Rating         = Rating,
        RuntimeMinutes = Runtime,
        Genres         = SplitCsv(GenresText),
        Directors      = SplitLines(DirectorsText),
        Cast           = SplitLines(CastText),
        ImdbId         = string.IsNullOrWhiteSpace(ImdbId) ? null : ImdbId,
        TmdbId         = string.IsNullOrWhiteSpace(TmdbId) ? null : TmdbId,
        Watched        = Watched
    };

    // ── Static helpers ─────────────────────────────────────────────────────

    private static string[] SplitCsv(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string[] SplitLines(string value) =>
        value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
