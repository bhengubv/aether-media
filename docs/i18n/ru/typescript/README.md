# Aether Media — Реализация на TypeScript

[English](../../../../typescript/README.md) · [Français](../../fr/typescript/README.md) · [Español](../../es/typescript/README.md) · [العربية](../../ar/typescript/README.md) · [中文简体](../../zh-CN/typescript/README.md) · [日本語](../../ja/typescript/README.md) · [Deutsch](../../de/typescript/README.md) · [Português (BR)](../../pt-BR/typescript/README.md) · [Русский](README.md) · [فارسی](../../fa/typescript/README.md) · [한국어](../../ko/typescript/README.md)

Веб-проигрыватель на TypeScript/Node.js для Aether Media. Интегрирует HLS.js и Shaka Player для адаптивного стриминга и подключается к mesh-сети Aether через привязки TypeScript `@bhengubv/aether-protocol`. Подходит для браузерных медиапроигрывателей, приложений Electron и медиасерверов на Node.js.

---

## Требования

- Node.js 20+
- npm 10+

---

## Установка

```bash
npm install @bhengubv/aether-media
```

Или сборка из исходников:

```bash
cd typescript
npm install
npm run build
```

---

## Запуск тестов

```bash
npm test
```

---

## Быстрый старт

```typescript
import { AetherNetMediaPlayer, FeedClient, ProfileClient } from '@bhengubv/aether-media';

// Play a piece of content by hash
const player = new AetherNetMediaPlayer();
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

## Модули

| Модуль | Экспорт | Описание |
|--------|--------|-------------|
| `player` | `AetherNetMediaPlayer` | Адаптивный движок воспроизведения HLS.js + Shaka Player |
| `social` | `FeedClient`, `ReactionClient` | Просмотр ленты и отправка реакций |
| `streaming` | `AetherNetStreamClient` | Подписка на трансляцию в реальном времени и буферизация сегментов |
| `content` | `ContentClient` | Одноранговые обнаружение и загрузка фрагментов контента |
| `identity` | `ProfileClient` | Разрешение профилей AetherNetTag |
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` | Общие доменные типы |

---

## Проигрыватель

`AetherNetMediaPlayer` оборачивает как HLS.js (для источников HTTP/HLS), так и Shaka Player (для потоков DASH и Aether mesh), автоматически выбирая лучший движок:

```typescript
import { AetherNetMediaPlayer } from '@bhengubv/aether-media';

const player = new AetherNetMediaPlayer({
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

## Трансляция в реальном времени

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

## Лента и реакции

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

## Модели

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

## Структура проекта

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

## Зависимости

| Пакет | Назначение |
|---------|---------|
| `hls.js` | Адаптивный HLS-стриминг в браузере |
| `shaka-player` | DASH + HLS с поддержкой DRM |
| `@bhengubv/aether-protocol` | Привязки транспортного слоя mesh-сети |

---

## Лицензия

MIT
