// SPDX-License-Identifier: MIT
import { describe, it } from "node:test";
import { strict as assert } from "node:assert";
import { AetherNetMediaPlayer } from "./AetherNetMediaPlayer.js";

// ── Minimal HTMLVideoElement mock ─────────────────────────────────────────────

type EventName = "playing" | "pause" | "ended" | "error";
type EventListener = () => void;

function makeMockVideoElement() {
  const listeners = new Map<EventName, EventListener[]>();

  const el = {
    currentTime:   0,
    duration:      NaN,
    volume:        1,
    playbackRate:  1,
    src:           "",

    addEventListener(event: string, cb: EventListener): void {
      const list = listeners.get(event as EventName) ?? [];
      list.push(cb);
      listeners.set(event as EventName, list);
    },

    removeEventListener(_event: string, _cb: EventListener): void { /* no-op */ },

    play(): Promise<void> {
      return Promise.resolve();
    },

    pause(): void { /* no-op */ },

    // Test helper: simulate a DOM event
    _emit(event: EventName): void {
      const list = listeners.get(event) ?? [];
      for (const cb of list) cb();
    },
  };

  return el;
}

type MockVideoElement = ReturnType<typeof makeMockVideoElement>;

function makePlayer(video?: MockVideoElement) {
  const v = video ?? makeMockVideoElement();
  const player = new AetherNetMediaPlayer(v as unknown as HTMLVideoElement);
  return { player, video: v };
}

// ── Tests ──────────────────────────────────────────────────────────────────────

