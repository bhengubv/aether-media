// Package feed provides a thread-safe in-memory feed aggregator.
package feed

import (
	"sync"

	"github.com/bhengubv/aether-media/go/models"
)

const feedCap = 500

// FeedAggregator stores up to 500 MediaFeedItems, newest-first.
// All methods are safe for concurrent use.
type FeedAggregator struct {
	items []models.MediaFeedItem
	mu    sync.RWMutex
}

// NewFeedAggregator creates an empty FeedAggregator.
func NewFeedAggregator() *FeedAggregator {
	return &FeedAggregator{items: make([]models.MediaFeedItem, 0, feedCap)}
}

// AddItem prepends item to the feed.  When at capacity the oldest item
// (the last element) is evicted.
func (a *FeedAggregator) AddItem(item models.MediaFeedItem) {
	a.mu.Lock()
	defer a.mu.Unlock()
	if len(a.items) >= feedCap {
		// Evict oldest (last element) by re-slicing then prepending
		a.items = a.items[:feedCap-1]
	}
	// Prepend: grow slice by one, shift everything right, put item at 0
	a.items = append(a.items, models.MediaFeedItem{}) // grow
	copy(a.items[1:], a.items[0:])                    // shift right
	a.items[0] = item
}

// GetFeed returns a copy of at most limit items starting at offset.
// Returns nil when offset is out of range.
func (a *FeedAggregator) GetFeed(limit, offset int) []models.MediaFeedItem {
	a.mu.RLock()
	defer a.mu.RUnlock()
	if offset >= len(a.items) || limit <= 0 {
		return nil
	}
	end := offset + limit
	if end > len(a.items) {
		end = len(a.items)
	}
	// Return a copy so callers cannot mutate internal state
	result := make([]models.MediaFeedItem, end-offset)
	copy(result, a.items[offset:end])
	return result
}

// MarkWatched records that the local user watched ms milliseconds of
// the content identified by contentHash.
func (a *FeedAggregator) MarkWatched(contentHash string, ms int64) {
	a.mu.Lock()
	defer a.mu.Unlock()
	for i := range a.items {
		if a.items[i].Content.ContentHash == contentHash {
			a.items[i].WatchedMs += ms
			a.items[i].WatchCount++
			return
		}
	}
}

// Len returns the number of items currently in the feed.
func (a *FeedAggregator) Len() int {
	a.mu.RLock()
	defer a.mu.RUnlock()
	return len(a.items)
}
