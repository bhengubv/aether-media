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
  sentAt: Date;
}

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
  sentAt: Date,
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

  return { reactionId, contentHash, fromUhid, type, positionMs, message, sentAt };
}
