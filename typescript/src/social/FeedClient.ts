import type { MediaFeedItem, MediaFeedItemWire } from "../models/MediaFeedItem.js";
import { fromWire } from "../models/MediaFeedItem.js";

const CACHE_KEY_PREFIX = "aethernet_feed_";
const WATCHED_KEY      = "aethernet_watched";
const CACHE_TTL_MS     = 5 * 60 * 1000; // 5 minutes

interface CacheEntry {
  fetchedAt: number;
  items: MediaFeedItem[];
}

interface WatchedEntry {
  watchedMs: number;
  lastWatchedAt: number;
}

/**
 * Fetches and caches MediaFeedItems from the Aether media relay API.
 *
 * Pagination uses limit/offset.  Results are stored in localStorage with a
 * 5-minute TTL so repeated requests within a session avoid round-trips.
 */
export class FeedClient {
  private readonly baseUrl: string;
  private readonly memCache = new Map<string, CacheEntry>();

  constructor(baseUrl: string = "https://relay.aethernet.network/media") {
    this.baseUrl = baseUrl.replace(/\/$/, "");
  }

  /**
   * Fetch a page of feed items.  Returns cached items if they are fresh.
   */
  async getFeed(limit: number = 20, offset: number = 0): Promise<MediaFeedItem[]> {
    const key = `${CACHE_KEY_PREFIX}${limit}_${offset}`;

    // Check memory cache first (fastest)
    const memEntry = this.memCache.get(key);
    if (memEntry && Date.now() - memEntry.fetchedAt < CACHE_TTL_MS) {
      return memEntry.items;
    }

    // Check localStorage
    try {
      const raw = localStorage.getItem(key);
      if (raw) {
        const entry: CacheEntry = JSON.parse(raw);
        if (Date.now() - entry.fetchedAt < CACHE_TTL_MS) {
          // Items stored in cache are already MediaFeedItem (camelCase, number timestamps)
          this.memCache.set(key, { fetchedAt: entry.fetchedAt, items: entry.items });
          return entry.items;
        }
      }
    } catch {
      // localStorage unavailable or corrupt — proceed to fetch
    }

    // Fetch from network
    const url = `${this.baseUrl}/feed?limit=${limit}&offset=${offset}`;
    const response = await fetch(url, { headers: { Accept: "application/json" } });
    if (!response.ok) {
      throw new Error(`Feed fetch failed: ${response.status} ${response.statusText}`);
    }

    const items: MediaFeedItem[] = (await response.json() as MediaFeedItemWire[]).map(fromWire);
    const entry: CacheEntry = { fetchedAt: Date.now(), items };

    this.memCache.set(key, entry);
    try {
      localStorage.setItem(key, JSON.stringify(entry));
    } catch {
      // ignore quota errors
    }

    return items;
  }

  /**
   * Fetch live streams sorted by proximity (relay-side geo-scoring).
   */
  async getNearbyStreams(): Promise<MediaFeedItem[]> {
    const url = `${this.baseUrl}/streams/nearby`;
    const response = await fetch(url, { headers: { Accept: "application/json" } });
    if (!response.ok) {
      throw new Error(`Nearby streams fetch failed: ${response.status} ${response.statusText}`);
    }
    const items: MediaFeedItem[] = (await response.json() as MediaFeedItemWire[]).map(fromWire);
    return items;
  }

  /**
   * Record that the viewer watched a piece of content for watchedMs milliseconds.
   * Persisted to localStorage and also POSTed to the relay for server-side stats.
   */
  async markWatched(contentHash: string, watchedMs: number): Promise<void> {
    if (!contentHash.trim()) throw new Error("contentHash must not be empty");
    if (watchedMs < 0)       throw new Error("watchedMs must be >= 0");

    // Persist locally
    let watched: Record<string, WatchedEntry> = {};
    try {
      const raw = localStorage.getItem(WATCHED_KEY);
      if (raw) watched = JSON.parse(raw);
    } catch {
      // ignore
    }

    const existing = watched[contentHash];
    watched[contentHash] = {
      watchedMs: (existing?.watchedMs ?? 0) + watchedMs,
      lastWatchedAt: Date.now(),
    };

    try {
      localStorage.setItem(WATCHED_KEY, JSON.stringify(watched));
    } catch {
      // ignore
    }

    // Best-effort server notification
    try {
      await fetch(`${this.baseUrl}/watched`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ contentHash, watchedMs }),
      });
    } catch {
      // Non-critical — local record is sufficient
    }
  }

}
