import type { MediaContent, MediaContentWire } from "./MediaContent.js";
import type { MediaReaction, MediaReactionWire } from "./MediaReaction.js";
import { toWire as contentToWire, fromWire as contentFromWire } from "./MediaContent.js";
import { toWire as reactionToWire, fromWire as reactionFromWire } from "./MediaReaction.js";

export interface MediaFeedItem {
  content: MediaContent;
  likeCount: number;
  shareCount: number;
  commentCount: number;
  watchCount: number;
  isLive: boolean;
  streamId: string | null;
  topReactions: MediaReaction[];
  publishedAtMs: number;   // unix milliseconds
}

// ── Wire format ───────────────────────────────────────────────────────────────

/** Snake_case JSON representation of MediaFeedItem as sent/received on the wire. */
export interface MediaFeedItemWire {
  content: MediaContentWire;
  like_count: number;
  share_count: number;
  comment_count: number;
  watch_count: number;
  is_live: boolean;
  stream_id: string | null;
  top_reactions: MediaReactionWire[];
  published_at_ms: number;
}

export function toWire(item: MediaFeedItem): MediaFeedItemWire {
  return {
    content:         contentToWire(item.content),
    like_count:      item.likeCount,
    share_count:     item.shareCount,
    comment_count:   item.commentCount,
    watch_count:     item.watchCount,
    is_live:         item.isLive,
    stream_id:       item.streamId,
    top_reactions:   item.topReactions.map(reactionToWire),
    published_at_ms: item.publishedAtMs,
  };
}

export function fromWire(obj: MediaFeedItemWire): MediaFeedItem {
  return {
    content:       contentFromWire(obj.content),
    likeCount:     obj.like_count,
    shareCount:    obj.share_count,
    commentCount:  obj.comment_count,
    watchCount:    obj.watch_count,
    isLive:        obj.is_live,
    streamId:      obj.stream_id,
    topReactions:  obj.top_reactions.map(reactionFromWire),
    publishedAtMs: obj.published_at_ms,
  };
}

// ── Helpers ───────────────────────────────────────────────────────────────────

/**
 * Returns true when the feed item was published within the last 24 hours.
 */
export function isNew(item: MediaFeedItem): boolean {
  const ageMs = Date.now() - item.publishedAtMs;
  return ageMs < 24 * 60 * 60 * 1000;
}

/**
 * Sum of likes + shares + comments.
 * SuperReacts are counted in the like bucket server-side and are not
 * separately tallied here.
 */
export function reactionTotal(item: MediaFeedItem): number {
  return item.likeCount + item.shareCount + item.commentCount;
}
