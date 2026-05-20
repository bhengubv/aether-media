// SPDX-License-Identifier: MIT
import { describe, it } from "node:test";
import { strict as assert } from "node:assert";
import { formattedDuration, isVideo, isAudio } from "./MediaContent.js";

// ── formattedDuration ─────────────────────────────────────────────────────────

describe("formattedDuration", () => {
  it("returns Live for 0ms", () => {
    assert.equal(formattedDuration(0), "Live");
  });

  it("returns Live for negative ms", () => {
    assert.equal(formattedDuration(-1), "Live");
  });

  it("formats seconds-only correctly (0:SS)", () => {
    assert.equal(formattedDuration(5_000),  "0:05");
    assert.equal(formattedDuration(45_000), "0:45");
  });

  it("formats minutes and seconds (M:SS)", () => {
    assert.equal(formattedDuration(60_000),   "1:00");
    assert.equal(formattedDuration(90_000),   "1:30");
    assert.equal(formattedDuration(272_000),  "4:32");
    assert.equal(formattedDuration(599_000),  "9:59");
  });

  it("formats double-digit minutes (MM:SS)", () => {
    assert.equal(formattedDuration(600_000),   "10:00");
    assert.equal(formattedDuration(3_599_000), "59:59");
  });

  it("formats hours correctly (H:MM:SS)", () => {
    assert.equal(formattedDuration(3_600_000),  "1:00:00");
    assert.equal(formattedDuration(3_723_000),  "1:02:03");
    assert.equal(formattedDuration(86_400_000), "24:00:00");
  });

  it("pads seconds with leading zero", () => {
    assert.equal(formattedDuration(3_605_000), "1:00:05");
  });
});

// ── isVideo ───────────────────────────────────────────────────────────────────

describe("isVideo", () => {
  const make = (contentType: string) => ({
    contentHash: "h", title: "t", durationMs: 0, codec: "c",
    contentType, creatorUhid: "u", sizeBytes: 0,
    createdAtMs: 0, thumbnailHash: null, tags: [],
  });

  it("returns true for video/mp4", () => {
    assert.ok(isVideo(make("video/mp4")));
  });

  it("returns true for video/webm", () => {
    assert.ok(isVideo(make("video/webm")));
  });

  it("returns true for VIDEO/MP4 (case-insensitive)", () => {
    assert.ok(isVideo(make("VIDEO/MP4")));
  });

  it("returns false for audio/mp3", () => {
    assert.ok(!isVideo(make("audio/mp3")));
  });

  it("returns false for application/octet-stream", () => {
    assert.ok(!isVideo(make("application/octet-stream")));
  });
});

// ── isAudio ───────────────────────────────────────────────────────────────────

describe("isAudio", () => {
  const make = (contentType: string) => ({
    contentHash: "h", title: "t", durationMs: 0, codec: "c",
    contentType, creatorUhid: "u", sizeBytes: 0,
    createdAtMs: 0, thumbnailHash: null, tags: [],
  });

  it("returns true for audio/mpeg", () => {
    assert.ok(isAudio(make("audio/mpeg")));
  });

  it("returns true for audio/ogg", () => {
    assert.ok(isAudio(make("audio/ogg")));
  });

  it("returns true for AUDIO/OPUS (case-insensitive)", () => {
    assert.ok(isAudio(make("AUDIO/OPUS")));
  });

  it("returns false for video/mp4", () => {
    assert.ok(!isAudio(make("video/mp4")));
  });

  it("returns false for text/plain", () => {
    assert.ok(!isAudio(make("text/plain")));
  });
});
