use crate::models::MediaContent;

/// Maximum number of items the feed keeps in memory.
pub const FEED_CAP: usize = 500;

/// An aggregated feed item stored in the FeedAggregator.
#[derive(Debug, Clone)]
pub struct MediaFeedItem {
    pub content: MediaContent,
    pub like_count: u32,
    pub share_count: u32,
    pub comment_count: u32,
    pub watch_count: u32,
    pub is_live: bool,
    pub stream_id: Option<String>,
    pub published_at_ms: u64,
    /// How many milliseconds the local user has watched this content.
    pub watched_ms: u64,
}

/// In-memory feed aggregator capped at [`FEED_CAP`] items.
///
/// Items are stored newest-first (index 0 = most recent).
/// When the cap is reached the oldest item (last in the Vec) is evicted.
pub struct FeedAggregator {
    items: Vec<MediaFeedItem>,
}

impl FeedAggregator {
    pub fn new() -> Self {
        Self { items: Vec::with_capacity(FEED_CAP) }
    }

    /// Add a new feed item.  If the feed is at capacity the oldest item
    /// (the last element) is removed to make room.
    pub fn add_item(&mut self, item: MediaFeedItem) {
        if self.items.len() >= FEED_CAP {
            self.items.pop(); // evict oldest
        }
        // Insert at front so items are newest-first
        self.items.insert(0, item);
    }

    /// Return a slice of at most `limit` items starting at `offset`.
    /// Returns an empty slice when offset >= total items.
    pub fn get_feed(&self, limit: usize, offset: usize) -> &[MediaFeedItem] {
        if offset >= self.items.len() {
            return &[];
        }
        let end = (offset + limit).min(self.items.len());
        &self.items[offset..end]
    }

    /// Record that the local user watched `ms` milliseconds of the given
    /// content.  Adds to the existing watched_ms counter for that content.
    pub fn mark_watched(&mut self, content_hash: &str, ms: u64) {
        for item in self.items.iter_mut() {
            if item.content.content_hash == content_hash {
                item.watched_ms = item.watched_ms.saturating_add(ms);
                item.watch_count = item.watch_count.saturating_add(1);
                return;
            }
        }
    }

    pub fn len(&self) -> usize {
        self.items.len()
    }

    pub fn is_empty(&self) -> bool {
        self.items.is_empty()
    }
}

impl Default for FeedAggregator {
    fn default() -> Self {
        Self::new()
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::models::MediaContent;

    fn mock_item(hash: &str) -> MediaFeedItem {
        MediaFeedItem {
            content: MediaContent {
                content_hash: hash.to_owned(),
                title: format!("Title {hash}"),
                duration_ms: 60_000,
                codec: "h264".into(),
                content_type: "video/mp4".into(),
                creator_uhid: "u1".into(),
                size_bytes: 10_000,
                created_at_ms: 1_700_000_000_000,
                thumbnail_hash: None,
                tags: vec![],
            },
            like_count: 0,
            share_count: 0,
            comment_count: 0,
            watch_count: 0,
            is_live: false,
            stream_id: None,
            published_at_ms: 0,
            watched_ms: 0,
        }
    }

    #[test]
    fn add_and_get() {
        let mut f = FeedAggregator::new();
        f.add_item(mock_item("a"));
        f.add_item(mock_item("b"));
        assert_eq!(f.len(), 2);
        let page = f.get_feed(10, 0);
        // newest-first: "b" was added last → should be at index 0
        assert_eq!(page[0].content.content_hash, "b");
        assert_eq!(page[1].content.content_hash, "a");
    }

    #[test]
    fn pagination() {
        let mut f = FeedAggregator::new();
        for i in 0..10 {
            f.add_item(mock_item(&i.to_string()));
        }
        let page = f.get_feed(3, 2);
        assert_eq!(page.len(), 3);
    }

    #[test]
    fn offset_beyond_end_returns_empty() {
        let mut f = FeedAggregator::new();
        f.add_item(mock_item("x"));
        let page = f.get_feed(10, 99);
        assert!(page.is_empty());
    }

    #[test]
    fn cap_evicts_oldest() {
        let mut f = FeedAggregator::new();
        for i in 0..=FEED_CAP {
            f.add_item(mock_item(&i.to_string()));
        }
        assert_eq!(f.len(), FEED_CAP);
        // The oldest item (hash "0") should have been evicted
        let all = f.get_feed(FEED_CAP, 0);
        let hashes: Vec<&str> = all.iter().map(|i| i.content.content_hash.as_str()).collect();
        assert!(!hashes.contains(&"0"));
    }

    #[test]
    fn mark_watched_accumulates() {
        let mut f = FeedAggregator::new();
        f.add_item(mock_item("vid1"));
        f.mark_watched("vid1", 30_000);
        f.mark_watched("vid1", 15_000);
        let item = &f.get_feed(1, 0)[0];
        assert_eq!(item.watched_ms, 45_000);
        assert_eq!(item.watch_count, 2);
    }
}
