// SPDX-License-Identifier: MIT
import { describe, it, before, after } from "node:test";
import { strict as assert } from "node:assert";
import { AetherMeshStreamClient } from "./AetherMeshStreamClient.js";
import type { AetherMeshMediaPlayer } from "../player/AetherMeshMediaPlayer.js";

// ── Mock AetherMeshMediaPlayer ────────────────────────────────────────────────────

function makeMockPlayer() {
  const segments: Array<{ bytes: Uint8Array; mimeCodec: string }> = [];
  const player = {
    feedSegment(bytes: Uint8Array, mimeCodec: string): void {
      segments.push({ bytes, mimeCodec });
    },
    segments,
  } as unknown as AetherMeshMediaPlayer & { segments: typeof segments };
  return player;
}

// ── ReadableStream helpers ────────────────────────────────────────────────────

/**
 * Build a 4-byte big-endian length-prefixed segment chunk.
 */
function encodeSegment(data: Uint8Array): Uint8Array {
  const len = data.length;
  const buf = new Uint8Array(4 + len);
  buf[0] = (len >>> 24) & 0xff;
  buf[1] = (len >>> 16) & 0xff;
  buf[2] = (len >>> 8)  & 0xff;
  buf[3] =  len         & 0xff;
  buf.set(data, 4);
  return buf;
}

/**
 * Create a ReadableStream that emits the given chunks then closes.
 */
function makeStream(chunks: Uint8Array[]): ReadableStream<Uint8Array> {
  let i = 0;
  return new ReadableStream<Uint8Array>({
    pull(controller) {
      if (i < chunks.length) {
        controller.enqueue(chunks[i++]);
      } else {
        controller.close();
      }
    },
  });
}

type FetchFn = typeof globalThis.fetch;
let originalFetch: FetchFn;

before(() => { originalFetch = globalThis.fetch; });
after(()  => { globalThis.fetch = originalFetch; });

