// SPDX-License-Identifier: MIT
import { describe, it } from "node:test";
import { strict as assert } from "node:assert";
import { isNew, reactionTotal, toWire, fromWire } from "./MediaFeedItem.js";
import type { MediaFeedItem } from "./MediaFeedItem.js";
import { MediaReactionType } from "./MediaReaction.js";

// ── Helpers ───────────────────────────────────────────────────────────────────

const ONE_HOUR_MS = 60 * 60 * 1000;
const ONE_DAY_MS  = 24 * ONE_HOUR_MS;

function makeItem(overrides?: Partial<MediaFeedItem>): MediaFeedItem {
  return {
    content: {
      contentHash:   "h1",
      title:         "Test Content",
      durationMs:    60_000,
      codec:         "h264",
      contentType:   "video/mp4",
      creatorUhid:   "creator-001",
      sizeBytes:     1024,
      createdAtMs:   1_700_000_000_000,
      thumbnailHash: null,
      tags:          [],
    },
    likeCount:     10,
    shareCount:    5,
    commentCount:  3,
    watchCount:    100,
    isLive:        false,
    streamId:      null,
    topReactions:  [],
    publishedAtMs: Date.now(),
    ...overrides,
  };
}

// ── isNew ─────────────────────────────────────────────────────────────────────

describe("isNew", () => {
  it("returns true for an item published 1 hour ago", () => {
    const publishedAtMs = Date.now() - ONE_HOUR_MS;
    assert.ok(isNew(makeItem({ publishedAtMs })));
  });

  it("returns true for an item published 23 hours ago", () => {
    const publishedAtMs = Date.now() - 23 * ONE_HOUR_MS;
    assert.ok(isNew(makeItem({ publishedAtMs })));
  });

  it("returns false for an item published 25 hours ago", () => {
    const publishedAtMs = Date.now() - 25 * ONE_HOUR_MS;
    assert.ok(!isNew(makeItem({ publishedAtMs })));
  });

  it("returns false for an item published 48 hours ago", () => {
    const publishedAtMs = Date.now() - 2 * ONE_DAY_MS;
    assert.ok(!isNew(makeItem({ publishedAtMs })));
  });

  it("returns false for an item published 7 days ago", () => {
    const publishedAtMs = Date.now() - 7 * ONE_DAY_MS;
    assert.ok(!isNew(makeItem({ publishedAtMs })));
  });
});

// ── reactionTotal ─────────────────────────────────────────────────────────────

describe("reactionTotal", () => {
  it("returns sum of likes + shares + comments", () => {
    const item = makeItem({ likeCount: 10, shareCount: 5, commentCount: 3 });
    assert.equal(reactionTotal(item), 18);
  });

  it("returns 0 when all counts are 0", () => {
    const item = makeItem({ likeCount: 0, shareCount: 0, commentCount: 0 });
    assert.equal(reactionTotal(item), 0);
  });

  it("returns correct total when only likes are non-zero", () => {
    const item = makeItem({ likeCount: 42, shareCount: 0, commentCount: 0 });
    assert.equal(reactionTotal(item), 42);
  });

  it("returns correct total when only comments are non-zero", () => {
    const item = makeItem({ likeCount: 0, shareCount: 0, commentCount: 7 });
    assert.equal(reactionTotal(item), 7);
  });

  it("does not include watchCount in the total", () => {
    const item = makeItem({ likeCount: 1, shareCount: 1, commentCount: 1, watchCount: 9999 });
    assert.equal(reactionTotal(item), 3);
  });

  it("handles large counts correctly", () => {
    const item = makeItem({
      likeCount:    1_000_000,
      shareCount:   500_000,
      commentCount: 250_000,
    });
    assert.equal(reactionTotal(item), 1_750_000);
  });
});

// ── toWire / fromWire roundtrip ───────────────────────────────────────────────

describe("toWire / fromWire", () => {
  it("roundtrips a minimal feed item", () => {
    const item    = makeItem();
    const wire    = toWire(item);
    const restored = fromWire(wire);

    assert.equal(restored.likeCount,     item.likeCount);
    assert.equal(restored.shareCount,    item.shareCount);
    assert.equal(restored.commentCount,  item.commentCount);
    assert.equal(restored.watchCount,    item.watchCount);
    assert.equal(restored.isLive,        item.isLive);
    assert.equal(restored.streamId,      item.streamId);
    assert.equal(restored.publishedAtMs, item.publishedAtMs);
  });

  it("roundtrips the nested content", () => {
    const item    = makeItem();
    const restored = fromWire(toWire(item));

    assert.equal(restored.content.contentHash, item.content.contentHash);
    assert.equal(restored.content.title,       item.content.title);
    assert.equal(restored.content.durationMs,  item.content.durationMs);
    assert.equal(restored.content.createdAtMs, item.content.createdAtMs);
  });

  it("roundtrips topReactions", () => {
    const reaction = {
      reactionId:  "rxn-1",
      contentHash: "h1",
      fromUhid:    "viewer-1",
      type:        MediaReactionType.Like,
      positionMs:  1_000,
      message:     null,
      sentAtMs:    1_700_000_000_000,
    };
    const item    = makeItem({ topReactions: [reaction] });
    const restored = fromWire(toWire(item));

    assert.equal(restored.topReactions.length, 1);
    assert.equal(restored.topReactions[0].reactionId, "rxn-1");
    assert.equal(restored.topReactions[0].type,       MediaReactionType.Like);
  });

  it("toWire uses snake_case keys", () => {
    const wire = toWire(makeItem());
    assert.ok("like_count"      in wire);
    assert.ok("share_count"     in wire);
    assert.ok("comment_count"   in wire);
    assert.ok("watch_count"     in wire);
    assert.ok("is_live"         in wire);
    assert.ok("stream_id"       in wire);
    assert.ok("top_reactions"   in wire);
    assert.ok("published_at_ms" in wire);
    assert.ok("content"         in wire);
  });
});
