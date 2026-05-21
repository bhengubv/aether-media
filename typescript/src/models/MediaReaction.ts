export enum MediaReactionType {
  Like       = 1,
  Share      = 2,
  Comment    = 3,
  SuperReact = 4,
}

export interface MediaReaction {
  reactionId: string;      // UUID string
  contentHash: string;
  fromUhid: string;
  type: MediaReactionType;
  positionMs: number;
  message: string | null;  // required for Comment, null otherwise
  sentAtMs: number;        // unix milliseconds
}

// ── Wire format ───────────────────────────────────────────────────────────────

/** Snake_case JSON representation of MediaReaction as sent/received on the wire. */
export interface MediaReactionWire {
  reaction_id: string;
  content_hash: string;
  from_uhid: string;
  type: string;            // lowercase enum string: "like" | "share" | "comment" | "super_react"
  position_ms: number;
  message: string | null;
  sent_at_ms: number;
}

const reactionTypeToWire: Record<MediaReactionType, string> = {
  [MediaReactionType.Like]:       "like",
  [MediaReactionType.Share]:      "share",
  [MediaReactionType.Comment]:    "comment",
  [MediaReactionType.SuperReact]: "super_react",
};

const wireToReactionType: Record<string, MediaReactionType> = {
  like:        MediaReactionType.Like,
  share:       MediaReactionType.Share,
  comment:     MediaReactionType.Comment,
  super_react: MediaReactionType.SuperReact,
};

export function toWire(reaction: MediaReaction): MediaReactionWire {
  return {
    reaction_id:  reaction.reactionId,
    content_hash: reaction.contentHash,
    from_uhid:    reaction.fromUhid,
    type:         reactionTypeToWire[reaction.type],
    position_ms:  reaction.positionMs,
    message:      reaction.message,
    sent_at_ms:   reaction.sentAtMs,
  };
}

export function fromWire(obj: MediaReactionWire): MediaReaction {
  const type = wireToReactionType[obj.type];
  if (type === undefined) throw new Error(`Unknown reaction type: ${obj.type}`);
  return {
    reactionId:  obj.reaction_id,
    contentHash: obj.content_hash,
    fromUhid:    obj.from_uhid,
    type,
    positionMs:  obj.position_ms,
    message:     obj.message,
    sentAtMs:    obj.sent_at_ms,
  };
}

// ── Factory ───────────────────────────────────────────────────────────────────

/**
 * Factory that validates the reaction rules before constructing the object.
 *
 * - Comment requires a non-empty message.
 * - All other types must have message === null.
 */
export function createReaction(
  reactionId: string,
  contentHash: string,
  fromUhid: string,
  type: MediaReactionType,
  positionMs: number,
  message: string | null,
  sentAtMs: number,
): MediaReaction {
  if (!contentHash.trim()) throw new Error("contentHash must not be empty");
  if (!fromUhid.trim())   throw new Error("fromUhid must not be empty");
  if (positionMs < 0)     throw new Error("positionMs must be >= 0");

  if (type === MediaReactionType.Comment) {
    if (!message || !message.trim()) {
      throw new Error("A message is required for Comment reactions");
    }
  } else {
    if (message !== null) {
      throw new Error(`message must be null for ${MediaReactionType[type]} reactions`);
    }
  }

  return { reactionId, contentHash, fromUhid, type, positionMs, message, sentAtMs };
}
