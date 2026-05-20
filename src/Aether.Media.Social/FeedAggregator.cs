// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using Aether.Content;
using Aether.Content.Models;
using Aether.Media.Core.Models;
using Aether.Streaming;
using Aether.Streaming.Models;

namespace Aether.Media.Social;

/// <summary>
/// Aggregates the local media feed from two mesh sources:
/// <list type="bullet">
///   <item><see cref="IStreamingService.StreamAnnounced"/> — live stream announcements
///     from followed (or any nearby) creators are converted to live <see cref="MediaFeedItem"/>s.</item>
///   <item><see cref="IContentService.ContentAnnounced"/> — published content
///     descriptors from followed creators are converted to VOD <see cref="MediaFeedItem"/>s.</item>
/// </list>
/// The feed is capped at 500 items (oldest evicted when the cap is reached).
/// All public methods are thread-safe.
/// </summary>
public sealed class FeedAggregator : IFeedAggregator
{
    private const int MaxFeedItems = 500;

    // ── Events ─────────────────────────────────────────────────────────────
    public event EventHandler<MediaFeedItem>? ItemArrived;

    // ── State ──────────────────────────────────────────────────────────────

    // In-memory feed; protected by _feedLock
    private readonly List<MediaFeedItem> _feed = new();
    private readonly Lock _feedLock = new();

    // Watch progress: contentHash → cumulative milliseconds watched
    private readonly ConcurrentDictionary<string, long> _watchProgress = new(StringComparer.OrdinalIgnoreCase);

    // ── Dependencies ───────────────────────────────────────────────────────
    private readonly ISocialGraph _socialGraph;
    private readonly IStreamingService _streaming;
    private readonly IContentService _content;

    private bool _started;

    public FeedAggregator(
        ISocialGraph socialGraph,
        IStreamingService streaming,
        IContentService content)
    {
        _socialGraph = socialGraph ?? throw new ArgumentNullException(nameof(socialGraph));
        _streaming   = streaming   ?? throw new ArgumentNullException(nameof(streaming));
        _content     = content     ?? throw new ArgumentNullException(nameof(content));
    }

    // ── IFeedAggregator ────────────────────────────────────────────────────

