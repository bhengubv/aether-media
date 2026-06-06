<div dir="rtl">

# Aether Media — تنفيذ TypeScript

[English](../../../../typescript/README.md) · [Français](../../fr/typescript/README.md) · [Español](../../es/typescript/README.md) · [العربية](README.md) · [中文简体](../../zh-CN/typescript/README.md) · [日本語](../../ja/typescript/README.md) · [Deutsch](../../de/typescript/README.md) · [Português (BR)](../../pt-BR/typescript/README.md) · [Русский](../../ru/typescript/README.md) · [فارسی](../../fa/typescript/README.md) · [한국어](../../ko/typescript/README.md)

مشغّل ويب بلغة TypeScript/Node.js لـ Aether Media. يدمج HLS.js وShaka Player للبث التكيّفي، ويتصل بشبكة Aether عبر ارتباطات TypeScript الخاصة بـ `@bhengubv/aether-protocol`. مناسب لمشغّلات الوسائط في المتصفح، وتطبيقات Electron، وخوادم وسائط Node.js.

---

## المتطلبات

- Node.js 20+
- npm 10+

---

## التثبيت

```bash
npm install @bhengubv/aether-media
```

أو قم بالبناء من المصدر:

```bash
cd typescript
npm install
npm run build
```

---

## تشغيل الاختبارات

```bash
npm test
```

---

## البدء السريع

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

## الوحدات

| الوحدة | التصدير | الوصف |
|--------|--------|-------------|
| `player` | `AetherNetMediaPlayer` | محرك التشغيل التكيّفي HLS.js + Shaka Player |
| `social` | `FeedClient`, `ReactionClient` | تصفح الخلاصة وإرسال التفاعلات |
| `streaming` | `AetherNetStreamClient` | اشتراك البث المباشر وتخزين الشرائح مؤقتاً |
| `content` | `ContentClient` | اكتشاف وتنزيل قطع المحتوى P2P |
| `identity` | `ProfileClient` | تحليل ملفات الشخصية بـ AetherNetTag |
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` | أنواع المجال المشتركة |

---

## المشغّل

يلفّ `AetherNetMediaPlayer` كلاً من HLS.js (لمصادر HTTP/HLS) وShaka Player (لتدفقات DASH وشبكة Aether)، ويختار المحرك الأفضل تلقائياً:

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

## البث المباشر

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

## الخلاصة والتفاعلات

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

## النماذج

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

## تخطيط المشروع

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

## التبعيات

| الحزمة | الغرض |
|---------|---------|
| `hls.js` | البث التكيّفي HLS في المتصفح |
| `shaka-player` | DASH + HLS مع دعم DRM |
| `@bhengubv/aether-protocol` | ارتباطات نقل الشبكة |

---

## الرخصة

MIT

</div>