describe("AetherNetMediaPlayer", () => {
  // ── Constructor ──────────────────────────────────────────────────────────

  describe("constructor", () => {
    it("throws when videoElement is null", () => {
      assert.throws(
        () => new AetherNetMediaPlayer(null as unknown as HTMLVideoElement),
        /videoElement is required/,
      );
    });

    it("constructs successfully with a valid video element", () => {
      const { player } = makePlayer();
      assert.ok(player);
    });
  });

  // ── Initial state ─────────────────────────────────────────────────────────

  describe("initial state", () => {
    it("state is idle after construction", () => {
      const { player } = makePlayer();
      assert.equal(player.state, "idle");
    });

    it("positionMs is 0 when currentTime is 0", () => {
      const v = makeMockVideoElement();
      v.currentTime = 0;
      const { player } = makePlayer(v);
      assert.equal(player.positionMs, 0);
    });

    it("durationMs is 0 when duration is NaN", () => {
      const v = makeMockVideoElement();
      v.duration = NaN;
      const { player } = makePlayer(v);
      assert.equal(player.durationMs, 0);
    });

    it("durationMs is 0 when duration is Infinity", () => {
      const v = makeMockVideoElement();
      v.duration = Infinity;
      const { player } = makePlayer(v);
      assert.equal(player.durationMs, 0);
    });

    it("durationMs converts correctly from finite duration", () => {
      const v = makeMockVideoElement();
      v.duration = 120.5;
      const { player } = makePlayer(v);
      assert.equal(player.durationMs, 120_500);
    });
  });

  // ── seek ─────────────────────────────────────────────────────────────────

  describe("seek", () => {
    it("throws RangeError for negative positionMs", () => {
      const { player } = makePlayer();
      assert.throws(
        () => player.seek(-1),
        (err) => err instanceof RangeError && /positionMs must be >= 0/.test((err as Error).message),
      );
    });

    it("accepts 0", () => {
      const v = makeMockVideoElement();
      const { player } = makePlayer(v);
      assert.doesNotThrow(() => player.seek(0));
      assert.equal(v.currentTime, 0);
    });

    it("converts ms to seconds on currentTime", () => {
      const v = makeMockVideoElement();
      const { player } = makePlayer(v);
      player.seek(90_000);
      assert.equal(v.currentTime, 90);
    });
  });

  // ── setVolume ─────────────────────────────────────────────────────────────

  describe("setVolume", () => {
    it("throws RangeError when volume > 1", () => {
      const { player } = makePlayer();
      assert.throws(() => player.setVolume(1.1), RangeError);
    });

    it("throws RangeError when volume < 0", () => {
      const { player } = makePlayer();
      assert.throws(() => player.setVolume(-0.001), RangeError);
    });

    it("accepts 0", () => {
      const v = makeMockVideoElement();
      const { player } = makePlayer(v);
      assert.doesNotThrow(() => player.setVolume(0));
      assert.equal(v.volume, 0);
    });

    it("accepts 1", () => {
      const v = makeMockVideoElement();
      const { player } = makePlayer(v);
      assert.doesNotThrow(() => player.setVolume(1));
      assert.equal(v.volume, 1);
    });

    it("accepts 0.5", () => {
      const v = makeMockVideoElement();
      const { player } = makePlayer(v);
      player.setVolume(0.5);
      assert.equal(v.volume, 0.5);
    });
  });

  // ── setSpeed ─────────────────────────────────────────────────────────────

  describe("setSpeed", () => {
    it("throws RangeError when speed is 0", () => {
      const { player } = makePlayer();
      assert.throws(() => player.setSpeed(0), RangeError);
    });

    it("throws RangeError when speed is negative", () => {
      const { player } = makePlayer();
      assert.throws(() => player.setSpeed(-1), RangeError);
    });

    it("accepts 1.0 (normal speed)", () => {
      const v = makeMockVideoElement();
      const { player } = makePlayer(v);
      player.setSpeed(1.0);
      assert.equal(v.playbackRate, 1.0);
    });

    it("accepts 2.0 (double speed)", () => {
      const v = makeMockVideoElement();
      const { player } = makePlayer(v);
      player.setSpeed(2.0);
      assert.equal(v.playbackRate, 2.0);
    });

    it("accepts 0.5 (half speed)", () => {
      const v = makeMockVideoElement();
      const { player } = makePlayer(v);
      player.setSpeed(0.5);
      assert.equal(v.playbackRate, 0.5);
    });
  });

  // ── stop ─────────────────────────────────────────────────────────────────

  describe("stop", () => {
    it("sets state to stopped and resets position", () => {
      const v = makeMockVideoElement();
      const { player } = makePlayer(v);
      player.stop();
      assert.equal(player.state, "stopped");
      assert.equal(v.currentTime, 0);
    });
  });

  // ── Event-driven state transitions ────────────────────────────────────────

  describe("state transitions via DOM events", () => {
    it("state becomes playing when video fires playing event", () => {
      const v = makeMockVideoElement();
      const { player } = makePlayer(v);
      v._emit("playing");
      assert.equal(player.state, "playing");
    });

    it("state becomes paused when video fires pause event", () => {
      const v = makeMockVideoElement();
      const { player } = makePlayer(v);
      v._emit("playing");
      v._emit("pause");
      assert.equal(player.state, "paused");
    });

    it("state becomes stopped when video fires ended event", () => {
      const v = makeMockVideoElement();
      const { player } = makePlayer(v);
      v._emit("playing");
      v._emit("ended");
      assert.equal(player.state, "stopped");
    });

    it("state becomes error when video fires error event", () => {
      const v = makeMockVideoElement();
      const { player } = makePlayer(v);
      v._emit("error");
      assert.equal(player.state, "error");
    });
  });

  // ── positionMs ────────────────────────────────────────────────────────────

  describe("positionMs", () => {
    it("reflects currentTime in milliseconds", () => {
      const v = makeMockVideoElement();
      const { player } = makePlayer(v);
      v.currentTime = 30.5;
      assert.equal(player.positionMs, 30_500);
    });

    it("floors to integer milliseconds", () => {
      const v = makeMockVideoElement();
      const { player } = makePlayer(v);
      v.currentTime = 1.9999;
      assert.equal(player.positionMs, 1_999);
    });
  });
});
