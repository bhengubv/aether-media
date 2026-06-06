// SPDX-License-Identifier: MIT

using System.Security.Cryptography;
using System.Text.Json;
using AetherMesh.Content;
using AetherMesh.Media.Reel.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AetherMesh.Media.Reel;

/// <summary>
/// Core Reel service — publishes to <c>IContentService</c>, maintains a local
/// metadata index, and fires events when Reels are published or received from
/// mesh peers.
/// </summary>
public class ReelService : IReelService
{
    private const string ReelContentType = "video/aether-reel";
    private const long   MaxDurationMs   = 60_000;

    private readonly IContentService   _content;
    private readonly IReelDiscovery    _discovery;
    private readonly string            _localUhid;
    private readonly string            _metaPath;
    private readonly SemaphoreSlim     _lock = new(1, 1);
    private readonly ILogger<ReelService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    // ── Events ────────────────────────────────────────────────────────────────

    public event EventHandler<Reel>?        ReelPublished;
    public event EventHandler<Reel>?        ReelReceived;
    public event EventHandler<ReelComment>? CommentReceived;

    /// <summary>Called by the mesh integration layer when a Reel arrives from a peer.</summary>
    internal void OnReelReceived(Reel reel)           => ReelReceived?.Invoke(this, reel);

    /// <summary>Called by the mesh integration layer when a comment arrives from a peer.</summary>
    internal void OnCommentReceived(ReelComment comment) => CommentReceived?.Invoke(this, comment);

    // ── Constructor ───────────────────────────────────────────────────────────

    public ReelService(
        IContentService   content,
        IReelDiscovery    discovery,
        string            localUhid,
        ILogger<ReelService>? logger = null)
        : this(content, discovery, localUhid, dataDirectory: null, logger) { }

    // Protected constructor allows test subclasses to inject a temp directory.
    protected ReelService(
        IContentService       content,
        IReelDiscovery        discovery,
        string                localUhid,
        string?               dataDirectory,
        ILogger<ReelService>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localUhid);
        _content   = content   ?? throw new ArgumentNullException(nameof(content));
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _localUhid = localUhid;
        _logger    = logger ?? NullLogger<ReelService>.Instance;

