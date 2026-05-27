// SPDX-License-Identifier: MIT
import { VaultManifest, VaultHealth, VaultShardRequest } from "./types.js";

type ShardRequestCallback = (request: VaultShardRequest) => void;

/**
 * Client-side facade for the Aether Vault distributed encrypted storage layer.
 *
 * Call {@link store} to encrypt and store a file, {@link recover} to
 * reconstruct it from shards, {@link checkHealth} to probe shard reachability,
 * and {@link replicate} to ensure redundancy targets are met.
 */
export class VaultService {
  /** Invoked whenever a shard request arrives from another mesh node. */
  onShardRequested: ShardRequestCallback | null = null;

  private readonly _baseUrl: string;

  constructor(baseUrl: string = "http://localhost:5290") {
    this._baseUrl = baseUrl.replace(/\/+$/, "");
  }

  /**
   * Encrypts and erasure-codes the provided content, distributes the shards
   * across mesh nodes, and returns the manifest required to recover the file.
   *
   * @param content  File content as a {@link Blob} or {@link Uint8Array}.
   * @param label    Human-readable label for the vault entry.
   */
  async store(content: Blob | Uint8Array, label: string): Promise<VaultManifest> {
    const form = new FormData();
    form.append("label", label);
    form.append("content", content instanceof Blob ? content : new Blob([content]));

    const res = await fetch(`${this._baseUrl}/vault/store`, {
      method: "POST",
      body: form,
    });

    if (!res.ok) {
      throw new Error(`VaultService.store failed: ${res.status} ${res.statusText}`);
    }

    return res.json() as Promise<VaultManifest>;
  }

  /**
   * Locates and reassembles the shards described by {@link manifest},
   * decrypts them, and returns the plaintext content as a {@link Blob}.
   * Requires at least {@link VaultManifest.k} reachable shards.
   *
   * @param manifest  The manifest identifying the stored file.
   */
  async recover(manifest: VaultManifest): Promise<Blob> {
    const res = await fetch(`${this._baseUrl}/vault/recover`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(manifest),
    });

    if (!res.ok) {
      throw new Error(`VaultService.recover failed: ${res.status} ${res.statusText}`);
    }

    return res.blob();
  }

  /**
   * Probes the mesh for shards described by {@link manifest} and returns
   * a {@link VaultHealth} snapshot.
   *
   * @param manifest  The manifest to check.
   */
  async checkHealth(manifest: VaultManifest): Promise<VaultHealth> {
    const res = await fetch(`${this._baseUrl}/vault/health`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(manifest),
    });

    if (!res.ok) {
      throw new Error(`VaultService.checkHealth failed: ${res.status} ${res.statusText}`);
    }

    return res.json() as Promise<VaultHealth>;
  }

  /**
   * Ensures that at least {@link targetReplicas} copies of each shard are
   * held across distinct mesh nodes, creating new copies as needed.
   *
   * @param manifest        The manifest describing the file to replicate.
   * @param targetReplicas  Desired replication factor per shard.
   */
  async replicate(manifest: VaultManifest, targetReplicas: number): Promise<void> {
    const res = await fetch(`${this._baseUrl}/vault/replicate`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ manifest, targetReplicas }),
    });

    if (!res.ok) {
      throw new Error(`VaultService.replicate failed: ${res.status} ${res.statusText}`);
    }
  }
}
