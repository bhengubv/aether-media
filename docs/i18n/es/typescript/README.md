# Aether Media — Implementación en TypeScript

[English](../../../../typescript/README.md) · [Français](../../fr/typescript/README.md) · [Español](README.md) · [العربية](../../ar/typescript/README.md) · [中文简体](../../zh-CN/typescript/README.md) · [日本語](../../ja/typescript/README.md) · [Deutsch](../../de/typescript/README.md) · [Português (BR)](../../pt-BR/typescript/README.md) · [Русский](../../ru/typescript/README.md) · [فارسی](../../fa/typescript/README.md) · [한국어](../../ko/typescript/README.md)

Un reproductor web TypeScript/Node.js para Aether Media. Integra HLS.js y Shaka Player para streaming adaptativo, y se conecta a la malla Aether mediante los bindings TypeScript de `@bhengubv/aether-protocol`. Adecuado para reproductores multimedia en el navegador, aplicaciones Electron y servidores multimedia Node.js.

---

## Requisitos

- Node.js 20+
- npm 10+

---

## Instalación

```bash
npm install @bhengubv/aether-media
```

O compilar desde el código fuente:

```bash
cd typescript
npm install
npm run build
```

---

## Ejecutar pruebas

```bash
npm test
```

---

## Inicio rápido

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

## Módulos

| Módulo | Exportación | Descripción |
|--------|--------|-------------|
| `player` | `AetherMediaPlayer` | Motor de reproducción adaptativa HLS.js + Shaka Player |
| `social` | `FeedClient`, `ReactionClient` | Navegación del feed y envío de reacciones |
| `streaming` | `AetherStreamClient` | Suscripción a stream en directo y almacenamiento en búfer de segmentos |
| `content` | `ContentClient` | Descubrimiento y descarga de fragmentos de contenido P2P |
| `identity` | `ProfileClient` | Resolución de perfiles AetherTag |
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` | Tipos de dominio compartidos |

---

## Reproductor

`AetherMediaPlayer` envuelve tanto HLS.js (para fuentes HTTP/HLS) como Shaka Player (para streams DASH y de malla Aether), seleccionando automáticamente el mejor motor:

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

## Transmisión en directo

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

## Feed y reacciones

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

## Modelos

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

## Estructura del proyecto

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

## Dependencias

| Paquete | Propósito |
|---------|---------|
| `hls.js` | Streaming HLS adaptativo en el navegador |
| `shaka-player` | DASH + HLS con soporte DRM |
| `@bhengubv/aether-protocol` | Bindings de transporte de malla |

---

## Licencia

MIT
