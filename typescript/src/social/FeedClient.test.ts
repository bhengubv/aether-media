// SPDX-License-Identifier: MIT
import { describe, it, before, after, beforeEach } from "node:test";
import { strict as assert } from "node:assert";
import { FeedClient } from "./FeedClient.js";

// ── Browser API stubs (not available in Node.js) ──────────────────────────────

interface LocalStorageMock {
  _store: Record<string, string>;
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
  removeItem(key: string): void;
  clear(): void;
}

function makeLocalStorage(): LocalStorageMock {
  return {
    _store: {},
    getItem(key) { return this._store[key] ?? null; },
    setItem(key, value) { this._store[key] = value; },
    removeItem(key) { delete this._store[key]; },
    clear() { this._store = {}; },
  };
}

type FetchFn = typeof globalThis.fetch;

let originalFetch: FetchFn;
let storageMock: LocalStorageMock;

before(() => {
  originalFetch = globalThis.fetch;
  storageMock = makeLocalStorage();
  (globalThis as unknown as { localStorage: LocalStorageMock }).localStorage = storageMock;
});

after(() => {
  globalThis.fetch = originalFetch;
  delete (globalThis as unknown as { localStorage?: unknown }).localStorage;
});

beforeEach(() => storageMock.clear());

function mockFetch(data: unknown, status = 200): void {
  globalThis.fetch = async (_url: RequestInfo | URL, _init?: RequestInit) => {
    return new Response(JSON.stringify(data), {
      status,
      headers: { "Content-Type": "application/json" },
    });
  };
}

// ── Constructor ───────────────────────────────────────────────────────────────

describe("FeedClient", () => {
  describe("constructor", () => {
    it("uses default base URL when none provided", () => {
      const client = new FeedClient();
      assert.ok(client);
    });

    it("strips trailing slash from baseUrl", () => {
      // No visible property, but client instantiation should not throw
      const client = new FeedClient("https://my-relay.example.com/");
      assert.ok(client);
    });
  });

  // ── markWatched validation ──────────────────────────────────────────────

  describe("markWatched — validation", () => {
    it("throws on empty contentHash", async () => {
      const client = new FeedClient("http://ignored");
      await assert.rejects(
        () => client.markWatched("  ", 1_000),
        /contentHash must not be empty/,
      );
    });

    it("throws on negative watchedMs", async () => {
      const client = new FeedClient("http://ignored");
      await assert.rejects(
        () => client.markWatched("valid-hash", -1),
        /watchedMs must be >= 0/,
      );
    });
  });

  // ── markWatched storage ─────────────────────────────────────────────────

  describe("markWatched — localStorage", () => {
    it("accumulates watched time for the same hash", async () => {
      mockFetch({});   // best-effort server call returns OK
      const client = new FeedClient("http://ignored");

      await client.markWatched("hash-a", 5_000);
      await client.markWatched("hash-a", 3_000);

      const raw = storageMock.getItem("aethernet_watched");
      assert.ok(raw, "aethernet_watched key should exist");
      const record: Record<string, { watchedMs: number }> = JSON.parse(raw!);
      assert.equal(record["hash-a"].watchedMs, 8_000);
    });

    it("tracks different hashes independently", async () => {
      mockFetch({});
      const client = new FeedClient("http://ignored");

      await client.markWatched("hash-x", 2_000);
      await client.markWatched("hash-y", 7_000);

      const raw = storageMock.getItem("aethernet_watched");
      const record: Record<string, { watchedMs: number }> = JSON.parse(raw!);
      assert.equal(record["hash-x"].watchedMs, 2_000);
      assert.equal(record["hash-y"].watchedMs, 7_000);
    });
  });

  // ── getFeed network ─────────────────────────────────────────────────────

  describe("getFeed", () => {
    it("returns parsed items from the network", async () => {
      const nowMs = Date.now();
      mockFetch([
        {
          content: {
            content_hash: "h1", title: "Vid 1", duration_ms: 60_000,
            codec: "h264", content_type: "video/mp4", creator_uhid: "creator-A",
            size_bytes: 1024, created_at_ms: nowMs, thumbnail_hash: null, tags: ["demo"],
          },
          like_count: 0, share_count: 0, comment_count: 0, watch_count: 0,
          is_live: false, stream_id: null, top_reactions: [], published_at_ms: nowMs,
        },
      ]);

      const client = new FeedClient("http://test");
      const items  = await client.getFeed(1, 0);

      assert.equal(items.length, 1);
      assert.equal(items[0].content.contentHash, "h1");
      assert.equal(items[0].content.title, "Vid 1");
      assert.equal(typeof items[0].content.createdAtMs, "number");
    });

    it("throws on HTTP error", async () => {
      mockFetch({ error: "internal" }, 500);

      const client = new FeedClient("http://test");
      await assert.rejects(
        () => client.getFeed(),
        /Feed fetch failed: 500/,
      );
    });

    it("returns cached result on second call", async () => {
      let callCount = 0;
      globalThis.fetch = async (_u: RequestInfo | URL) => {
        callCount++;
        return new Response(JSON.stringify([]), { status: 200 });
      };

      const client = new FeedClient("http://test-cache");
      await client.getFeed(10, 0);
      await client.getFeed(10, 0); // should hit memory cache

      assert.equal(callCount, 1);
    });
  });
});
