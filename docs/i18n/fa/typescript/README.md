<div dir="rtl">

# Aether Media — پیاده‌سازی TypeScript

[English](../../../../typescript/README.md) · [Français](../../fr/typescript/README.md) · [Español](../../es/typescript/README.md) · [العربية](../../ar/typescript/README.md) · [中文简体](../../zh-CN/typescript/README.md) · [日本語](../../ja/typescript/README.md) · [Deutsch](../../de/typescript/README.md) · [Português (BR)](../../pt-BR/typescript/README.md) · [Русский](../../ru/typescript/README.md) · [فارسی](README.md) · [한국어](../../ko/typescript/README.md)

یک پخش‌کننده وب TypeScript/Node.js برای Aether Media. HLS.js و Shaka Player را برای استریم تطبیقی یکپارچه می‌کند، و از طریق اتصالات TypeScript `@bhengubv/aether-protocol` به mesh Aether متصل می‌شود. مناسب برای پخش‌کننده‌های رسانه‌ای مبتنی بر مرورگر، اپلیکیشن‌های Electron، و سرورهای رسانه Node.js.

---

## پیش‌نیازها

- Node.js 20+
- npm 10+

---

## نصب

```bash
npm install @bhengubv/aether-media
```

یا ساخت از سورس:

```bash
cd typescript
npm install
npm run build
```

---

## اجرای تست‌ها

```bash
npm test
```

---

## شروع سریع

```typescript
import { AetherMeshMediaPlayer, FeedClient, ProfileClient } from '@bhengubv/aether-media';

// Play a piece of content by hash
const player = new AetherMeshMediaPlayer();
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

## ماژول‌ها

| ماژول | خروجی | توضیحات |
|--------|--------|-------------|
| `player` | `AetherMeshMediaPlayer` | موتور پخش تطبیقی HLS.js + Shaka Player |
| `social` | `FeedClient`, `ReactionClient` | مرور فید و ارسال واکنش |
| `streaming` | `AetherMeshStreamClient` | اشتراک در پخش زنده و بافرینگ قطعات |
| `content` | `ContentClient` | کشف و دانلود قطعه‌ای محتوا P2P |
| `identity` | `ProfileClient` | تفسیر پروفایل AetherMeshTag |
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` | انواع دامنه مشترک |

---

## پخش‌کننده

`AetherMeshMediaPlayer` هم HLS.js (برای منابع HTTP/HLS) و هم Shaka Player (برای DASH و استریم‌های mesh Aether) را در بر می‌گیرد و بهترین موتور را به صورت خودکار انتخاب می‌کند:

```typescript
import { AetherMeshMediaPlayer } from '@bhengubv/aether-media';

const player = new AetherMeshMediaPlayer({
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

## پخش زنده

```typescript
import { AetherMeshStreamClient } from '@bhengubv/aether-media';

const client = new AetherMeshStreamClient();
await client.subscribe('host-uhid-abc123');

client.on('segment', (segment) => {
    player.appendSegment(segment);
});

client.on('ended', () => {
    console.log('Stream ended');
});
```

---

## فید و واکنش‌ها

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

## مدل‌ها

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

## ساختار پروژه

```
typescript/
├── src/
│   ├── content/         # P2P content chunk client
│   ├── identity/        # ProfileClient
│   ├── models/          # Domain types + computed properties
│   ├── player/          # AetherMeshMediaPlayer (HLS.js + Shaka)
│   ├── social/          # FeedClient, ReactionClient
│   ├── streaming/       # AetherMeshStreamClient
│   └── index.ts         # Public re-exports
├── package.json
└── tsconfig.json
```

---

## وابستگی‌ها

| پکیج | هدف |
|---------|---------|
| `hls.js` | استریم تطبیقی HLS در مرورگر |
| `shaka-player` | DASH + HLS با پشتیبانی DRM |
| `@bhengubv/aether-protocol` | اتصالات انتقال Mesh |

---

## مجوز

MIT

</div>
