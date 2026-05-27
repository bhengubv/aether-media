// SPDX-License-Identifier: MIT
import { PoVToken, PoVScore } from "./types.js";

type TokenCallback = (token: PoVToken) => void;

/**
 * Client-side facade for the Aether Market Proof-of-Vicinity service.
 *
 * Call {@link issueToken} to initiate a proximity handshake,
 * {@link acceptToken} to record an inbound token, {@link getScore} to query
 * reputation, and {@link reportDefection} to flag a bad actor.
 */
export class PoVService {
  /** Invoked whenever a PoV token arrives from a nearby mesh node. */
  onTokenReceived: TokenCallback | null = null;

  private readonly _baseUrl: string;

  constructor(baseUrl: string = "http://localhost:5290") {
    this._baseUrl = baseUrl.replace(/\/+$/, "");
  }

  /**
   * Initiates a Proof-of-Vicinity handshake with the device identified by
   * {@link subjectAetherTag} and returns the signed token on success.
   *
   * @param subjectAetherTag  The @tag of the device to witness.
   */
  async issueToken(subjectAetherTag: string): Promise<PoVToken> {
    const res = await fetch(`${this._baseUrl}/market/pov/issue`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ subjectAetherTag }),
    });

    if (!res.ok) {
      throw new Error(`PoVService.issueToken failed: ${res.status} ${res.statusText}`);
    }

    return res.json() as Promise<PoVToken>;
  }

  /**
   * Accepts an inbound {@link PoVToken} from a witnessing node, verifies both
   * signatures, and incorporates it into the local PoV score.
   *
   * @param token  The token to accept.
   */
  async acceptToken(token: PoVToken): Promise<void> {
    const res = await fetch(`${this._baseUrl}/market/pov/accept`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(token),
    });

    if (!res.ok) {
      throw new Error(`PoVService.acceptToken failed: ${res.status} ${res.statusText}`);
    }
  }

  /**
   * Returns the current decay-adjusted {@link PoVScore} for the node
   * identified by {@link uhid}.
   *
   * @param uhid  Universal host ID of the node to query.
   */
  async getScore(uhid: string): Promise<PoVScore> {
    const params = new URLSearchParams({ uhid });
    const res = await fetch(`${this._baseUrl}/market/pov/score?${params}`);

    if (!res.ok) {
      throw new Error(`PoVService.getScore failed: ${res.status} ${res.statusText}`);
    }

    return res.json() as Promise<PoVScore>;
  }

  /**
   * Verifies the cryptographic signatures on {@link token} and returns
   * `true` when both signatures are valid.
   *
   * @param token  The token to verify.
   */
  async verifyToken(token: PoVToken): Promise<boolean> {
    const res = await fetch(`${this._baseUrl}/market/pov/verify`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(token),
    });

    if (!res.ok) {
      throw new Error(`PoVService.verifyToken failed: ${res.status} ${res.statusText}`);
    }

    const body = await res.json() as { valid: boolean };
    return body.valid;
  }

  /**
   * Reports a node that defected from a confirmed trade.
   *
   * @param uhid      Universal host ID of the defecting node.
   * @param evidence  Signed evidence of defection (serialised TradeEscrow or artefact).
   */
  async reportDefection(uhid: string, evidence: string): Promise<void> {
    const res = await fetch(`${this._baseUrl}/market/pov/defection`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ uhid, evidence }),
    });

    if (!res.ok) {
      throw new Error(`PoVService.reportDefection failed: ${res.status} ${res.statusText}`);
    }
  }
}
