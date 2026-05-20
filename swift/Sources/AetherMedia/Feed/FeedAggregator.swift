import Foundation

/// Thread-safe in-memory feed aggregator capped at 500 items.
///
/// All mutations and reads are serialised on the actor's executor.
/// Items are stored newest-first (index 0 = most recent).
public actor FeedAggregator {
    private static let cap = 500
    private var items: [MediaFeedItem] = []

    public init() {}

    /// Prepend `item` to the feed.  When at capacity the oldest item
    /// (last element) is evicted.
    public func addItem(_ item: MediaFeedItem) {
        if items.count >= FeedAggregator.cap {
            items.removeLast()
        }
        items.insert(item, at: 0)
    }

    /// Return a copy of at most `limit` items starting at `offset`.
    /// Returns an empty array when `offset` >= total count or `limit` <= 0.
    public func getFeed(limit: Int, offset: Int) -> [MediaFeedItem] {
        guard limit > 0, offset < items.count else { return [] }
        let end = min(offset + limit, items.count)
        return Array(items[offset..<end])
    }

    /// Record that the local user watched `ms` milliseconds of the content
    /// identified by `contentHash`.  Accumulates into watchedMs.
    public func markWatched(contentHash: String, ms: Int64) {
        guard ms >= 0 else { return }
        guard let idx = items.firstIndex(where: { $0.content.contentHash == contentHash }) else { return }
        let old = items[idx]
        items[idx] = MediaFeedItem(
            content:      old.content,
            likeCount:    old.likeCount,
            shareCount:   old.shareCount,
            commentCount: old.commentCount,
            watchCount:   old.watchCount + 1,
            isLive:       old.isLive,
            streamId:     old.streamId,
            topReactions: old.topReactions,
            publishedAt:  old.publishedAt,
            watchedMs:    old.watchedMs + ms
        )
    }

    /// Number of items in the feed.
    public var count: Int { items.count }
}
