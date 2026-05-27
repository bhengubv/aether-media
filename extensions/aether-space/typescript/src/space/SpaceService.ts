// SPDX-License-Identifier: MIT
import { BreadcrumbType, GeoHash, SpaceBreadcrumb } from "./types.js";

type BreadcrumbCallback = (breadcrumb: SpaceBreadcrumb) => void;

/**
 * Client-side facade for the Aether Space geo-anchored breadcrumb layer.
 *
 * Call {@link drop} to publish a breadcrumb at a geohash cell, {@link scan}
 * to query nearby breadcrumbs, {@link pin} to cache a known breadcrumb
 * locally, and {@link delete_} to retract one.
 */
export class SpaceService {
  /** Invoked whenever a breadcrumb is received from the mesh. */
  onBreadcrumbReceived: BreadcrumbCallback | null = null;

  private readonly _baseUrl: string;

  constructor(baseUrl: string = "http://localhost:5280") {
    this._baseUrl = baseUrl.replace(/\/+$/, "");
  }

  /**
   * Creates and broadcasts a new breadcrumb at {@link geoHash}.
   *
   * @param geoHash   Target geohash cell.
   * @param content   Payload as a `Uint8Array` or `Blob`.
   * @param type      Semantic type of the breadcrumb.
   * @param ttlHours  Time-to-live in hours (must be > 0).
   */
  async drop(
    geoHash: GeoHash,
    content: Uint8Array | Blob,
    type: BreadcrumbType,
    ttlHours: number,
  ): Promise<SpaceBreadcrumb> {
    const form = new FormData();
    form.append("geo_hash", geoHash);
    form.append("type", String(type));
    form.append("ttl_hours", String(ttlHours));
    form.append("content", content instanceof Blob ? content : new Blob([content]));

    const res = await fetch(`${this._baseUrl}/space/drop`, {
      method: "POST",
      body: form,
    });

    if (!res.ok) {
      throw new Error(`SpaceService.drop failed: ${res.status} ${res.statusText}`);
    }

    const breadcrumb: SpaceBreadcrumb = await res.json();
    this.onBreadcrumbReceived?.(breadcrumb);
    return breadcrumb;
  }

  /**
   * Returns all live breadcrumbs within {@link radiusCells} cells of
   * {@link geoHash}.
   *
   * @param geoHash      Centre cell to scan from.
   * @param radiusCells  Number of cells to expand outward (default 3).
   */
  async scan(geoHash: GeoHash, radiusCells: number = 3): Promise<SpaceBreadcrumb[]> {
    const params = new URLSearchParams({
      geo_hash:     geoHash,
      radius_cells: String(radiusCells),
    });

    const res = await fetch(`${this._baseUrl}/space/scan?${params}`);

    if (!res.ok) {
      throw new Error(`SpaceService.scan failed: ${res.status} ${res.statusText}`);
    }

    return res.json() as Promise<SpaceBreadcrumb[]>;
  }

  /**
   * Pins a pre-existing breadcrumb to the local node's cache without
   * re-broadcasting it.
   */
  async pin(breadcrumb: SpaceBreadcrumb): Promise<void> {
    const res = await fetch(`${this._baseUrl}/space/pin`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(breadcrumb),
    });

    if (!res.ok) {
      throw new Error(`SpaceService.pin failed: ${res.status} ${res.statusText}`);
    }
  }

  /**
   * Deletes a breadcrumb from the local cache and broadcasts a retract
   * message to neighbouring nodes.
   */
  async delete_(breadcrumb: SpaceBreadcrumb): Promise<void> {
    const res = await fetch(`${this._baseUrl}/space/delete`, {
      method: "DELETE",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ content_hash: breadcrumb.contentHash }),
    });

    if (!res.ok) {
      throw new Error(`SpaceService.delete_ failed: ${res.status} ${res.statusText}`);
    }
  }
}
