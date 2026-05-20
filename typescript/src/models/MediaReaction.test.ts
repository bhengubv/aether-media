// SPDX-License-Identifier: MIT
import { describe, it } from "node:test";
import { strict as assert } from "node:assert";
import { createReaction, MediaReactionType, toWire, fromWire } from "./MediaReaction.js";

// ── Helpers ───────────────────────────────────────────────────────────────────

const SENT_AT_MS = new Date("2025-06-01T12:00:00.000Z").getTime();

// ── createReaction — Happy path ───────────────────────────────────────────────

describe("createReaction", () => {
  describe("happy path", () => {
    it("creates a Like reaction with null message", () => {
      const sentAtMs = SENT_AT_MS;
      const r = createReaction(
        "rxn-001", "content-abc", "viewer-001",
        MediaReactionType.Like, 5_000, null, sentAtMs,
      );
      assert.equal(r.reactionId,  "rxn-001");
      assert.equal(r.contentHash, "content-abc");
      assert.equal(r.fromUhid,    "viewer-001");
      assert.equal(r.type,        MediaReactionType.Like);
      assert.equal(r.positionMs,  5_000);
      assert.equal(r.message,     null);
      assert.equal(r.sentAtMs,    sentAtMs);
    });

    it("creates a Share reaction with null message", () => {
      const r = createReaction(
        "rxn-002", "content-abc", "viewer-001",
        MediaReactionType.Share, 0, null, SENT_AT_MS,
      );
      assert.equal(r.type, MediaReactionType.Share);
      assert.equal(r.message, null);
      assert.equal(r.sentAtMs, SENT_AT_MS);
    });

    it("creates a SuperReact reaction with null message", () => {
      const r = createReaction(
        "rxn-003", "content-abc", "viewer-001",
        MediaReactionType.SuperReact, 1_000, null, SENT_AT_MS,
      );
      assert.equal(r.type, MediaReactionType.SuperReact);
      assert.equal(r.message, null);
    });

    it("creates a Comment reaction with a non-empty message", () => {
      const r = createReaction(
        "rxn-004", "content-abc", "viewer-001",
        MediaReactionType.Comment, 2_500, "Great video!", SENT_AT_MS,
      );
      assert.equal(r.type,    MediaReactionType.Comment);
      assert.equal(r.message, "Great video!");
    });

    it("accepts positionMs = 0", () => {
      const r = createReaction(
        "rxn-005", "hash", "viewer",
        MediaReactionType.Like, 0, null, SENT_AT_MS,
      );
      assert.equal(r.positionMs, 0);
    });
  });

  // ── Validation errors ─────────────────────────────────────────────────────

  describe("validation", () => {
    it("throws when contentHash is empty", () => {
      assert.throws(
        () => createReaction("id", "  ", "viewer", MediaReactionType.Like, 0, null, SENT_AT_MS),
        /contentHash must not be empty/,
      );
    });

    it("throws when fromUhid is empty", () => {
      assert.throws(
        () => createReaction("id", "hash", "", MediaReactionType.Like, 0, null, SENT_AT_MS),
        /fromUhid must not be empty/,
      );
    });

    it("throws when positionMs is negative", () => {
      assert.throws(
        () => createReaction("id", "hash", "viewer", MediaReactionType.Like, -1, null, SENT_AT_MS),
        /positionMs must be >= 0/,
      );
    });

    it("throws for Comment without a message", () => {
      assert.throws(
        () => createReaction("id", "hash", "viewer", MediaReactionType.Comment, 0, null, SENT_AT_MS),
        /message is required for Comment/i,
      );
    });

    it("throws for Comment with a whitespace-only message", () => {
      assert.throws(
        () => createReaction("id", "hash", "viewer", MediaReactionType.Comment, 0, "   ", SENT_AT_MS),
        /message is required for Comment/i,
      );
    });

    it("throws for Like with a non-null message", () => {
      assert.throws(
        () => createReaction("id", "hash", "viewer", MediaReactionType.Like, 0, "oops", SENT_AT_MS),
        /message must be null for Like/i,
      );
    });

    it("throws for Share with a non-null message", () => {
      assert.throws(
        () => createReaction("id", "hash", "viewer", MediaReactionType.Share, 0, "oops", SENT_AT_MS),
        /message must be null for Share/i,
      );
    });

    it("throws for SuperReact with a non-null message", () => {
      assert.throws(
        () => createReaction("id", "hash", "viewer", MediaReactionType.SuperReact, 0, "oops", SENT_AT_MS),
        /message must be null for SuperReact/i,
      );
    });
  });

  // ── MediaReactionType enum values ─────────────────────────────────────────

  describe("MediaReactionType enum", () => {
    it("Like = 1",       () => assert.equal(MediaReactionType.Like,       1));
    it("Share = 2",      () => assert.equal(MediaReactionType.Share,      2));
    it("Comment = 3",    () => assert.equal(MediaReactionType.Comment,    3));
    it("SuperReact = 4", () => assert.equal(MediaReactionType.SuperReact, 4));
  });
});

// ── toWire / fromWire roundtrip ───────────────────────────────────────────────

describe("toWire / fromWire", () => {
  it("roundtrips a Like reaction", () => {
    const r = createReaction(
      "rxn-rt", "content-rt", "viewer-rt",
      MediaReactionType.Like, 3_000, null, SENT_AT_MS,
    );
    const wire     = toWire(r);
    const restored = fromWire(wire);

    assert.equal(restored.reactionId,  r.reactionId);
    assert.equal(restored.contentHash, r.contentHash);
    assert.equal(restored.fromUhid,    r.fromUhid);
    assert.equal(restored.type,        MediaReactionType.Like);
    assert.equal(restored.positionMs,  r.positionMs);
    assert.equal(restored.message,     null);
    assert.equal(restored.sentAtMs,    SENT_AT_MS);
  });

  it("roundtrips a Comment reaction", () => {
    const r = createReaction(
      "rxn-cmt", "content-cmt", "viewer-cmt",
      MediaReactionType.Comment, 10_000, "Nice!", SENT_AT_MS,
    );
    const restored = fromWire(toWire(r));
    assert.equal(restored.type,    MediaReactionType.Comment);
    assert.equal(restored.message, "Nice!");
  });

  it("toWire uses snake_case keys", () => {
    const r = createReaction("id", "hash", "v", MediaReactionType.Share, 0, null, SENT_AT_MS);
    const wire = toWire(r);
    assert.ok("reaction_id"  in wire);
    assert.ok("content_hash" in wire);
    assert.ok("from_uhid"    in wire);
    assert.ok("position_ms"  in wire);
    assert.ok("sent_at_ms"   in wire);
  });

  it("fromWire throws on unknown type string", () => {
    assert.throws(
      () => fromWire({
        reaction_id:  "id",
        content_hash: "hash",
        from_uhid:    "v",
        type:         "unknown_type",
        position_ms:  0,
        message:      null,
        sent_at_ms:   SENT_AT_MS,
      }),
      /Unknown reaction type/,
    );
  });
});
