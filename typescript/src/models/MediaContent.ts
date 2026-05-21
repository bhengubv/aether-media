export interface MediaContent {
  contentHash: string;
  title: string;
  durationMs: number;
  codec: string;
  contentType: string;
  creatorUhid: string;
  sizeBytes: number;
  createdAtMs: number;
  thumbnailHash: string | null;
  tags: string[];
}

// ── Wire format ───────────────────────────────────────────────────────────────

/** Snake_case JSON representation of MediaContent as sent/received on the wire. */
export interface MediaContentWire {
  content_hash: string;
  title: string;
  duration_ms: number;
  codec: string;
  content_type: string;
  creator_uhid: string;
  size_bytes: number;
  created_at_ms: number;
  thumbnail_hash: string | null;
  tags: string[];
}

export function toWire(content: MediaContent): MediaContentWire {
  return {
    content_hash:   content.contentHash,
    title:          content.title,
    duration_ms:    content.durationMs,
    codec:          content.codec,
    content_type:   content.contentType,
    creator_uhid:   content.creatorUhid,
    size_bytes:     content.sizeBytes,
    created_at_ms:  content.createdAtMs,
    thumbnail_hash: content.thumbnailHash,
    tags:           content.tags,
  };
}

export function fromWire(obj: MediaContentWire): MediaContent {
  return {
    contentHash:   obj.content_hash,
    title:         obj.title,
    durationMs:    obj.duration_ms,
    codec:         obj.codec,
    contentType:   obj.content_type,
    creatorUhid:   obj.creator_uhid,
    sizeBytes:     obj.size_bytes,
    createdAtMs:   obj.created_at_ms,
    thumbnailHash: obj.thumbnail_hash,
    tags:          obj.tags,
  };
}

// ── Helpers ───────────────────────────────────────────────────────────────────

/**
 * Returns a human-readable duration string.
 *
 * - 0  → "Live"
 * - < 3 600 000 ms → "M:SS" or "MM:SS"  (e.g. "4:32")
 * - >= 3 600 000 ms → "H:MM:SS"          (e.g. "1:23:45")
 */
export function formattedDuration(durationMs: number): string {
  if (durationMs <= 0) return "Live";

  const totalSeconds = Math.floor(durationMs / 1000);
  const hours   = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;

  const ss = seconds.toString().padStart(2, "0");

  if (hours > 0) {
    const mm = minutes.toString().padStart(2, "0");
    return `${hours}:${mm}:${ss}`;
  }

  return `${minutes}:${ss}`;
}

export function isVideo(content: MediaContent): boolean {
  return content.contentType.toLowerCase().startsWith("video/");
}

export function isAudio(content: MediaContent): boolean {
  return content.contentType.toLowerCase().startsWith("audio/");
}
