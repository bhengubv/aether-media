// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using AetherNet.Content;
using AetherMedia.Reel.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherMedia.Reel;

/// <summary>
/// Sound library backed by <c>IContentService</c> for chunk storage and a local
/// JSON index for metadata and trending information.
///
/// Audio extraction from video files is handled by reading the raw file as bytes
/// and publishing the whole file as a content chunk. A production implementation
/// would use FFmpeg or LibVLC to demux the audio stream; this implementation
/// publishes the full source file and notes the limitation.
/// </summary>
public class SoundLibrary : ISoundLibrary
{
    private const string SoundContentType = "audio/aether-sound";

    private readonly IContentService      _content;
    private readonly string               _localUhid;
    private readonly string               _indexPath;
    private readonly string               _cacheDir;
    private readonly SemaphoreSlim        _lock = new(1, 1);
    private readonly ILogger<SoundLibrary> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".aac", ".flac", ".ogg", ".opus", ".wav", ".m4a", ".wma", ".aiff", ".aif",
    };

    // ── Constructor ───────────────────────────────────────────────────────────

    public SoundLibrary(
        IContentService content,
        string          localUhid,
        ILogger<SoundLibrary>? logger = null)
        : this(content, localUhid, dataDirectory: null, logger) { }

    // Protected constructor allows test subclasses to inject a temp directory.
    protected SoundLibrary(
        IContentService        content,
        string                 localUhid,
        string?                dataDirectory,
        ILogger<SoundLibrary>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localUhid);
        _content   = content ?? throw new ArgumentNullException(nameof(content));
        _localUhid = localUhid;
        _logger    = logger ?? NullLogger<SoundLibrary>.Instance;

        var dir = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "aether-media");
        Directory.CreateDirectory(dir);
        _indexPath = Path.Combine(dir, "sound-library.json");
        _cacheDir  = Path.Combine(dir, "sound-cache");
        Directory.CreateDirectory(_cacheDir);
    }

    // ── ISoundLibrary ─────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<Sound>> GetTrendingAsync(
        int count = 20,
        CancellationToken ct = default)
    {
        var index = await LoadIndexAsync(ct).ConfigureAwait(false);
        return index
            .OrderByDescending(s => s.UseCount)
            .Take(count)
            .ToList();
    }

    public async Task<IReadOnlyList<Sound>> SearchAsync(
        string query,
        int    count = 20,
        CancellationToken ct = default)
    {
        var index = await LoadIndexAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(query))
            return index.Take(count).ToList();

        return index
            .Where(s =>
                s.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (s.ArtistName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .Take(count)
            .ToList();
    }

    public async Task<Sound?> GetAsync(string soundHash, CancellationToken ct = default)
    {
        var index = await LoadIndexAsync(ct).ConfigureAwait(false);
        return index.Find(s => s.SoundHash == soundHash);
    }

    public async Task<Sound> ExtractAndPublishAsync(
        string  videoFilePath,
        string  title,
        string? artistName       = null,
        string? originalReelHash = null,
        CancellationToken ct = default)
    {
        // In a full implementation, FFmpeg would demux audio only.
        // Here we publish the whole video file as the sound source and note it.
        _logger.LogInformation(
            "SoundLibrary: extracting audio from '{Path}' (full-file publish; demux via FFmpeg in production)",
            videoFilePath);

        return await PublishAudioFileAsync(videoFilePath, title, artistName, ct)
            .ConfigureAwait(false);
    }

    public async Task<Sound> PublishAudioFileAsync(
        string  audioFilePath,
        string  title,
        string? artistName = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audioFilePath);
        if (!File.Exists(audioFilePath))
            throw new FileNotFoundException("Audio file not found.", audioFilePath);

        var fileBytes   = await File.ReadAllBytesAsync(audioFilePath, ct).ConfigureAwait(false);
        var soundHash   = Convert.ToHexString(SHA256.HashData(fileBytes)).ToLowerInvariant();

        // Skip if already in library
        var existing = await GetAsync(soundHash, ct).ConfigureAwait(false);
        if (existing is not null)
            return existing;

        var descriptor = await _content.PublishAsync(
            name:            Path.GetFileName(audioFilePath),
            data:            fileBytes,
            contentType:     SoundContentType,
            cancellationToken: ct).ConfigureAwait(false);

        await _content.AnnounceAsync(descriptor, ct).ConfigureAwait(false);

        // Cache locally for immediate playback
        var cachePath = Path.Combine(_cacheDir, soundHash + Path.GetExtension(audioFilePath));
        if (!File.Exists(cachePath))
            await File.WriteAllBytesAsync(cachePath, fileBytes, ct).ConfigureAwait(false);

        var sound = new Sound(
            SoundHash:       soundHash,
            Title:           title,
            ArtistName:      artistName,
            OriginalReelHash: null,
            DurationMs:      0,       // populated by media info in production
            UseCount:        0);

        await ModifyIndexAsync(index =>
        {
            if (!index.Any(s => s.SoundHash == soundHash))
                index.Add(sound);
        }, ct).ConfigureAwait(false);

        _logger.LogInformation("SoundLibrary: published sound '{Title}' ({Hash})", title, soundHash);
        return sound;
    }

    public async Task<string> GetLocalPathAsync(string soundHash, CancellationToken ct = default)
    {
        // Check cache first
        var cached = Directory.EnumerateFiles(_cacheDir, soundHash + ".*").FirstOrDefault();
        if (cached is not null)
            return cached;

        // Fetch from content layer
        var data = await _content.AssembleAsync(soundHash, ct).ConfigureAwait(false);
        if (data is null)
            throw new InvalidOperationException($"Sound {soundHash} not available locally or from peers.");

        var cachePath = Path.Combine(_cacheDir, soundHash + ".audio");
        await File.WriteAllBytesAsync(cachePath, data, ct).ConfigureAwait(false);
        return cachePath;
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    private async Task<List<Sound>> LoadIndexAsync(CancellationToken ct)
    {
        if (!File.Exists(_indexPath)) return [];
        try
        {
            var json = await File.ReadAllTextAsync(_indexPath, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<List<Sound>>(json, JsonOpts) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SoundLibrary: failed to load index.");
            return [];
        }
    }

    private async Task ModifyIndexAsync(Action<List<Sound>> mutate, CancellationToken ct)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var index = await LoadIndexAsync(ct).ConfigureAwait(false);
            mutate(index);
            var tmp  = _indexPath + ".tmp";
            var json = JsonSerializer.Serialize(index, JsonOpts);
            await File.WriteAllTextAsync(tmp, json, ct).ConfigureAwait(false);
            File.Move(tmp, _indexPath, overwrite: true);
        }
        finally { _lock.Release(); }
    }
}
