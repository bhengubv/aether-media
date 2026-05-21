package feed_test

import (
	"testing"
	"time"

	"github.com/bhengubv/aether-media/go/feed"
	"github.com/bhengubv/aether-media/go/models"
)

// ── helpers ──────────────────────────────────────────────────────────────────

func makeItem(hash string) models.MediaFeedItem {
	return models.MediaFeedItem{
		Content: models.MediaContent{
			ContentHash: hash,
			Title:       "Test " + hash,
			DurationMs:  1000,
			ContentType: "video/mp4",
			CreatorUHID: "u1",
			SizeBytes:   1000,
			CreatedAtMs: time.Now().UnixMilli(),
		},
		LikeCount:     0,
		PublishedAtMs: time.Now().UnixMilli(),
	}
}

// ── Len ──────────────────────────────────────────────────────────────────────

func TestLen_StartsAtZero(t *testing.T) {
	a := feed.NewFeedAggregator()
	if a.Len() != 0 {
		t.Fatalf("expected 0, got %d", a.Len())
	}
}

// ── AddItem / Len ─────────────────────────────────────────────────────────────

func TestAddItem_IncreasesLen(t *testing.T) {
	a := feed.NewFeedAggregator()
	a.AddItem(makeItem("h1"))
	a.AddItem(makeItem("h2"))
	if a.Len() != 2 {
		t.Fatalf("expected len 2, got %d", a.Len())
	}
}

func TestAddItem_NewestFirst(t *testing.T) {
	a := feed.NewFeedAggregator()
	a.AddItem(makeItem("first"))
	a.AddItem(makeItem("second"))

	page := a.GetFeed(2, 0)
	if len(page) != 2 {
		t.Fatalf("expected 2 items, got %d", len(page))
	}
	if page[0].Content.ContentHash != "second" {
		t.Fatalf("expected newest first, got %q", page[0].Content.ContentHash)
	}
	if page[1].Content.ContentHash != "first" {
		t.Fatalf("expected oldest last, got %q", page[1].Content.ContentHash)
	}
}

// ── GetFeed ───────────────────────────────────────────────────────────────────

func TestGetFeed_LimitCaps(t *testing.T) {
	a := feed.NewFeedAggregator()
	for i := 0; i < 5; i++ {
		a.AddItem(makeItem("h"))
	}
	page := a.GetFeed(3, 0)
	if len(page) != 3 {
		t.Fatalf("expected 3, got %d", len(page))
	}
}

func TestGetFeed_OffsetSkips(t *testing.T) {
	a := feed.NewFeedAggregator()
	a.AddItem(makeItem("a"))
	a.AddItem(makeItem("b"))
	a.AddItem(makeItem("c"))

	page := a.GetFeed(10, 1) // skip newest
	if len(page) != 2 {
		t.Fatalf("expected 2, got %d", len(page))
	}
}

func TestGetFeed_OutOfRangeOffset_ReturnsNil(t *testing.T) {
	a := feed.NewFeedAggregator()
	a.AddItem(makeItem("a"))
	if page := a.GetFeed(10, 99); page != nil {
		t.Fatal("expected nil for out-of-range offset")
	}
}

func TestGetFeed_ZeroLimit_ReturnsNil(t *testing.T) {
	a := feed.NewFeedAggregator()
	a.AddItem(makeItem("a"))
	if page := a.GetFeed(0, 0); page != nil {
		t.Fatal("expected nil for zero limit")
	}
}

func TestGetFeed_ReturnsCopy_NotReference(t *testing.T) {
	a := feed.NewFeedAggregator()
	a.AddItem(makeItem("x"))

	page := a.GetFeed(1, 0)
	page[0].LikeCount = 999 // mutate the copy

	page2 := a.GetFeed(1, 0)
	if page2[0].LikeCount == 999 {
		t.Fatal("GetFeed must return a copy, not a reference")
	}
}

// ── MarkWatched ───────────────────────────────────────────────────────────────

func TestMarkWatched_RecordsMs(t *testing.T) {
	a := feed.NewFeedAggregator()
	a.AddItem(makeItem("abc"))
	a.MarkWatched("abc", 30_000)

	page := a.GetFeed(1, 0)
	if page[0].WatchedMs != 30_000 {
		t.Fatalf("expected WatchedMs 30000, got %d", page[0].WatchedMs)
	}
	if page[0].WatchCount != 1 {
		t.Fatalf("expected WatchCount 1, got %d", page[0].WatchCount)
	}
}

func TestMarkWatched_Accumulates(t *testing.T) {
	a := feed.NewFeedAggregator()
	a.AddItem(makeItem("abc"))
	a.MarkWatched("abc", 10_000)
	a.MarkWatched("abc", 20_000)

	page := a.GetFeed(1, 0)
	if page[0].WatchedMs != 30_000 {
		t.Fatalf("expected accumulated 30000, got %d", page[0].WatchedMs)
	}
	if page[0].WatchCount != 2 {
		t.Fatalf("expected WatchCount 2, got %d", page[0].WatchCount)
	}
}

func TestMarkWatched_UnknownHash_Noop(t *testing.T) {
	a := feed.NewFeedAggregator()
	a.AddItem(makeItem("known"))
	a.MarkWatched("unknown", 5_000) // must not panic

	page := a.GetFeed(1, 0)
	if page[0].WatchedMs != 0 {
		t.Fatalf("known item must be unaffected, got WatchedMs %d", page[0].WatchedMs)
	}
}
