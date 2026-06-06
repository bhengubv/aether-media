const SHORT_BIO_MAX = 120;

export interface MediaProfile {
  uhid: string;
  displayName: string;
  avatarHash: string | null;
  bio: string | null;
  aethermeshTag: string;
  followerCount: number;
  followingCount: number;
  contentCount: number;
  isVerified: boolean;
  joinedAtMs: number;  // unix milliseconds
}

// ── Wire format ───────────────────────────────────────────────────────────────

/** Snake_case JSON representation of MediaProfile as sent/received on the wire. */
export interface MediaProfileWire {
  uhid: string;
  display_name: string;
  avatar_hash: string | null;
  bio: string | null;
  aethermesh_tag: string;
  follower_count: number;
  following_count: number;
  content_count: number;
  is_verified: boolean;
  joined_at_ms: number;
}

export function toWire(profile: MediaProfile): MediaProfileWire {
  return {
    uhid:            profile.uhid,
    display_name:    profile.displayName,
    avatar_hash:     profile.avatarHash,
    bio:             profile.bio,
    aethermesh_tag:      profile.aethermeshTag,
    follower_count:  profile.followerCount,
    following_count: profile.followingCount,
    content_count:   profile.contentCount,
    is_verified:     profile.isVerified,
    joined_at_ms:    profile.joinedAtMs,
  };
}

export function fromWire(obj: MediaProfileWire): MediaProfile {
  return {
    uhid:           obj.uhid,
    displayName:    obj.display_name,
    avatarHash:     obj.avatar_hash,
    bio:            obj.bio,
    aethermeshTag:      obj.aethermesh_tag,
    followerCount:  obj.follower_count,
    followingCount: obj.following_count,
    contentCount:   obj.content_count,
    isVerified:     obj.is_verified,
    joinedAtMs:     obj.joined_at_ms,
  };
}

// ── Helpers ───────────────────────────────────────────────────────────────────

/**
 * Returns the bio trimmed to 120 characters, cutting at the last word
 * boundary and appending "…" when truncated.  Returns "" when bio is null
 * or whitespace.
 */
export function shortBio(profile: MediaProfile): string {
  const bio = profile.bio?.trim();
  if (!bio) return "";

  if (bio.length <= SHORT_BIO_MAX) return bio;

  const cut = bio.slice(0, SHORT_BIO_MAX);
  const lastSpace = cut.lastIndexOf(" ");
  const boundary = lastSpace > 0 ? lastSpace : SHORT_BIO_MAX;
  return cut.slice(0, boundary).trimEnd() + "…";
}
