const SHORT_BIO_MAX = 120;

export interface MediaProfile {
  uhid: string;
  displayName: string;
  avatarHash: string | null;
  bio: string | null;
  aetherTagValue: string;
  followerCount: number;
  followingCount: number;
  contentCount: number;
  isVerified: boolean;
  joinedAt: Date;
}

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
