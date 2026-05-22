# Aether Media — Implementação em TypeScript

[English](../../../../typescript/README.md) · [Français](../../fr/typescript/README.md) · [Español](../../es/typescript/README.md) · [العربية](../../ar/typescript/README.md) · [中文简体](../../zh-CN/typescript/README.md) · [日本語](../../ja/typescript/README.md) · [Deutsch](../../de/typescript/README.md) · [Português (BR)](README.md) · [Русский](../../ru/typescript/README.md) · [فارسی](../../fa/typescript/README.md) · [한국어](../../ko/typescript/README.md)

Um player web TypeScript/Node.js para o Aether Media. Integra HLS.js e Shaka Player para streaming adaptativo e se conecta à mesh Aether por meio dos bindings TypeScript do `@bhengubv/aether-protocol`. Adequado para players de mídia baseados em navegador, aplicativos Electron e servidores de mídia Node.js.

---

## Requisitos

- Node.js 20+
- npm 10+

---

## Instalação

```bash
npm install @bhengubv/aether-media
```

Ou compilar a partir do código-fonte:

```bash
cd typescript
npm install
npm run build
```

---

## Executar testes

```bash
npm test
```

---

## Início rápido

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

| Módulo | Exportação | Descrição |
|--------|--------|-------------|
| `player` | `AetherMediaPlayer` | Motor de reprodução adaptativa HLS.js + Shaka Player |
| `social` | `FeedClient`, `ReactionClient` | Navegação no feed e envio de reações |
| `streaming` | `AetherStreamClient` | Assinatura de transmissão ao vivo e bufferização de segmentos |
| `content` | `ContentClient` | Descoberta e download de fragmentos de conteúdo P2P |
| `identity` | `ProfileClient` | Resolução de perfil por AetherTag |
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` | Tipos de domínio compartilhados |

---

## Player

`AetherMediaPlayer` encapsula tanto HLS.js (para fontes HTTP/HLS) quanto Shaka Player (para streams DASH e da mesh Aether), selecionando automaticamente o melhor motor:

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

## Transmissão ao vivo

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

## Feed e reações

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

## Estrutura do projeto

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

## Dependências

| Pacote | Finalidade |
|---------|---------|
| `hls.js` | Streaming HLS adaptativo no navegador |
| `shaka-player` | DASH + HLS com suporte a DRM |
| `@bhengubv/aether-protocol` | Bindings de transporte da mesh |

---

## Licença

MIT
