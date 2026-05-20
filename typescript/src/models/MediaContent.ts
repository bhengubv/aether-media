export interface MediaContent {
  contentHash: string;
  title: string;
  durationMs: number;
  codec: string;
  contentType: string;
  creatorUhid: string;
  sizeBytes: number;
  createdAt: Date;
  thumbnailHash: string | null;
  tags: string[];
}

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
