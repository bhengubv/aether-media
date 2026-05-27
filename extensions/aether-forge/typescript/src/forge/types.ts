// SPDX-License-Identifier: MIT

/** A single package cached in the Aether Forge distributed package-cache layer. */
export interface ForgeEntry {
  /** SHA-256 hex digest of the cached package bytes. */
  contentHash: string;
  /**
   * Fully-qualified package identifier in the form `ecosystem:name@version`,
   * e.g. `npm:react@18.2.0`.
   */
  packageId: string;
  /** ISO-8601 UTC timestamp of when the package was first cached. */
  fetchedAtUtc: string;
  /** Byte length of the cached package payload. */
  sizeBytes: number;
  /** Number of times this entry has been served from the local Forge cache. */
  downloadCount: number;
}

/** Aggregate statistics for the local Aether Forge cache node. */
export interface ForgeStats {
  /** Total bytes served from the local cache (bandwidth saved). */
  totalBytesSaved: number;
  /** Number of distinct peer nodes that downloaded from this node. */
  totalPeersServed: number;
  /** Number of unique package entries in the local cache. */
  catalogueSize: number;
  /** Most-downloaded packages, ordered by downloadCount descending. */
  topPackages: ForgeEntry[];
}