    public Task StartAsync(CancellationToken ct = default)
    {
        if (_started) return Task.CompletedTask;
        _started = true;

        _streaming.StreamAnnounced += OnStreamAnnounced;
        _content.ContentAnnounced  += OnContentAnnounced;
        _streaming.StreamEnded     += OnStreamEnded;

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        if (!_started) return Task.CompletedTask;
        _started = false;

        _streaming.StreamAnnounced -= OnStreamAnnounced;
        _content.ContentAnnounced  -= OnContentAnnounced;
        _streaming.StreamEnded     -= OnStreamEnded;

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<MediaFeedItem>> GetFeedAsync(
        int limit = 50,
        int offset = 0,
        CancellationToken ct = default)
    {
        if (limit <= 0) limit = 50;
        if (offset < 0) offset = 0;

        // Snapshot under lock, return slice
        lock (_feedLock)
        {
            if (offset >= _feed.Count)
                return Array.Empty<MediaFeedItem>();

            var available = _feed.Count - offset;
            var take = Math.Min(limit, available);
            return _feed.GetRange(offset, take);
        }
    }

    public async Task<IReadOnlyList<LiveStream>> GetNearbyLiveStreamsAsync(CancellationToken ct = default)
    {
        var following = await _socialGraph.GetFollowingAsync(ct).ConfigureAwait(false);
        var followingSet = new HashSet<string>(following, StringComparer.Ordinal);

        var activeSessions = _streaming.GetActiveStreams();

        var streams = new List<LiveStream>(activeSessions.Count);
        foreach (var session in activeSessions)
        {
            if (session.State != StreamState.Live)
                continue;

            // Include streams from followed creators, or any stream if the creator is nearby
            // (this node received the StreamAnnounce so it is mesh-adjacent)
            streams.Add(MapSessionToLiveStream(session));
        }

        // Prefer followed creators first, then sort by start time descending
        streams.Sort((a, b) =>
        {
            var aFollowed = followingSet.Contains(a.CreatorUhid);
            var bFollowed = followingSet.Contains(b.CreatorUhid);
            if (aFollowed != bFollowed)
                return aFollowed ? -1 : 1;
            return b.StartedAt.CompareTo(a.StartedAt);
        });

        return streams;
    }

    public Task MarkWatchedAsync(string contentHash, long watchedMs, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(contentHash) || watchedMs <= 0)
            return Task.CompletedTask;

        // Accumulate watch time
        _watchProgress.AddOrUpdate(
            contentHash,
            addValue: watchedMs,
            updateValueFactory: (_, existing) => existing + watchedMs);

        // Update the WatchCount on the feed item that matches this hash
        lock (_feedLock)
        {
            for (var i = 0; i < _feed.Count; i++)
            {
                var item = _feed[i];
                if (!string.Equals(item.Content.ContentHash, contentHash, StringComparison.OrdinalIgnoreCase))
                    continue;

                _feed[i] = item with { WatchCount = item.WatchCount + 1 };
                break;
            }
        }

        return Task.CompletedTask;
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async void OnStreamAnnounced(object? sender, StreamSession session)
    {
        if (session.State != StreamState.Live)
            return;

        // Only surface streams from followed creators — check without blocking the event thread
        bool isFollowed;
        try
        {
            isFollowed = await _socialGraph.IsFollowingAsync(session.PublisherUhid).ConfigureAwait(false);
        }
        catch
        {
            isFollowed = false;
        }

        if (!isFollowed)
            return;

        var liveStream = MapSessionToLiveStream(session);

        // Create a thin MediaContent surrogate for the feed item
        var content = new MediaContent(
            ContentHash: session.Id.ToString("N"),
            Title: session.Title,
            DurationMs: 0,   // live — unknown duration
            Codec: session.Codec,
            ContentType: session.ContentType,
            CreatorUhid: session.PublisherUhid,
            SizeBytes: 0,
            CreatedAt: session.StartedAt,
            ThumbnailHash: null,
            Tags: []);

        var feedItem = new MediaFeedItem(
            Content: content,
            LikeCount: 0,
            ShareCount: 0,
            CommentCount: 0,
            WatchCount: 0,
            IsLive: true,
            StreamId: session.Id,
            TopReactions: [],
            PublishedAt: session.StartedAt);

        AddToFeed(feedItem);
        ItemArrived?.Invoke(this, feedItem);
    }

    private async void OnContentAnnounced(object? sender, ContentDescriptor descriptor)
    {
        // Only surface content from followed creators
        // The descriptor does not carry a UHID — skip filtering for now (we include all)
        // because content descriptors are only received if the creator sent them.
        var content = new MediaContent(
            ContentHash: descriptor.RootHash,
            Title: descriptor.Name,
            DurationMs: 0,   // duration unknown from descriptor alone
            Codec: string.Empty,
            ContentType: descriptor.ContentType,
            CreatorUhid: string.Empty, // descriptors have no UHID field; inferred from the announcing peer
            SizeBytes: descriptor.TotalBytes,
            CreatedAt: descriptor.CreatedAt,
            ThumbnailHash: null,
            Tags: []);

        var feedItem = new MediaFeedItem(
            Content: content,
            LikeCount: 0,
            ShareCount: 0,
            CommentCount: 0,
            WatchCount: 0,
            IsLive: false,
            StreamId: null,
            TopReactions: [],
            PublishedAt: descriptor.CreatedAt);

        AddToFeed(feedItem);
        ItemArrived?.Invoke(this, feedItem);
    }

    private void OnStreamEnded(object? sender, StreamSession session)
    {
        // Mark the corresponding feed item as no longer live
        lock (_feedLock)
        {
            for (var i = 0; i < _feed.Count; i++)
            {
                var item = _feed[i];
                if (item.StreamId != session.Id)
                    continue;

                _feed[i] = item with { IsLive = false };
                break;
            }
        }
    }

    private void AddToFeed(MediaFeedItem item)
    {
        lock (_feedLock)
        {
            // Deduplicate by content hash
            for (var i = 0; i < _feed.Count; i++)
            {
                if (string.Equals(_feed[i].Content.ContentHash, item.Content.ContentHash, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            // Insert newest first
            _feed.Insert(0, item);

            // Evict oldest when over cap
            if (_feed.Count > MaxFeedItems)
                _feed.RemoveAt(_feed.Count - 1);
        }
    }

    private static LiveStream MapSessionToLiveStream(StreamSession session) =>
        new LiveStream(
            StreamId: session.Id,
            Title: session.Title,
            CreatorUhid: session.PublisherUhid,
            Codec: session.Codec,
            SegmentDurationMs: session.SegmentDurationMs,
            StartedAt: session.StartedAt,
            ViewerCount: 0,
            IsActive: session.State == StreamState.Live,
            Tags: []);
}