        var dir = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "aether-media");
        Directory.CreateDirectory(dir);
        _metaPath = Path.Combine(dir, "reel-meta.json");
    }

    // ── Publishing ────────────────────────────────────────────────────────────

    public async Task<Reel> PublishAsync(
        string   videoFilePath,
        string?  soundHash,
        string[] hashtags,
        string   title = "",
        CancellationToken ct = default)
        => await PublishInternalAsync(videoFilePath, soundHash, hashtags, title,
               ReelType.Original, sourceReelHash: null, ct).ConfigureAwait(false);

    public async Task<Reel> PublishDuetAsync(
        string videoFilePath,
        string sourceReelHash,
        string[] hashtags = default!,
        CancellationToken ct = default)
        => await PublishInternalAsync(videoFilePath, soundHash: null,
               hashtags ?? [], title: "",
               ReelType.Duet, sourceReelHash, ct).ConfigureAwait(false);

    public async Task<Reel> PublishStitchAsync(
        string videoFilePath,
        string sourceReelHash,
        string[] hashtags = default!,
        CancellationToken ct = default)
        => await PublishInternalAsync(videoFilePath, soundHash: null,
               hashtags ?? [], title: "",
               ReelType.Stitch, sourceReelHash, ct).ConfigureAwait(false);

    // ── Retrieval ─────────────────────────────────────────────────────────────

    public async Task<Reel?> GetAsync(string contentHash, CancellationToken ct = default)
    {
        var meta = await LoadMetaAsync(ct).ConfigureAwait(false);
        return meta.Reels.Find(r => r.ContentHash == contentHash);
    }

    public async Task<IReadOnlyList<Reel>> GetByCreatorAsync(
        string creatorUhid,
        CancellationToken ct = default)
    {
        var meta = await LoadMetaAsync(ct).ConfigureAwait(false);
        return meta.Reels
            .Where(r => r.CreatorUhid == creatorUhid)
            .OrderByDescending(r => r.CreatedAtMs)
            .ToList();
    }

    // ── Interactions ──────────────────────────────────────────────────────────

    public async Task LikeAsync(string contentHash, CancellationToken ct = default)
        => await ModifyMetaAsync(m => m.Likes.Add(contentHash), ct).ConfigureAwait(false);

    public async Task UnlikeAsync(string contentHash, CancellationToken ct = default)
        => await ModifyMetaAsync(m => m.Likes.Remove(contentHash), ct).ConfigureAwait(false);

    public async Task<bool> IsLikedAsync(string contentHash, CancellationToken ct = default)
    {
        var meta = await LoadMetaAsync(ct).ConfigureAwait(false);
        return meta.Likes.Contains(contentHash);
    }

    public async Task BookmarkAsync(string contentHash, CancellationToken ct = default)
        => await ModifyMetaAsync(m => m.Bookmarks.Add(contentHash), ct).ConfigureAwait(false);

    public async Task UnbookmarkAsync(string contentHash, CancellationToken ct = default)
        => await ModifyMetaAsync(m => m.Bookmarks.Remove(contentHash), ct).ConfigureAwait(false);

    public async Task<bool> IsBookmarkedAsync(string contentHash, CancellationToken ct = default)
    {
        var meta = await LoadMetaAsync(ct).ConfigureAwait(false);
        return meta.Bookmarks.Contains(contentHash);
    }

    // ── Comments ──────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ReelComment>> GetCommentsAsync(
        string contentHash,
        CancellationToken ct = default)
    {
        var meta = await LoadMetaAsync(ct).ConfigureAwait(false);
        return meta.Comments
            .Where(c => c.ReelHash == contentHash)
            .OrderBy(c => c.CreatedAtMs)
            .ToList();
    }

    public async Task<ReelComment> AddCommentAsync(
        string  contentHash,
        string  text,
        string? parentCommentId = null,
        CancellationToken ct = default)
    {
        var comment = new ReelComment(
            CommentId:       Guid.NewGuid().ToString("N"),
            ReelHash:        contentHash,
            AuthorUhid:      _localUhid,
            Text:            text,
            CreatedAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ParentCommentId: parentCommentId);

        await ModifyMetaAsync(m => m.Comments.Add(comment), ct).ConfigureAwait(false);
        return comment;
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<Reel> PublishInternalAsync(
        string    videoFilePath,
        string?   soundHash,
        string[]  hashtags,
        string    title,
        ReelType  type,
        string?   sourceReelHash,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(videoFilePath);
        if (!File.Exists(videoFilePath))
            throw new FileNotFoundException("Reel video file not found.", videoFilePath);

        var fileBytes = await File.ReadAllBytesAsync(videoFilePath, ct).ConfigureAwait(false);
        var contentHash = Convert.ToHexString(SHA256.HashData(fileBytes)).ToLowerInvariant();

        // Publish chunks to the content layer
        var descriptor = await _content.PublishAsync(
            name:            Path.GetFileName(videoFilePath),
            data:            fileBytes,
            contentType:     ReelContentType,
            cancellationToken: ct).ConfigureAwait(false);

        await _content.AnnounceAsync(descriptor, ct).ConfigureAwait(false);

        // Approximate duration from file size (placeholder — a real implementation
        // would use a media info library or rely on the video metadata passed by the caller).
        var durationMs = Math.Min(MaxDurationMs, fileBytes.Length / 250);   // ~250 B/ms at 2 Mbps

        var reel = new Reel(
            ContentHash:    contentHash,
            CreatorUhid:    _localUhid,
            Title:          title,
            DurationMs:     Math.Max(1, durationMs),
            SoundHash:      soundHash,
            SoundTitle:     null,
            Hashtags:       hashtags.Select(h => h.TrimStart('#').ToLowerInvariant()).ToArray(),
            Type:           type,
            SourceReelHash: sourceReelHash,
            ThumbnailHash:  null,
            CreatedAtMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ViewCount:      0,
            LikeCount:      0);

        await ModifyMetaAsync(m =>
        {
            if (!m.Reels.Any(r => r.ContentHash == reel.ContentHash))
                m.Reels.Insert(0, reel);
        }, ct).ConfigureAwait(false);

        await _discovery.AnnounceReelAsync(reel, ct).ConfigureAwait(false);

        _logger.LogInformation("Reel published: {Hash} ({Type})", contentHash, type);
        ReelPublished?.Invoke(this, reel);
        return reel;
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    private async Task<ReelMeta> LoadMetaAsync(CancellationToken ct)
    {
        if (!File.Exists(_metaPath)) return new ReelMeta();
        try
        {
            var json = await File.ReadAllTextAsync(_metaPath, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<ReelMeta>(json, JsonOpts) ?? new ReelMeta();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ReelService: failed to load meta, starting fresh.");
            return new ReelMeta();
        }
    }

    private async Task ModifyMetaAsync(Action<ReelMeta> mutate, CancellationToken ct)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var meta = await LoadMetaAsync(ct).ConfigureAwait(false);
            mutate(meta);
            var tmp  = _metaPath + ".tmp";
            var json = JsonSerializer.Serialize(meta, JsonOpts);
            await File.WriteAllTextAsync(tmp, json, ct).ConfigureAwait(false);
            File.Move(tmp, _metaPath, overwrite: true);
        }
        finally { _lock.Release(); }
    }

    private sealed class ReelMeta
    {
        public List<Reel>        Reels     { get; set; } = [];
        public HashSet<string>   Likes     { get; set; } = [];
        public HashSet<string>   Bookmarks { get; set; } = [];
        public List<ReelComment> Comments  { get; set; } = [];
    }
}
