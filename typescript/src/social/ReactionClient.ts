import { type MediaReaction, type MediaReactionWire, toWire, fromWire } from "../models/MediaReaction.js";

/**
 * Sends reactions via the Aether protocol transport and retrieves
 * aggregated reactions for a piece of content.
 *
 * Reactions are serialised to JSON and dispatched over the Aether relay
 * HTTP API.  Future versions will use the native mesh transport once the
 * @bhengubv/aethernet-protocol package exposes a browser-compatible IPC.
 */
export class ReactionClient {
  private readonly baseUrl: string;

  constructor(baseUrl: string = "https://relay.aethernet.network/media") {
    this.baseUrl = baseUrl.replace(/\/$/, "");
  }

  /**
   * Send a reaction to the relay.  The relay broadcasts it to the creator
   * and all watching peers on the Aether mesh.
   */
  async sendReaction(reaction: MediaReaction): Promise<void> {
    if (!reaction.contentHash.trim()) throw new Error("contentHash must not be empty");
    if (!reaction.fromUhid.trim())    throw new Error("fromUhid must not be empty");

    const response = await fetch(`${this.baseUrl}/reactions`, {
      method:  "POST",
      headers: { "Content-Type": "application/json" },
      body:    JSON.stringify(toWire(reaction)),
    });

    if (!response.ok) {
      throw new Error(`sendReaction failed: ${response.status} ${response.statusText}`);
    }
  }

  /**
   * Retrieve all reactions for the given contentHash, sorted by positionMs.
   */
  async getReactions(contentHash: string): Promise<MediaReaction[]> {
    if (!contentHash.trim()) throw new Error("contentHash must not be empty");

    const response = await fetch(
      `${this.baseUrl}/reactions/${encodeURIComponent(contentHash)}`,
      { headers: { Accept: "application/json" } },
    );

    if (!response.ok) {
      throw new Error(`getReactions failed: ${response.status} ${response.statusText}`);
    }

    const raw: MediaReactionWire[] = await response.json();
    return raw.map(fromWire);
  }
}
