// SPDX-License-Identifier: MIT

/** Semantic classification of a {@link SpaceBreadcrumb}. */
export enum BreadcrumbType {
  /** General-purpose notice or announcement. */
  Notice     = 0,
  /**
   * Emergency alert. Recipients must flood this type beyond the normal
   * 3-cell radius constraint.
   */
  Emergency  = 1,
  /** Commercial listing or advertisement anchored to a location. */
  Commerce   = 2,
  /** Scheduled or live event. */
  Event      = 3,
  /** Job posting anchored to a physical location. */
  JobPosting = 4,
}

/**
 * A Geohash cell identifier — a variable-length alphanumeric string where
 * longer values encode higher geographic precision.
 */
export type GeoHash = string;

/** An immutable geo-anchored content record on the Aether Space layer. */
export interface SpaceBreadcrumb {
  /** SHA-256 hex digest of the payload bytes. */
  contentHash: string;
  /** Geohash cell in which the breadcrumb was dropped. */
  geoHash: GeoHash;
  /** Universal host ID of the node that dropped this breadcrumb. */
  anchorUhid: string;
  /** UTC timestamp of creation (ISO-8601 string). */
  createdAtUtc: string;
  /** Time-to-live in hours before the breadcrumb is considered expired. */
  ttlHours: number;
  /** Semantic classification. */
  type: BreadcrumbType;
  /** Ed25519 signature as Base64. */
  signatureB64: string;
}
