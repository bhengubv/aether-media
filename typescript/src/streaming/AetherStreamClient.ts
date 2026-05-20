import type { AetherMediaPlayer } from "../player/AetherMediaPlayer.js";

type SegmentCallback = (bytes: Uint8Array) => void;

/**
 * Subscribes to an Aether mesh stream endpoint and feeds MPEG-TS or fMP4
 * segments to an AetherMediaPlayer via its MediaSource API.
 *
 * The stream is fetched with a ReadableStream over HTTP/2 (or long-poll
 * fallback).  Each \0-delimited chunk is treated as one media segment.
 *
 * Usage:
 *   const client = new AetherStreamClient(player);
 *   await client.subscribe("stream://aether/abc123");
 *   // later:
 *   client.unsubscribe();
 */
export class AetherStreamClient {
  private readonly player: AetherMediaPlayer;
  private _abortController: AbortController | null = null;
  private _onSegment: SegmentCallback | null = null;
  private _activeStreamId: string | null = null;
  // MIME type for the MediaSource SourceBuffer
  private static readonly MIME_CODEC = 'video/mp4; codecs="avc1.42E01E, mp4a.40.2"';

  constructor(player: AetherMediaPlayer) {
    this.player = player;
  }

  get activeStreamId(): string | null { return this._activeStreamId; }

  /**
   * Register a callback that fires for each raw segment received.
   */
  set onSegment(cb: SegmentCallback) {
    this._onSegment = cb;
  }

  /**
   * Subscribe to a stream.  The streamId may be an Aether mesh stream URI
   * (aether://…) or an HTTP URL pointing to an Aether relay endpoint.
   *
   * Segments are pushed to the player's MediaSource automatically and the
   * onSegment callback is also invoked for each one.
   */
  async subscribe(streamId: string): Promise<void> {
    if (this._abortController) {
      this.unsubscribe();
    }

    this._activeStreamId = streamId;
    this._abortController = new AbortController();
    const signal = this._abortController.signal;

    // Resolve aether:// URIs to HTTP relay
    const url = streamId.startsWith("aether://")
      ? `https://relay.aether.network/stream/${streamId.slice("aether://".length)}`
      : streamId;

    try {
      const response = await fetch(url, { signal, headers: { Accept: "application/octet-stream" } });

      if (!response.ok) {
        throw new Error(`Stream fetch failed: ${response.status} ${response.statusText}`);
      }

      if (!response.body) {
        throw new Error("Response has no body — streaming not supported by this endpoint");
      }

      const reader = response.body.getReader();
      let buffer = new Uint8Array(0);

      while (true) {
        const { done, value } = await reader.read();
        if (done || signal.aborted) break;

        // Append chunk to accumulation buffer
        const merged = new Uint8Array(buffer.length + value.length);
        merged.set(buffer, 0);
        merged.set(value, buffer.length);
        buffer = merged;

        // Segments are separated by a 4-byte big-endian length prefix
        while (buffer.length >= 4) {
          const segLen = (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
          if (buffer.length < 4 + segLen) break;

          const segment = buffer.slice(4, 4 + segLen);
          buffer = buffer.slice(4 + segLen);

          this._onSegment?.(segment);
          this.player.feedSegment(segment, AetherStreamClient.MIME_CODEC);
        }
      }
    } catch (err) {
      if (signal.aborted) return; // normal cancellation
      console.error("[AetherStreamClient] stream error:", err);
    } finally {
      this._activeStreamId = null;
    }
  }

  /**
   * Cancel the active stream subscription.
   */
  unsubscribe(): void {
    this._abortController?.abort();
    this._abortController = null;
    this._activeStreamId = null;
  }
}
