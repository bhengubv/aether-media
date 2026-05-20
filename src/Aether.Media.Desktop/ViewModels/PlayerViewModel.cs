using System.Collections.ObjectModel;
using Aether.Media.Core;
using Aether.Media.Core.Models;
using Aether.Media.Social;
using Aether.Media.Streaming;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aether.Media.Desktop.ViewModels;

/// <summary>
/// Drives the media player screen: playback controls, reactions, and watch-party state.
/// </summary>
public sealed partial class PlayerViewModel : ViewModelBase
{
    private readonly IMediaPlayer _player;
    private readonly IReactionService _reactions;
    private readonly IWatchPartyCoordinator _watchParty;

    private const int MaxLiveReactions = 20;

    // ── Observable properties ──────────────────────────────────────────────

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private long _positionMs;

    [ObservableProperty]
    private long _durationMs;

    [ObservableProperty]
    private double _volume = 1.0;

    [ObservableProperty]
    private double _playbackSpeed = 1.0;

    [ObservableProperty]
    private string? _currentTitle;

    [ObservableProperty]
    private ObservableCollection<MediaReactionViewModel> _liveReactions = [];

    [ObservableProperty]
    private bool _isInWatchParty;

    [ObservableProperty]
    private int _watchPartyParticipants;

    /// <summary>Current playback position formatted as H:MM:SS (hours omitted when under 1 hour).</summary>
    public string PositionFormatted => FormatMs(PositionMs);

    /// <summary>Total duration formatted as H:MM:SS (hours omitted when under 1 hour).</summary>
    public string DurationFormatted => FormatMs(DurationMs);

    // ── Commands ───────────────────────────────────────────────────────────

    public IAsyncRelayCommand PlayPauseCommand { get; }
    public IAsyncRelayCommand StopCommand { get; }
    public IAsyncRelayCommand<long> SeekCommand { get; }
    public IAsyncRelayCommand<double> SetVolumeCommand { get; }
    public IAsyncRelayCommand<double> SetSpeedCommand { get; }
    public IAsyncRelayCommand<string> SendReactionCommand { get; }

    // ── Constructor ────────────────────────────────────────────────────────

    public PlayerViewModel(
        IMediaPlayer player,
        IReactionService reactions,
        IWatchPartyCoordinator watchParty)
    {
        _player     = player     ?? throw new ArgumentNullException(nameof(player));
        _reactions  = reactions  ?? throw new ArgumentNullException(nameof(reactions));
        _watchParty = watchParty ?? throw new ArgumentNullException(nameof(watchParty));

        PlayPauseCommand = new AsyncRelayCommand(ExecutePlayPauseAsync);
        StopCommand      = new AsyncRelayCommand(ExecuteStopAsync);
        SeekCommand      = new AsyncRelayCommand<long>(ExecuteSeekAsync);
        SetVolumeCommand = new AsyncRelayCommand<double>(ExecuteSetVolumeAsync);
        SetSpeedCommand  = new AsyncRelayCommand<double>(ExecuteSetSpeedAsync);
        SendReactionCommand = new AsyncRelayCommand<string>(ExecuteSendReactionAsync);

        // Wire player events
        _player.StateChanged    += OnStateChanged;
        _player.PositionChanged += OnPositionChanged;
        _player.MediaLoaded     += OnMediaLoaded;

        // Wire reaction events
        _reactions.ReactionReceived += OnReactionReceived;

        // Wire watch-party events
        _watchParty.ParticipantJoined += (_, _) => UpdateWatchPartyState();
        _watchParty.ParticipantLeft   += (_, _) => UpdateWatchPartyState();

        // Initialise from current player state
        SyncFromPlayer();
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task ExecutePlayPauseAsync()
    {
        if (_player.State == MediaPlayerState.Playing)
            await _player.PauseAsync();
        else
            await _player.PlayAsync();
    }

    private Task ExecuteStopAsync() => _player.StopAsync();

    private Task ExecuteSeekAsync(long positionMs) => _player.SeekAsync(positionMs);

    private async Task ExecuteSetVolumeAsync(double vol)
    {
        await _player.SetVolumeAsync(vol);
        Volume = Math.Clamp(vol, 0.0, 1.0);
    }

    private async Task ExecuteSetSpeedAsync(double speed)
    {
        await _player.SetSpeedAsync(speed);
        PlaybackSpeed = speed;
    }

    private async Task ExecuteSendReactionAsync(string? reactionText)
    {
        if (string.IsNullOrEmpty(reactionText))
            return;

        // Determine type from the emoji string passed in
        var type = reactionText switch
        {
            "❤️" => MediaReactionType.Like,
            "🔁" => MediaReactionType.Share,
            "⭐" => MediaReactionType.SuperReact,
            _    => MediaReactionType.Comment
        };

        var reaction = new MediaReaction(
            reactionId:  Guid.NewGuid(),
            contentHash: _player.CurrentContentHash ?? string.Empty,
            fromUhid:    "local",
            type:        type,
            positionMs:  _player.PositionMs,
            message:     type == MediaReactionType.Comment ? reactionText : null,
            sentAt:      DateTime.UtcNow);

        await _reactions.SendReactionAsync(reaction);
    }

    private void OnStateChanged(object? sender, MediaPlayerState state)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            IsPlaying = state == MediaPlayerState.Playing;
            IsPaused  = state == MediaPlayerState.Paused;
        });
    }

    private void OnPositionChanged(object? sender, long posMs)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            PositionMs = posMs;
            OnPropertyChanged(nameof(PositionFormatted));
        });
    }

    private void OnMediaLoaded(object? sender, MediaContent content)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            CurrentTitle = content.Title;
            DurationMs   = content.DurationMs;
            OnPropertyChanged(nameof(DurationFormatted));
        });
    }

    private void OnReactionReceived(object? sender, MediaReaction reaction)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            while (LiveReactions.Count >= MaxLiveReactions)
                LiveReactions.RemoveAt(LiveReactions.Count - 1);

            LiveReactions.Insert(0, new MediaReactionViewModel(reaction));
        });
    }

    private void UpdateWatchPartyState()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            IsInWatchParty          = _watchParty.ActiveSessionId.HasValue;
            WatchPartyParticipants  = _watchParty.ParticipantUhids.Count;
        });
    }

    private void SyncFromPlayer()
    {
        IsPlaying     = _player.State == MediaPlayerState.Playing;
        IsPaused      = _player.State == MediaPlayerState.Paused;
        PositionMs    = _player.PositionMs;
        DurationMs    = _player.DurationMs;
        Volume        = _player.Volume;
        PlaybackSpeed = _player.PlaybackSpeed;
        IsInWatchParty = _watchParty.ActiveSessionId.HasValue;
        WatchPartyParticipants = _watchParty.ParticipantUhids.Count;
    }

    private static string FormatMs(long ms)
    {
        var totalSeconds = ms / 1000L;
        var hours   = totalSeconds / 3600;
        var minutes = (totalSeconds % 3600) / 60;
        var seconds = totalSeconds % 60;

        return hours > 0
            ? $"{hours}:{minutes:D2}:{seconds:D2}"
            : $"{minutes}:{seconds:D2}";
    }

    partial void OnPositionMsChanged(long value) =>
        OnPropertyChanged(nameof(PositionFormatted));

    partial void OnDurationMsChanged(long value) =>
        OnPropertyChanged(nameof(DurationFormatted));
}
