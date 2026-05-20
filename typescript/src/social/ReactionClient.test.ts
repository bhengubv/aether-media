// SPDX-License-Identifier: MIT
import { describe, it, before, after } from "node:test";
import { strict as assert } from "node:assert";
import { ReactionClient } from "./ReactionClient.js";
import { type MediaReaction, MediaReactionType } from "../models/MediaReaction.js";

// ── Mock helpers ───────────────────────────────────────────────────────────────

type FetchFn = typeof globalThis.fetch;
let originalFetch: FetchFn;

before(() => { originalFetch = globalThis.fetch; });
after(()  => { globalThis.fetch = originalFetch; });

function mockFetch(data: unknown, status = 200): void {
  globalThis.fetch = async (_u: RequestInfo | URL, _init?: RequestInit) =>
    new Response(JSON.stringify(data), {
      status,
      headers: { "Content-Type": "application/json" },
    });
}

function makeReaction(overrides?: Partial<MediaReaction>): MediaReaction {
  return {
    reactionId:  "rxn-001",
    contentHash: "content-abc",
    fromUhid:    "viewer-001",
    type:        MediaReactionType.Like,
    positionMs:  1_500,
    message:     null,
    sentAtMs:    new Date("2025-01-01T00:00:00Z").getTime(),
    ...overrides,
  };
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe("ReactionClient", () => {
  // ── Constructor ──────────────────────────────────────────────────────────

  describe("constructor", () => {
    it("uses default base URL", () => {
      assert.ok(new ReactionClient());
    });

    it("strips trailing slash", () => {
      assert.ok(new ReactionClient("https://example.com/media/"));
    });
  });

  // ── sendReaction — validation ────────────────────────────────────────────

  describe("sendReaction — validation", () => {
    it("throws on blank contentHash", async () => {
      const client = new ReactionClient("http://ignored");
      await assert.rejects(
        () => client.sendReaction(makeReaction({ contentHash: "   " })),
        /contentHash must not be empty/,
      );
    });

    it("throws on blank fromUhid", async () => {
      const client = new ReactionClient("http://ignored");
      await assert.rejects(
        () => client.sendReaction(makeReaction({ fromUhid: "" })),
        /fromUhid must not be empty/,
      );
    });
  });

  // ── sendReaction — network ───────────────────────────────────────────────

  describe("sendReaction — network", () => {
    it("resolves on 200 OK", async () => {
      mockFetch({}, 200);
      const client = new ReactionClient("http://test");
      await assert.doesNotReject(() => client.sendReaction(makeReaction()));
    });

    it("throws on HTTP error", async () => {
      mockFetch({ error: "server error" }, 503);
      const client = new ReactionClient("http://test");
      await assert.rejects(
        () => client.sendReaction(makeReaction()),
        /sendReaction failed: 503/,
      );
    });

    it("sends a POST with correct content-type", async () => {
      let capturedInit: RequestInit | undefined;
      globalThis.fetch = async (_u: RequestInfo | URL, init?: RequestInit) => {
        capturedInit = init;
        return new Response("{}", { status: 200 });
      };

      const client = new ReactionClient("http://test");
      await client.sendReaction(makeReaction());

      assert.equal(capturedInit?.method, "POST");
      assert.match(
        (capturedInit?.headers as Record<string, string>)?.["Content-Type"] ?? "",
        /application\/json/i,
      );
    });

    it("serialises using snake_case wire format with unix-ms timestamp", async () => {
      let capturedBody = "";
      globalThis.fetch = async (_u: RequestInfo | URL, init?: RequestInit) => {
        capturedBody = init?.body as string;
        return new Response("{}", { status: 200 });
      };

      const sentAtMs = new Date("2025-06-01T12:00:00.000Z").getTime();
      const client   = new ReactionClient("http://test");
      await client.sendReaction(makeReaction({ sentAtMs }));

      const parsed = JSON.parse(capturedBody);
      assert.equal(parsed.sent_at_ms, sentAtMs);
      assert.equal(typeof parsed.sent_at_ms, "number");
      assert.equal(parsed.type, "like");             // wire enum as lowercase string
      assert.equal(parsed.reaction_id, "rxn-001");   // snake_case field
    });
  });

  // ── getReactions — validation ────────────────────────────────────────────

  describe("getReactions — validation", () => {
    it("throws on blank contentHash", async () => {
      const client = new ReactionClient("http://ignored");
      await assert.rejects(
        () => client.getReactions("  "),
        /contentHash must not be empty/,
      );
    });
  });

  // ── getReactions — network ───────────────────────────────────────────────

  describe("getReactions — network", () => {
    it("returns an empty array when server returns []", async () => {
      mockFetch([]);
      const client   = new ReactionClient("http://test");
      const reactions = await client.getReactions("some-hash");
      assert.deepEqual(reactions, []);
    });

    it("deserialises snake_case wire reactions into MediaReaction objects", async () => {
      const sentAtMs = new Date("2025-03-15T08:30:00.000Z").getTime();
      mockFetch([
        {
          reaction_id:  "r-001",
          content_hash: "content-xyz",
          from_uhid:    "viewer-abc",
          type:         "comment",
          position_ms:  5_000,
          message:      "Nice!",
          sent_at_ms:   sentAtMs,
        },
      ]);

      const client    = new ReactionClient("http://test");
      const reactions = await client.getReactions("content-xyz");

      assert.equal(reactions.length, 1);
      const r = reactions[0];
      assert.equal(r.reactionId,  "r-001");
      assert.equal(r.contentHash, "content-xyz");
      assert.equal(r.fromUhid,    "viewer-abc");
      assert.equal(r.positionMs,  5_000);
      assert.equal(r.message,     "Nice!");
      assert.equal(r.sentAtMs,    sentAtMs);
      assert.equal(typeof r.sentAtMs, "number");
      assert.equal(r.type, MediaReactionType.Comment);
    });

    it("throws on HTTP error", async () => {
      mockFetch({ error: "not found" }, 404);
      const client = new ReactionClient("http://test");
      await assert.rejects(
        () => client.getReactions("hash"),
        /getReactions failed: 404/,
      );
    });
  });
});
