// SPDX-License-Identifier: MIT
import { ForgeService } from "./ForgeService.js";
import { ForgeEntry } from "./types.js";

const NPM_REGISTRY = "https://registry.npmjs.org";

/**
 * Intercepts npm registry HTTP calls and routes them through the Aether Forge
 * cache.  If the package is already cached locally the bytes are returned
 * immediately without touching the internet.  On a cache miss the request is
 * forwarded to the npm registry, the response is stored in the Forge cache,
 * and then returned to the caller.
 */
export class NpmProxy {
  private readonly _forge: ForgeService;

  constructor(forge: ForgeService) {
    this._forge = forge;
  }

  /**
   * Resolves an npm package tarball by name and version.
   *
   * @param name    Package name (e.g. `react` or `@types/node`).
   * @param version Exact version string (e.g. `18.2.0`).
   * @returns The package tarball as a `Uint8Array`.
   */
  async resolve(name: string, version: string): Promise<Uint8Array> {
    const packageId = `npm:${name}@${version}`;

    // Check Forge cache first.
    const entry = await this._forge.query(packageId);
    if (entry !== null) {
      const cached = await this._forge.fetch(entry.contentHash);
      if (cached !== null) {
        return cached;
      }
    }

    // Cache miss — fetch from npm registry.
    const tarballUrl = await this._resolveTarballUrl(name, version);
    const res = await fetch(tarballUrl);

    if (!res.ok) {
      throw new Error(
        `NpmProxy: failed to fetch ${name}@${version} from npm registry: ` +
        `${res.status} ${res.statusText}`,
      );
    }

    const buffer  = await res.arrayBuffer();
    const bytes   = new Uint8Array(buffer);
    const hash    = await this._sha256Hex(bytes);

    // Store in Forge cache for future requests.
    await this._forge.cache(packageId, bytes, hash);

    return bytes;
  }

  /**
   * Resolves the tarball download URL for a given npm package version by
   * querying the npm registry metadata endpoint.
   */
  private async _resolveTarballUrl(name: string, version: string): Promise<string> {
    const encodedName = name.startsWith("@")
      ? `@${encodeURIComponent(name.slice(1))}`
      : encodeURIComponent(name);

    const metaUrl = `${NPM_REGISTRY}/${encodedName}/${version}`;
    const res     = await fetch(metaUrl, {
      headers: { Accept: "application/json" },
    });

    if (!res.ok) {
      throw new Error(
        `NpmProxy: failed to fetch metadata for ${name}@${version}: ` +
        `${res.status} ${res.statusText}`,
      );
    }

    const meta = (await res.json()) as { dist?: { tarball?: string } };
    const tarball = meta?.dist?.tarball;

    if (!tarball) {
      throw new Error(
        `NpmProxy: no tarball URL found in metadata for ${name}@${version}`,
      );
    }

    return tarball;
  }

  /** Computes a lowercase hex SHA-256 digest using the Web Crypto API. */
  private async _sha256Hex(data: Uint8Array): Promise<string> {
    const hashBuffer = await globalThis.crypto.subtle.digest("SHA-256", data);
    const hashArray  = Array.from(new Uint8Array(hashBuffer));
    return hashArray.map((b) => b.toString(16).padStart(2, "0")).join("");
  }

  /**
   * Exposes the resolved {@link ForgeEntry} for a package without downloading
   * the tarball.  Useful for checking cache status before committing to a
   * full fetch.
   */
  async stat(name: string, version: string): Promise<ForgeEntry | null> {
    return this._forge.query(`npm:${name}@${version}`);
  }
}
