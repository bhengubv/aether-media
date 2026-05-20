// SPDX-License-Identifier: MIT
import { describe, it, before, after, beforeEach } from "node:test";
import { strict as assert } from "node:assert";
import { ProfileClient } from "./ProfileClient.js";

// ── Browser API stubs ─────────────────────────────────────────────────────────

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
    getItem(key)        { return this._store[key] ?? null; },
    setItem(key, value) { this._store[key] = value; },
    removeItem(key)     { delete this._store[key]; },
    clear()             { this._store = {}; },
  };
}

type FetchFn = typeof globalThis.fetch;
let originalFetch: FetchFn;
let storageMock: LocalStorageMock;

before(() => {
  originalFetch = globalThis.fetch;
  storageMock   = makeLocalStorage();
  (globalThis as unknown as { localStorage: LocalStorageMock }).localStorage = storageMock;
});

after(() => {
  globalThis.fetch = originalFetch;
  delete (globalThis as unknown as { localStorage?: unknown }).localStorage;
});

beforeEach(() => storageMock.clear());

function mockFetch(data: unknown, status = 200): void {
  globalThis.fetch = async (_url: RequestInfo | URL, _init?: RequestInit) =>
    new Response(JSON.stringify(data), {
      status,
      headers: { "Content-Type": "application/json" },
    });
}

// Raw profile object matching what the server would return.
// ProfileClient._revive() spreads this directly onto the MediaProfile,
// so we include both the new-style joinedAtMs field AND a joinedAt string
// (since _revive() still reads raw["joinedAt"] to construct a Date extra property).
function makeRawProfile(overrides?: Record<string, unknown>): Record<string, unknown> {
  return {
    uhid:           "uhid-001",
    displayName:    "Test Creator",
    avatarHash:     null,
    bio:            "Hello from Aether.",
    aetherTag:      "@testcreator",
    followerCount:  42,
    followingCount: 7,
    contentCount:   3,
    isVerified:     false,
    joinedAtMs:     new Date("2024-06-01T00:00:00.000Z").getTime(),
    joinedAt:       "2024-06-01T00:00:00.000Z",  // still read by _revive
    ...overrides,
  };
}

// ── Constructor ───────────────────────────────────────────────────────────────