function mockStreamFetch(stream: ReadableStream<Uint8Array>, status = 200): void {
  globalThis.fetch = async (_url: RequestInfo | URL, _init?: RequestInit) => {
    return new Response(stream, { status });
  };
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe("AetherMeshStreamClient", () => {
  // ── Constructor ──────────────────────────────────────────────────────────

  describe("constructor", () => {
    it("constructs with a player instance", () => {
      const player = makeMockPlayer();
      const client = new AetherMeshStreamClient(player);
      assert.ok(client);
    });

    it("activeStreamId is null before subscribing", () => {
      const client = new AetherMeshStreamClient(makeMockPlayer());
      assert.equal(client.activeStreamId, null);
    });
  });

  // ── subscribe — HTTP error ────────────────────────────────────────────────
  // The source code catches all non-abort errors internally and logs them.
  // subscribe() resolves (does not reject) on HTTP errors.

  describe("subscribe — HTTP error", () => {
    it("resolves without throwing when the relay returns non-OK status", async () => {
      globalThis.fetch = async (_url: RequestInfo | URL) =>
        new Response("{}", { status: 503 });

      const client = new AetherMeshStreamClient(makeMockPlayer());
      // Should resolve, not reject
      await assert.doesNotReject(() => client.subscribe("http://test.example/stream/abc"));
    });

    it("activeStreamId is null after a failed subscribe", async () => {
      globalThis.fetch = async (_url: RequestInfo | URL) =>
        new Response("{}", { status: 503 });

      const client = new AetherMeshStreamClient(makeMockPlayer());
      await client.subscribe("http://test.example/stream/abc");
      assert.equal(client.activeStreamId, null);
    });
  });

  // ── subscribe — no body ───────────────────────────────────────────────────
  // Response body is null/missing — the source code catches and logs this too.

  describe("subscribe — no body", () => {
    it("resolves without throwing when response has no body", async () => {
      globalThis.fetch = async (_url: RequestInfo | URL) =>
        new Response(null, { status: 200 });

      const client = new AetherMeshStreamClient(makeMockPlayer());
      await assert.doesNotReject(() => client.subscribe("http://test.example/stream/abc"));
    });
  });

  // ── subscribe — aether:// URI resolution ─────────────────────────────────

  describe("subscribe — URI resolution", () => {
    it("resolves aether:// URI to the relay HTTP URL", async () => {
      let capturedUrl = "";
      const seg = encodeSegment(new Uint8Array([1, 2, 3]));
      globalThis.fetch = async (url: RequestInfo | URL, _init?: RequestInit) => {
        capturedUrl = url.toString();
        return new Response(makeStream([seg]), { status: 200 });
      };

      const client = new AetherMeshStreamClient(makeMockPlayer());
      await client.subscribe("aether://my-stream-id");

      assert.match(capturedUrl, /relay\.aethermesh\.network/);
      assert.match(capturedUrl, /my-stream-id/);
    });

    it("uses HTTP URLs unchanged", async () => {
      let capturedUrl = "";
      const seg = encodeSegment(new Uint8Array([4, 5, 6]));
      globalThis.fetch = async (url: RequestInfo | URL, _init?: RequestInit) => {
        capturedUrl = url.toString();
        return new Response(makeStream([seg]), { status: 200 });
      };

      const client = new AetherMeshStreamClient(makeMockPlayer());
      await client.subscribe("http://my-relay.example.com/stream/xyz");

      assert.equal(capturedUrl, "http://my-relay.example.com/stream/xyz");
    });
  });

  // ── subscribe — segment delivery ─────────────────────────────────────────

  describe("subscribe — segment delivery", () => {
    it("delivers a single segment to the player and the onSegment callback", async () => {
      const payload   = new Uint8Array([10, 20, 30, 40]);
      const encoded   = encodeSegment(payload);
      mockStreamFetch(makeStream([encoded]));

      const player   = makeMockPlayer();
      const client   = new AetherMeshStreamClient(player);

      const received: Uint8Array[] = [];
      client.onSegment = (bytes) => received.push(bytes);

      await client.subscribe("http://test/stream");

      assert.equal(player.segments.length, 1);
      assert.deepEqual(Array.from(player.segments[0].bytes), Array.from(payload));
      assert.equal(received.length, 1);
      assert.deepEqual(Array.from(received[0]), Array.from(payload));
    });

    it("delivers multiple segments from a single chunk", async () => {
      const payloads = [
        new Uint8Array([1, 2]),
        new Uint8Array([3, 4, 5]),
        new Uint8Array([6]),
      ];

      // Combine all encoded segments into one big chunk
      const parts = payloads.map(encodeSegment);
      const totalLen = parts.reduce((s, p) => s + p.length, 0);
      const combined = new Uint8Array(totalLen);
      let offset = 0;
      for (const p of parts) { combined.set(p, offset); offset += p.length; }

      mockStreamFetch(makeStream([combined]));

      const player = makeMockPlayer();
      const client = new AetherMeshStreamClient(player);
      await client.subscribe("http://test/stream");

      assert.equal(player.segments.length, 3);
      assert.deepEqual(Array.from(player.segments[0].bytes), [1, 2]);
      assert.deepEqual(Array.from(player.segments[1].bytes), [3, 4, 5]);
      assert.deepEqual(Array.from(player.segments[2].bytes), [6]);
    });

    it("handles a segment split across two chunks", async () => {
      const payload = new Uint8Array([0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE]);
      const encoded = encodeSegment(payload);
      // Split the encoded bytes into two chunks mid-segment
      const half1 = encoded.slice(0, 5);
      const half2 = encoded.slice(5);

      mockStreamFetch(makeStream([half1, half2]));

      const player = makeMockPlayer();
      const client = new AetherMeshStreamClient(player);
      await client.subscribe("http://test/stream");

      assert.equal(player.segments.length, 1);
      assert.deepEqual(Array.from(player.segments[0].bytes), Array.from(payload));
    });

    it("passes the correct MIME codec string to feedSegment", async () => {
      const payload = new Uint8Array([1]);
      mockStreamFetch(makeStream([encodeSegment(payload)]));

      const player = makeMockPlayer();
      const client = new AetherMeshStreamClient(player);
      await client.subscribe("http://test/stream");

      assert.match(player.segments[0].mimeCodec, /video\/mp4/);
      assert.match(player.segments[0].mimeCodec, /avc1/);
    });

    it("delivers no segments for an empty stream", async () => {
      mockStreamFetch(makeStream([]));

      const player = makeMockPlayer();
      const client = new AetherMeshStreamClient(player);
      await client.subscribe("http://test/stream");

      assert.equal(player.segments.length, 0);
    });
  });

  // ── unsubscribe ───────────────────────────────────────────────────────────

  describe("unsubscribe", () => {
    it("is a no-op when there is no active subscription", () => {
      const client = new AetherMeshStreamClient(makeMockPlayer());
      assert.doesNotThrow(() => client.unsubscribe());
    });

    it("activeStreamId is null after unsubscribe called with no active stream", () => {
      const client = new AetherMeshStreamClient(makeMockPlayer());
      client.unsubscribe();
      assert.equal(client.activeStreamId, null);
    });

    it("can be called multiple times safely", () => {
      const client = new AetherMeshStreamClient(makeMockPlayer());
      assert.doesNotThrow(() => {
        client.unsubscribe();
        client.unsubscribe();
        client.unsubscribe();
      });
    });
  });

  // ── subscribe then unsubscribe ────────────────────────────────────────────

  describe("subscribe — abort via unsubscribe", () => {
    it("resolves cleanly after unsubscribe aborts the stream", async () => {
      // Use a stream that has a readable body but can be read from —
      // unsubscribe is called after the subscribe promise resolves a completed stream
      const seg = encodeSegment(new Uint8Array([99]));
      mockStreamFetch(makeStream([seg]));

      const client = new AetherMeshStreamClient(makeMockPlayer());
      // Subscribe completes (stream closes), then unsubscribe is safe
      await client.subscribe("http://test/stream");
      assert.doesNotThrow(() => client.unsubscribe());
      assert.equal(client.activeStreamId, null);
    });
  });

  // ── onSegment setter ──────────────────────────────────────────────────────

  describe("onSegment setter", () => {
    it("receives all segment bytes via the callback", async () => {
      const payloads = [new Uint8Array([1]), new Uint8Array([2])];
      const encoded  = payloads.map(encodeSegment);
      const totalLen = encoded.reduce((s, p) => s + p.length, 0);
      const combined = new Uint8Array(totalLen);
      let off = 0;
      for (const p of encoded) { combined.set(p, off); off += p.length; }

      mockStreamFetch(makeStream([combined]));

      const player = makeMockPlayer();
      const client = new AetherMeshStreamClient(player);

      const received: number[] = [];
      client.onSegment = (b) => received.push(b[0]);

      await client.subscribe("http://test/stream");

      assert.deepEqual(received, [1, 2]);
    });

    it("does not crash when no callback is registered", async () => {
      const seg = encodeSegment(new Uint8Array([42]));
      mockStreamFetch(makeStream([seg]));

      const player = makeMockPlayer();
      const client = new AetherMeshStreamClient(player);
      // no onSegment set — should not throw
      await assert.doesNotReject(() => client.subscribe("http://test/stream"));
      assert.equal(player.segments.length, 1);
    });
  });
});
