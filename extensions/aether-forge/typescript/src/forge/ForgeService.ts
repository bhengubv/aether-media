// SPDX-License-Identifier: MIT
import { ForgeEntry, ForgeStats } from "./types.js";

/**
 * Client-side facade for the Aether Forge distributed package-cache layer.
 */
export class ForgeService {
  private readonly _baseUrl: string;

  constructor(baseUrl: string = "http://localhost:5281") {
    this._baseUrl = baseUrl.replace(/\/+$/, "");
  }

  /**
   * Looks up a package in the local Forge cache by its fully-qualified
   * package ID (e.g. `npm:react@18.2.0`).
   * Returns `null` when the package is not cached.
   */
  async query(packageId: string): Promise<ForgeEntry | null> {
    const params = new URLSearchParams({ package_id: packageId });
    const res = await fetch(`${this._baseUrl}/forge/query?${params}`);

    if (res.status === 404) return null;
    if (!res.ok) {
      throw new Error(`ForgeService.query failed: ${res.status} ${res.statusText}`);
    }

    return res.json() as Promise<ForgeEntry>;
  }

  /**
   * Stores a package payload in the local Forge cache.
   *
   * @param packageId   Ecosystem-prefixed package identifier.
   * @param content     Raw package bytes.
   * @param contentHash Pre-computed SHA-256 hex digest of `content`.
   */
  async cache(
    packageId: string,
    content: Uint8Array | Blob,
    contentHash: string,
  ): Promise<ForgeEntry> {
    const form = new FormData();
    form.append("package_id",   packageId);
    form.append("content_hash", contentHash);
    form.append("content", content instanceof Blob ? content : new Blob([content]));

    const res = await fetch(`${this._baseUrl}/forge/cache`, {
      method: "POST",
      body: form,
    });

    if (!res.ok) {
      throw new Error(`ForgeService.cache failed: ${res.status} ${res.statusText}`);
    }

    return res.json() as Promise<ForgeEntry>;
  }

  /**
   * Fetches the raw bytes of a cached package by its content hash.
   * Returns `null` when the content is not found locally.
   */
  async fetch(contentHash: string): Promise<Uint8Array | null> {
    const params = new URLSearchParams({ content_hash: contentHash });
    const res = await fetch(`${this._baseUrl}/forge/fetch?${params}`);

    if (res.status === 404) return null;
    if (!res.ok) {
      throw new Error(`ForgeService.fetch failed: ${res.status} ${res.statusText}`);
    }

    const buffer = await res.arrayBuffer();
    return new Uint8Array(buffer);
  }

  /**
   * Returns aggregate statistics for the local Forge cache node.
   */
  async getStats(): Promise<ForgeStats> {
    const res = await fetch(`${this._baseUrl}/forge/stats`);

    if (!res.ok) {
      throw new Error(`ForgeService.getStats failed: ${res.status} ${res.statusText}`);
    }

    return res.json() as Promise<ForgeStats>;
  }
}
