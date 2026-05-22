# Aether Media — TypeScript-Implementierung

[English](../../../../typescript/README.md) · [Français](../../fr/typescript/README.md) · [Español](../../es/typescript/README.md) · [العربية](../../ar/typescript/README.md) · [中文简体](../../zh-CN/typescript/README.md) · [日本語](../../ja/typescript/README.md) · [Deutsch](README.md) · [Português (BR)](../../pt-BR/typescript/README.md) · [Русский](../../ru/typescript/README.md) · [فارسی](../../fa/typescript/README.md) · [한국어](../../ko/typescript/README.md)

Ein TypeScript/Node.js-Web-Player für Aether Media. Integriert HLS.js und Shaka Player für adaptives Streaming und verbindet sich über die TypeScript-Bindungen `@bhengubv/aether-protocol` mit dem Aether-Mesh. Geeignet für browserbasierte Mediaplayer, Electron-Apps und Node.js-Medienserver.

---

## Voraussetzungen

- Node.js 20+
- npm 10+

---

## Installation

```bash
npm install @bhengubv/aether-media
```

Oder aus dem Quellcode installieren:

```bash
cd typescript
npm install
npm run build
```

---

## Tests ausführen

```bash
npm test
```

---

## Schnellstart

```typescript
import { AetherMediaPlayer, FeedClient, ProfileClient } from '@bhengubv/aether-media';

// Einen Inhalt per Hash abspielen
const player = new AetherMediaPlayer();
await player.load('sha256abc');
player.play();

// Den Feed durchsuchen
const feed = new FeedClient();
const items = await feed.fetch({ viewerUhid: 'my-uhid' });
for (const item of items) {
    console.log(item.content.title, item.content.formattedDuration);
}

// Ein Ersteller-Profil auflösen
const profiles = new ProfileClient();
const profile = await profiles.get('creator-uhid');
console.log(profile.displayName, profile.aetherTag);
```

---

## Module

| Modul | Export | Beschreibung |
|--------|--------|-------------|
| `player` | `AetherMediaPlayer` | Adaptive Wiedergabe-Engine mit HLS.js + Shaka Player |
| `social` | `FeedClient`, `ReactionClient` | Feed-Browsing und Reaktionen senden |
| `streaming` | `AetherStreamClient` | Live-Stream-Abonnement und Segment-Pufferung |
| `content` | `ContentClient` | P2P-Inhalts-Chunk-Entdeckung und -Download |
| `identity` | `ProfileClient` | AetherTag-Profilauflösung |
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` | Gemeinsame Domänentypen |

---

## Player

`AetherMediaPlayer` umschließt sowohl HLS.js (für HTTP/HLS-Quellen) als auch Shaka Player (für DASH und Aether-Mesh-Streams) und wählt automatisch die beste Engine aus:

```typescript
import { AetherMediaPlayer } from '@bhengubv/aether-media';

const player = new AetherMediaPlayer({
    container: document.getElementById('video-container')!,
    autoQuality: true,
});

// Per Aether-Inhalts-Hash laden (wird über das Mesh aufgelöst)
await player.load('sha256abc');

// Eine direkte HLS-URL laden (HTTP-Fallback)
await player.loadUrl('https://relay.example.com/stream.m3u8');

player.play();
player.pause();
player.seek(30_000); // ms

player.on('ended', () => console.log('Playback finished'));
player.on('error',  (err) => console.error(err));
```

---

## Live-Streaming

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

## Feed und Reaktionen

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

## Modelle

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

## Projektstruktur

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

## Abhängigkeiten

| Paket | Verwendungszweck |
|---------|---------|
| `hls.js` | Adaptives HLS-Streaming im Browser |
| `shaka-player` | DASH + HLS mit DRM-Unterstützung |
| `@bhengubv/aether-protocol` | Mesh-Transport-Bindungen |

---

## Lizenz

MIT
