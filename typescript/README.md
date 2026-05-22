# Aether Media — TypeScript Implementation

[English](README.md) · [Français](../docs/i18n/fr/typescript/README.md) · [Español](../docs/i18n/es/typescript/README.md) · [العربية](../docs/i18n/ar/typescript/README.md) · [中文简体](../docs/i18n/zh-CN/typescript/README.md) · [日本語](../docs/i18n/ja/typescript/README.md) · [Deutsch](../docs/i18n/de/typescript/README.md) · [Português (BR)](../docs/i18n/pt-BR/typescript/README.md) · [Русский](../docs/i18n/ru/typescript/README.md) · [فارسی](../docs/i18n/fa/typescript/README.md) · [한국어](../docs/i18n/ko/typescript/README.md)

A TypeScript/Node.js web player for Aether Media. Integrates HLS.js and Shaka Player for adaptive streaming, and connects to the Aether mesh via the `@bhengubv/aether-protocol` TypeScript bindings. Suitable for browser-based media players, Electron apps, and Node.js media servers.

---

## Requirements

- Node.js 20+
- npm 10+

---

## Install

```bash
npm install @bhengubv/aether-media
```

Or build from source:

```bash
cd typescript
npm install
npm run build
```

---

## Run tests

```bash
npm test
```

---

## Quick start

```typescript
import { AetherMediaPlayer, FeedClient, ProfileClient } from '@bhengubv/aether-media';

// Play a piece of content by hash
const player = new AetherMediaPlayer();
await player.load('sha256abc');
player.play();

// Browse the feed
const feed = new FeedClient();
const items = await feed.fetch({ viewerUhid: 'my-uhid' });
for (const item of items) {
    console.log(item.content.title, item.content.formattedDuration);
}

// Resolve a creator profile
const profiles = new ProfileClient();
const profile = await profiles.get('creator-uhid');
console.log(profile.displayName, profile.aetherTag);
```

---

## Modules

| Module | Export | Description |
|--------|--------|-------------|
| `player` | `AetherMediaPlayer` | HLS.js + Shaka Player adaptive playback engine |
| `social` | `FeedClient`, `ReactionClient` | Feed browsing and reaction sending |
| `streaming` | `AetherStreamClient` | Live stream subscription and segment buffering |
| `content` | `ContentClient` | P2P content chunk discovery and download |
| `identity` | `ProfileClient` | AetherTag profile resolution |
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` | Shared domain types |

---

## Player

`AetherMediaPlayer` wraps both HLS.js (for HTTP/HLS sources) and Shaka Player (for DASH and Aether mesh streams), selecting the best engine automatically:

```typescript
import { AetherMediaPlayer } from '@bhengubv/aether-media';

const player = new AetherMediaPlayer({
    container: document.getElementById('video-container')!,
    autoQuality: true,
});

// Load by Aether content hash (resolved via mesh)
await player.load('sha256abc');

// Load a direct HLS URL (HTTP fallback)
await player.loadUrl('https://relay.example.com/stream.m3u8');

player.play();
player.pause();
player.seek(30_000); // ms

player.on('ended', () => console.log('Playback finished'));
player.on('error',  (err) => console.error(err));
```

---

## Live streaming

```typescript
import { AetherStreamClient } from '@bhengubv/aether-media';

const client = new AetherStreamClient();
await client.subscribe('host-uhid-abc123');

client.on('segment', (segment) => {
    player.appendSegment(segment);
});

client.on('ended', () => {
    console.log('Stream ended');
});
```

---

## Feed and reactions

```typescript
import { FeedClient, ReactionClient } from '@bhengubv/aether-media';

const feed = new FeedClient();
const items = await feed.fetch({ viewerUhid: 'my-uhid', limit: 20 });

const reactions = new ReactionClient();
await reactions.send({
    contentHash: 'sha256abc',
    type: 'Like',
    positionMs: 12_500,
});
```

---

## Models

```typescript
import type { MediaContent } from '@bhengubv/aether-media';

const content: MediaContent = {
    contentHash: 'sha256abc',
    title: 'My Video',
    durationMs: 180_000,
    codec: 'h264',
    contentType: 'video/mp4',
    creatorUhid: 'uhid-xyz',
    sizeBytes: 52_428_800,
};

console.log(content.formattedDuration); // "3:00"
console.log(content.isVideo);            // true
```

---

## Project layout

```
typescript/
├── src/
│   ├── content/         # P2P content chunk client
│   ├── identity/        # ProfileClient
│   ├── models/          # Domain types + computed properties
│   ├── player/          # AetherMediaPlayer (HLS.js + Shaka)
│   ├── social/          # FeedClient, ReactionClient
│   ├── streaming/       # AetherStreamClient
│   └── index.ts         # Public re-exports
├── package.json
└── tsconfig.json
```

---

## Dependencies

| Package | Purpose |
|---------|---------|
| `hls.js` | HLS adaptive streaming in the browser |
| `shaka-player` | DASH + HLS with DRM support |
| `@bhengubv/aether-protocol` | Mesh transport bindings |

---

## License

MIT
