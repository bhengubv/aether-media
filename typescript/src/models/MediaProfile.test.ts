// SPDX-License-Identifier: MIT
import { describe, it } from "node:test";
import { strict as assert } from "node:assert";
import { shortBio, toWire, fromWire } from "./MediaProfile.js";
import type { MediaProfile } from "./MediaProfile.js";

// ── Helpers ───────────────────────────────────────────────────────────────────

function makeProfile(bio: string | null): MediaProfile {
  return {
    uhid:           "uhid-001",
    displayName:    "Test User",
    avatarHash:     null,
    bio,
    aethermeshTag:      "@testuser",
    followerCount:  0,
    followingCount: 0,
    contentCount:   0,
    isVerified:     false,
    joinedAtMs:     new Date("2024-01-01T00:00:00Z").getTime(),
  };
}

// ── shortBio ──────────────────────────────────────────────────────────────────

describe("shortBio", () => {
  // Happy path

  it("returns bio unchanged when it is under 120 characters", () => {
    const bio = "Short bio that is well under the limit.";
    assert.equal(shortBio(makeProfile(bio)), bio);
  });

  it("returns bio unchanged when it is exactly 120 characters", () => {
    const bio = "a".repeat(120);
    assert.equal(shortBio(makeProfile(bio)), bio);
  });

  it("truncates at last word boundary and appends ellipsis", () => {
    // 121 chars: 119 "a"s + space + 1 trailing char → boundary at space (pos 119)
    const bio = "a".repeat(119) + " b";
    const result = shortBio(makeProfile(bio));
    assert.ok(result.endsWith("…"), `Expected ellipsis, got: ${result}`);
    assert.ok(!result.includes(" b"));
  });

  it("truncates at hard boundary when no space exists within 120 chars", () => {
    const bio = "a".repeat(150); // no spaces
    const result = shortBio(makeProfile(bio));
    assert.ok(result.endsWith("…"));
    assert.equal(result, "a".repeat(120) + "…");
  });

  it("truncates a realistic long bio at a word boundary", () => {
    const words = Array.from({ length: 30 }, (_, i) => `word${i}`);
    const bio = words.join(" ");
    const result = shortBio(makeProfile(bio));
    assert.ok(result.endsWith("…"));
    const beforeEllipsis = result.slice(0, -1);
    assert.ok(!beforeEllipsis.endsWith(" "), "Should not end with trailing space before ellipsis");
  });

  // Edge cases

  it("returns empty string for null bio", () => {
    assert.equal(shortBio(makeProfile(null)), "");
  });

  it("returns empty string for empty string bio", () => {
    assert.equal(shortBio(makeProfile("")), "");
  });

  it("returns empty string for whitespace-only bio", () => {
    assert.equal(shortBio(makeProfile("   ")), "");
  });

  it("trims leading/trailing whitespace before measuring length", () => {
    const padded = "  Short bio.  ";
    const result = shortBio(makeProfile(padded));
    assert.equal(result, "Short bio.");
  });
});

// ── toWire / fromWire roundtrip ───────────────────────────────────────────────

describe("toWire / fromWire", () => {
  it("roundtrips a profile", () => {
    const p = makeProfile("Hello from Aether.");
    const restored = fromWire(toWire(p));

    assert.equal(restored.uhid,           p.uhid);
    assert.equal(restored.displayName,    p.displayName);
    assert.equal(restored.avatarHash,     p.avatarHash);
    assert.equal(restored.bio,            p.bio);
    assert.equal(restored.aethermeshTag,      p.aethermeshTag);
    assert.equal(restored.followerCount,  p.followerCount);
    assert.equal(restored.followingCount, p.followingCount);
    assert.equal(restored.contentCount,   p.contentCount);
    assert.equal(restored.isVerified,     p.isVerified);
    assert.equal(restored.joinedAtMs,     p.joinedAtMs);
  });

  it("toWire uses snake_case keys", () => {
    const wire = toWire(makeProfile(null));
    assert.ok("display_name"    in wire);
    assert.ok("avatar_hash"     in wire);
    assert.ok("aethermesh_tag"      in wire);
    assert.ok("follower_count"  in wire);
    assert.ok("following_count" in wire);
    assert.ok("content_count"   in wire);
    assert.ok("is_verified"     in wire);
    assert.ok("joined_at_ms"    in wire);
  });

  it("roundtrips a verified profile with null avatarHash", () => {
    const p: MediaProfile = {
      ...makeProfile(null),
      isVerified: true,
      avatarHash: null,
    };
    const restored = fromWire(toWire(p));
    assert.equal(restored.isVerified, true);
    assert.equal(restored.avatarHash, null);
  });

  it("roundtrips joinedAtMs as a unix timestamp number", () => {
    const ms = new Date("2023-06-15T10:30:00.000Z").getTime();
    const p: MediaProfile = { ...makeProfile(null), joinedAtMs: ms };
    const restored = fromWire(toWire(p));
    assert.equal(restored.joinedAtMs, ms);
    assert.equal(typeof restored.joinedAtMs, "number");
  });
});
