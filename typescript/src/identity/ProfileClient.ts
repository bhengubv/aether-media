import type { MediaProfile, MediaProfileWire } from "../models/MediaProfile.js";
import { fromWire } from "../models/MediaProfile.js";

const LOCAL_PROFILE_KEY = "aethermesh_local_profile";

/**
 * Manages media creator profiles, caching them in memory to avoid
 * repeated round-trips to the Aether relay.
 */
export class ProfileClient {
  private readonly baseUrl: string;
  private readonly cache = new Map<string, MediaProfile>();

  constructor(baseUrl: string = "https://relay.aethermesh.network/media") {
    this.baseUrl = baseUrl.replace(/\/$/, "");
  }

  /**
   * Fetch a profile by UHID.  Returns cached copy if available.
   */
  async getProfile(uhid: string): Promise<MediaProfile> {
    if (!uhid.trim()) throw new Error("uhid must not be empty");

    const cached = this.cache.get(uhid);
    if (cached) return cached;

    const response = await fetch(
      `${this.baseUrl}/profiles/${encodeURIComponent(uhid)}`,
      { headers: { Accept: "application/json" } },
    );

    if (!response.ok) {
      throw new Error(`getProfile failed: ${response.status} ${response.statusText}`);
    }

    const profile = fromWire(await response.json() as MediaProfileWire);
    this.cache.set(uhid, profile);
    return profile;
  }

  /**
   * Load the local device profile from localStorage, then verify it with
   * the relay to get fresh follower/following counts.
   *
   * Returns null if the device has not yet completed identity setup.
   */
  async getLocalProfile(): Promise<MediaProfile | null> {
    try {
      const raw = localStorage.getItem(LOCAL_PROFILE_KEY);
      if (!raw) return null;

      const local: MediaProfile = JSON.parse(raw) as MediaProfile;

      // Refresh from network (non-blocking — return stale if network fails)
      this.getProfile(local.uhid).then((fresh) => {
        this.cache.set(fresh.uhid, fresh);
        try {
          localStorage.setItem(LOCAL_PROFILE_KEY, JSON.stringify(fresh));
        } catch {
          // ignore quota
        }
      }).catch(() => {/* use stale */});

      return local;
    } catch {
      return null;
    }
  }

  /**
   * Update the local profile's displayName and bio on the relay and in
   * localStorage.  Returns the updated profile.
   */
  async updateProfile(displayName: string, bio: string): Promise<MediaProfile> {
    if (!displayName.trim()) throw new Error("displayName must not be empty");

    const local = await this.getLocalProfile();
    if (!local) throw new Error("No local profile — identity not set up");

    const response = await fetch(`${this.baseUrl}/profiles/${encodeURIComponent(local.uhid)}`, {
      method:  "PATCH",
      headers: { "Content-Type": "application/json" },
      body:    JSON.stringify({ displayName, bio }),
    });

    if (!response.ok) {
      throw new Error(`updateProfile failed: ${response.status} ${response.statusText}`);
    }

    const updated = fromWire(await response.json() as MediaProfileWire);
    this.cache.set(updated.uhid, updated);

    try {
      localStorage.setItem(LOCAL_PROFILE_KEY, JSON.stringify(updated));
    } catch {
      // ignore
    }

    return updated;
  }

}
