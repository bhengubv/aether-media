import type { MediaContent } from "./MediaContent.js";
import type { MediaReaction } from "./MediaReaction.js";

export interface MediaFeedItem {
  content: MediaContent;
  likeCount: number;
  shareCount: number;
  commentCount: number;
  watchCount: number;
  isLive: boolean;
  streamId: string | null;
  topReactions: MediaReaction[];
  publishedAt: Date;
}

/**
 * Returns true when the feed item was published within the last 24 hours.
 */
export function isNew(item: MediaFeedItem): boolean {
  const ageMs = Date.now() - item.publishedAt.getTime();
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
