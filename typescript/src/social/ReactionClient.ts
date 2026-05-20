import { type MediaReaction } from "../models/MediaReaction.js";

/**
 * Sends reactions via the Aether protocol transport and retrieves
 * aggregated reactions for a piece of content.
 *
 * Reactions are serialised to JSON and dispatched over the Aether relay
 * HTTP API.  Future versions will use the native mesh transport once the
 * @bhengubv/aether-protocol package exposes a browser-compatible IPC.
 */
export class ReactionClient {
  private readonly baseUrl: string;

  constructor(baseUrl: string = "https://relay.aether.network/media") {
    this.baseUrl = baseUrl.replace(/\/$/, "");
  }

  /**
   * Send a reaction to the relay.  The relay broadcasts it to the creator
   * and all watching peers on the Aether mesh.
   */
  async sendReaction(reaction: MediaReaction): Promise<void> {
    if (!reaction.contentHash.trim()) throw new Error("contentHash must not be empty");
    if (!reaction.fromUhid.trim())    throw new Error("fromUhid must not be empty");

    const payload = {
      reactionId:  reaction.reactionId,
      contentHash: reaction.contentHash,
      fromUhid:    reaction.fromUhid,
      type:        reaction.type,
      positionMs:  reaction.positionMs,
      message:     reaction.message,
      sentAt:      reaction.sentAt.toISOString(),
    };

    const response = await fetch(`${this.baseUrl}/reactions`, {
      method:  "POST",
      headers: { "Content-Type": "application/json" },
      body:    JSON.stringify(payload),
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

    const raw: Array<{
      reactionId: string;
      contentHash: string;
      fromUhid: string;
      type: number;
      positionMs: number;
      message: string | null;
      sentAt: string;
    }> = await response.json();

    return raw.map((r) => ({
      reactionId:  r.reactionId,
      contentHash: r.contentHash,
      fromUhid:    r.fromUhid,
      type:        r.type,
      positionMs:  r.positionMs,
      message:     r.message,
      sentAt:      new Date(r.sentAt),
    }));
  }
}
