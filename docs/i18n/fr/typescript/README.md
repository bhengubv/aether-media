# Aether Media — Implémentation TypeScript

[English](../../../../typescript/README.md) · [Français](README.md) · [Español](../../es/typescript/README.md) · [العربية](../../ar/typescript/README.md) · [中文简体](../../zh-CN/typescript/README.md) · [日本語](../../ja/typescript/README.md) · [Deutsch](../../de/typescript/README.md) · [Português (BR)](../../pt-BR/typescript/README.md) · [Русский](../../ru/typescript/README.md) · [فارسی](../../fa/typescript/README.md) · [한국어](../../ko/typescript/README.md)

Un lecteur web TypeScript/Node.js pour Aether Media. Intègre HLS.js et Shaka Player pour le streaming adaptatif, et se connecte au maillage Aether via les liaisons TypeScript `@bhengubv/aether-protocol`. Convient aux lecteurs multimédias basés sur navigateur, aux applications Electron et aux serveurs multimédias Node.js.

---

## Prérequis

- Node.js 20+
- npm 10+

---

## Installation

```bash
npm install @bhengubv/aether-media
```

Ou installer depuis les sources :

```bash
cd typescript
npm install
npm run build
```

---

## Exécuter les tests

```bash
npm test
```

---

## Démarrage rapide

```typescript
import { AetherNetMediaPlayer, FeedClient, ProfileClient } from '@bhengubv/aether-media';

// Lire un contenu par son hash
const player = new AetherNetMediaPlayer();
await player.load('sha256abc');
player.play();

// Parcourir le flux
const feed = new FeedClient();
const items = await feed.fetch({ viewerUhid: 'my-uhid' });
for (const item of items) {
    console.log(item.content.title, item.content.formattedDuration);
}

// Résoudre le profil d'un créateur
const profiles = new ProfileClient();
const profile = await profiles.get('creator-uhid');
console.log(profile.displayName, profile.aetherTag);
```

---

## Modules

| Module | Export | Description |
|--------|--------|-------------|
| `player` | `AetherNetMediaPlayer` | Moteur de lecture adaptatif HLS.js + Shaka Player |
| `social` | `FeedClient`, `ReactionClient` | Navigation dans le flux et envoi de réactions |
| `streaming` | `AetherNetStreamClient` | Abonnement aux flux en direct et mise en mémoire tampon des segments |
| `content` | `ContentClient` | Découverte et téléchargement de fragments de contenu P2P |
| `identity` | `ProfileClient` | Résolution de profils AetherNetTag |
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` | Types de domaine partagés |

---

## Lecteur

`AetherNetMediaPlayer` encapsule à la fois HLS.js (pour les sources HTTP/HLS) et Shaka Player (pour les flux DASH et le maillage Aether), en sélectionnant automatiquement le meilleur moteur :

```typescript
import { AetherNetMediaPlayer } from '@bhengubv/aether-media';

const player = new AetherNetMediaPlayer({
    container: document.getElementById('video-container')!,
    autoQuality: true,
});

// Charger par hash de contenu Aether (résolu via le maillage)
await player.load('sha256abc');

// Charger une URL HLS directe (recours HTTP)
await player.loadUrl('https://relay.example.com/stream.m3u8');

player.play();
player.pause();
player.seek(30_000); // ms

player.on('ended', () => console.log('Playback finished'));
player.on('error',  (err) => console.error(err));
```

---

## Streaming en direct

```typescript
import { AetherNetStreamClient } from '@bhengubv/aether-media';

const client = new AetherNetStreamClient();
await client.subscribe('host-uhid-abc123');

client.on('segment', (segment) => {
    player.appendSegment(segment);
});

client.on('ended', () => {
    console.log('Stream ended');
});
```

---

## Flux et réactions

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

## Modèles

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

## Structure du projet

```
typescript/
├── src/
│   ├── content/         # P2P content chunk client
│   ├── identity/        # ProfileClient
│   ├── models/          # Domain types + computed properties
│   ├── player/          # AetherNetMediaPlayer (HLS.js + Shaka)
│   ├── social/          # FeedClient, ReactionClient
│   ├── streaming/       # AetherNetStreamClient
│   └── index.ts         # Public re-exports
├── package.json
└── tsconfig.json
```

---

## Dépendances

| Package | Utilisation |
|---------|---------|
| `hls.js` | Streaming adaptatif HLS dans le navigateur |
| `shaka-player` | DASH + HLS avec prise en charge DRM |
| `@bhengubv/aether-protocol` | Liaisons de transport pour le maillage |

---

## Licence

MIT