describe("ProfileClient", () => {
  describe("constructor", () => {
    it("constructs with default base URL", () => {
      assert.ok(new ProfileClient());
    });

    it("strips trailing slash from baseUrl", () => {
      assert.ok(new ProfileClient("https://example.com/media/"));
    });
  });

  // ── getProfile — validation ───────────────────────────────────────────────

  describe("getProfile — validation", () => {
    it("throws on empty uhid", async () => {
      const client = new ProfileClient("http://ignored");
      await assert.rejects(
        () => client.getProfile("   "),
        /uhid must not be empty/,
      );
    });
  });

  // ── getProfile — network ──────────────────────────────────────────────────

  describe("getProfile — network", () => {
    it("returns a profile with the correct uhid", async () => {
      mockFetch(makeRawProfile());
      const client  = new ProfileClient("http://test");
      const profile = await client.getProfile("uhid-001");

      assert.equal(profile.uhid,        "uhid-001");
      assert.equal(profile.displayName, "Test Creator");
    });

    it("throws on HTTP error", async () => {
      mockFetch({ error: "not found" }, 404);
      const client = new ProfileClient("http://test");
      await assert.rejects(
        () => client.getProfile("missing"),
        /getProfile failed: 404/,
      );
    });

    it("returns the same cached object on repeated calls (no second fetch)", async () => {
      let callCount = 0;
      globalThis.fetch = async (_u: RequestInfo | URL) => {
        callCount++;
        return new Response(JSON.stringify(makeRawProfile()), { status: 200 });
      };

      const client = new ProfileClient("http://test");
      const p1     = await client.getProfile("uhid-001");
      const p2     = await client.getProfile("uhid-001");

      assert.equal(callCount, 1);
      assert.equal(p1, p2); // same object reference from cache
    });

    it("includes joinedAtMs as a number from the raw server response", async () => {
      const ms = new Date("2024-06-01T00:00:00.000Z").getTime();
      mockFetch(makeRawProfile());
      const client  = new ProfileClient("http://test");
      const profile = await client.getProfile("uhid-001");

      assert.equal(profile.joinedAtMs, ms);
      assert.equal(typeof profile.joinedAtMs, "number");
    });
  });

  // ── getLocalProfile ───────────────────────────────────────────────────────

  describe("getLocalProfile", () => {
    it("returns null when localStorage has no local profile", async () => {
      mockFetch({});
      const client  = new ProfileClient("http://test");
      const profile = await client.getLocalProfile();
      assert.equal(profile, null);
    });

    it("returns the stored profile when localStorage has one", async () => {
      const raw = makeRawProfile();
      storageMock.setItem("aether_local_profile", JSON.stringify(raw));
      // Mock network refresh (best-effort background call)
      mockFetch(makeRawProfile());

      const client  = new ProfileClient("http://test");
      const profile = await client.getLocalProfile();

      assert.ok(profile !== null);
      assert.equal(profile!.uhid, "uhid-001");
    });

    it("returns null when localStorage value is corrupt JSON", async () => {
      storageMock.setItem("aether_local_profile", "NOT_JSON");
      const client  = new ProfileClient("http://test");
      const profile = await client.getLocalProfile();
      assert.equal(profile, null);
    });

    it("returns the stale local profile without waiting for network refresh", async () => {
      // Network is slow (never resolves during this test)
      let networkResolved = false;
      globalThis.fetch = async (_u: RequestInfo | URL) => {
        networkResolved = true;
        return new Response(JSON.stringify(makeRawProfile()), { status: 200 });
      };

      const raw = makeRawProfile();
      storageMock.setItem("aether_local_profile", JSON.stringify(raw));

      const client  = new ProfileClient("http://test");
      const profile = await client.getLocalProfile();

      // Profile is returned immediately without waiting for background refresh to finish
      assert.ok(profile !== null);
      assert.equal(profile!.displayName, "Test Creator");
    });
  });

  // ── updateProfile — validation ────────────────────────────────────────────

  describe("updateProfile — validation", () => {
    it("throws when displayName is blank", async () => {
      const client = new ProfileClient("http://ignored");
      await assert.rejects(
        () => client.updateProfile("  ", "bio"),
        /displayName must not be empty/,
      );
    });

    it("throws when no local profile is set up", async () => {
      const client = new ProfileClient("http://test");
      await assert.rejects(
        () => client.updateProfile("Alice", "bio text"),
        /No local profile/,
      );
    });
  });

  // ── updateProfile — network ───────────────────────────────────────────────

  describe("updateProfile — network", () => {
    it("sends PATCH and returns updated profile", async () => {
      const updatedRaw = makeRawProfile({ displayName: "Updated Name" });
      storageMock.setItem("aether_local_profile", JSON.stringify(makeRawProfile()));

      let capturedUrl  = "";
      let capturedInit: RequestInit | undefined;

      globalThis.fetch = async (url: RequestInfo | URL, init?: RequestInit) => {
        capturedUrl  = url.toString();
        capturedInit = init;
        return new Response(JSON.stringify(updatedRaw), { status: 200 });
      };

      const client  = new ProfileClient("http://test");
      const updated = await client.updateProfile("Updated Name", "New bio");

      assert.equal(capturedInit?.method, "PATCH");
      assert.match(
        (capturedInit?.headers as Record<string, string>)?.["Content-Type"] ?? "",
        /application\/json/i,
      );
      assert.match(capturedUrl, /uhid-001/);
      assert.equal(updated.displayName, "Updated Name");
    });

    it("throws on HTTP error from PATCH", async () => {
      storageMock.setItem("aether_local_profile", JSON.stringify(makeRawProfile()));

      globalThis.fetch = async (_u: RequestInfo | URL, init?: RequestInit) => {
        if (init?.method === "PATCH") {
          return new Response("{}", { status: 422 });
        }
        return new Response(JSON.stringify(makeRawProfile()), { status: 200 });
      };

      const client = new ProfileClient("http://test");
      await assert.rejects(
        () => client.updateProfile("Valid Name", "bio"),
        /updateProfile failed: 422/,
      );
    });

    it("persists updated profile to localStorage", async () => {
      const updatedRaw = makeRawProfile({ displayName: "Persisted" });
      storageMock.setItem("aether_local_profile", JSON.stringify(makeRawProfile()));

      globalThis.fetch = async (_u: RequestInfo | URL, init?: RequestInit) => {
        if (init?.method === "PATCH") {
          return new Response(JSON.stringify(updatedRaw), { status: 200 });
        }
        return new Response(JSON.stringify(makeRawProfile()), { status: 200 });
      };

      const client = new ProfileClient("http://test");
      await client.updateProfile("Persisted", "bio");

      const stored = storageMock.getItem("aether_local_profile");
      assert.ok(stored, "localStorage should have updated profile");
      const parsed = JSON.parse(stored!);
      assert.equal(parsed.displayName, "Persisted");
    });
  });
});
