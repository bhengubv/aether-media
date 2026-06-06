/**
 * AetherMeshMediaPlayer — wraps HLS.js for HLS/M3U8 streams and Shaka Player
 * for DASH streams, unified behind a single API.
 *
 * The constructor receives an HTMLVideoElement.  Call load(url) to set the
 * source, then play()/pause()/stop() to control playback.
 *
 * HLS detection: URL ends with .m3u8 or contains /hls/.
 * DASH detection: URL ends with .mpd or contains /dash/.
 * Anything else falls back to native <video> src assignment.
 */

// Dynamic imports keep the bundle split friendly.
// In environments where these libraries are not available the player
// degrades gracefully to native src.

export type PlayerState = "idle" | "loading" | "playing" | "paused" | "stopped" | "error";

export class AetherMeshMediaPlayer {
  private readonly video: HTMLVideoElement;
  private _state: PlayerState = "idle";
  private _hls: unknown = null;    // HLS.js instance when active
  private _shaka: unknown = null;  // Shaka Player instance when active
  private _mediaSource: MediaSource | null = null;

  constructor(videoElement: HTMLVideoElement) {
    if (!videoElement) throw new Error("videoElement is required");
    this.video = videoElement;

    this.video.addEventListener("playing", () => { this._state = "playing"; });
    this.video.addEventListener("pause",   () => { this._state = "paused"; });
    this.video.addEventListener("ended",   () => { this._state = "stopped"; });
    this.video.addEventListener("error",   () => { this._state = "error"; });
  }

  // ── Public properties ──────────────────────────────────────────────────────

  get state(): PlayerState { return this._state; }

  get positionMs(): number {
    return Math.floor(this.video.currentTime * 1000);
  }

  get durationMs(): number {
    const d = this.video.duration;
    return isNaN(d) || !isFinite(d) ? 0 : Math.floor(d * 1000);
  }

  // ── Control methods ────────────────────────────────────────────────────────

  async load(url: string): Promise<void> {
    this._state = "loading";
    await this._teardown();

    if (this._isDash(url)) {
      await this._loadWithShaka(url);
    } else if (this._isHls(url)) {
      await this._loadWithHls(url);
    } else {
      // Native src — browser handles it directly
      this.video.src = url;
    }
  }

  play(): void {
    this.video.play().catch((err) => {
      console.warn("[AetherMeshMediaPlayer] play() rejected:", err);
      this._state = "error";
    });
  }

  pause(): void {
    this.video.pause();
  }

  stop(): void {
    this.video.pause();
    this.video.currentTime = 0;
    this._state = "stopped";
  }

  seek(positionMs: number): void {
    if (positionMs < 0) throw new RangeError("positionMs must be >= 0");
    this.video.currentTime = positionMs / 1000;
  }

  setVolume(v: number): void {
    if (v < 0 || v > 1) throw new RangeError("volume must be 0–1");
    this.video.volume = v;
  }

  setSpeed(s: number): void {
    if (s <= 0) throw new RangeError("speed must be > 0");
    this.video.playbackRate = s;
  }

  // ── Feed a raw segment into the MediaSource ────────────────────────────────

  feedSegment(bytes: Uint8Array, mimeCodec: string): void {
    if (!this._mediaSource) {
      this._mediaSource = new MediaSource();
      this.video.src = URL.createObjectURL(this._mediaSource);
    }

    if (this._mediaSource.readyState !== "open") {
      // Buffer the segment until source opens
      this._mediaSource.addEventListener("sourceopen", () => {
        this._appendSegment(bytes, mimeCodec);
      }, { once: true });
    } else {
      this._appendSegment(bytes, mimeCodec);
    }
  }

  destroy(): void {
    this._teardown();
    this.video.src = "";
    this._state = "idle";
  }

  // ── Private helpers ────────────────────────────────────────────────────────

  private _isHls(url: string): boolean {
    return url.endsWith(".m3u8") || url.includes("/hls/");
  }

  private _isDash(url: string): boolean {
    return url.endsWith(".mpd") || url.includes("/dash/");
  }

  private async _loadWithHls(url: string): Promise<void> {
    const Hls = await import("hls.js").then((m) => m.default);
    if (!Hls.isSupported()) {
      // Fall back to native (Safari has built-in HLS support)
      this.video.src = url;
      return;
    }
    const hls = new Hls();
    this._hls = hls;
    hls.loadSource(url);
    hls.attachMedia(this.video);
  }

  private async _loadWithShaka(url: string): Promise<void> {
    const shaka = await import("shaka-player");
    shaka.polyfill.installAll();
    if (!shaka.Player.isBrowserSupported()) {
      this.video.src = url;
      return;
    }
    const player = new shaka.Player(this.video);
    this._shaka = player;
    await player.load(url);
  }

  private _appendSegment(bytes: Uint8Array, mimeCodec: string): void {
    if (!this._mediaSource) return;
    let sb: SourceBuffer;
    if (this._mediaSource.sourceBuffers.length === 0) {
      sb = this._mediaSource.addSourceBuffer(mimeCodec);
    } else {
      sb = this._mediaSource.sourceBuffers[0];
    }
    if (!sb.updating) {
      sb.appendBuffer(bytes);
    }
  }

  private async _teardown(): Promise<void> {
    if (this._hls) {
      (this._hls as { destroy(): void }).destroy();
      this._hls = null;
    }
    if (this._shaka) {
      await (this._shaka as { destroy(): Promise<void> }).destroy();
      this._shaka = null;
    }
    if (this._mediaSource) {
      if (this._mediaSource.readyState === "open") {
        this._mediaSource.endOfStream();
      }
      this._mediaSource = null;
    }
  }
}
