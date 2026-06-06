# Aether Media — TypeScript 实现

[English](../../../../typescript/README.md) · [Français](../../fr/typescript/README.md) · [Español](../../es/typescript/README.md) · [العربية](../../ar/typescript/README.md) · [中文简体](README.md) · [日本語](../../ja/typescript/README.md) · [Deutsch](../../de/typescript/README.md) · [Português (BR)](../../pt-BR/typescript/README.md) · [Русский](../../ru/typescript/README.md) · [فارسی](../../fa/typescript/README.md) · [한국어](../../ko/typescript/README.md)

Aether Media 的 TypeScript/Node.js 网页播放器。集成 HLS.js 和 Shaka Player 用于自适应流媒体，并通过 `@bhengubv/aether-protocol` TypeScript 绑定连接 Aether 网状网络。适用于基于浏览器的媒体播放器、Electron 应用和 Node.js 媒体服务器。

---

## 环境要求

- Node.js 20+
- npm 10+

---

## 安装

```bash
npm install @bhengubv/aether-media
```

或从源码构建：

```bash
cd typescript
npm install
npm run build
```

---

## 运行测试

```bash
npm test
```

---

## 快速入门

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

## 模块

| 模块 | 导出 | 说明 |
|--------|--------|-------------|
| `player` | `AetherMeshMediaPlayer` | HLS.js + Shaka Player 自适应播放引擎 |
| `social` | `FeedClient`、`ReactionClient` | 信息流浏览与互动发送 |
| `streaming` | `AetherMeshStreamClient` | 直播订阅与分片缓冲 |
| `content` | `ContentClient` | P2P 内容块发现与下载 |
| `identity` | `ProfileClient` | AetherMeshTag 个人资料解析 |
| `models` | `MediaContent`、`MediaProfile`、`MediaFeedItem`、`MediaReaction` | 共享领域类型 |

---

## 播放器

`AetherMeshMediaPlayer` 同时封装了 HLS.js（用于 HTTP/HLS 源）和 Shaka Player（用于 DASH 和 Aether 网状流），并自动选择最佳引擎：

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

## 直播

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

## 信息流与互动

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

## 模型

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

## 项目结构

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

## 依赖项

| 包 | 用途 |
|---------|---------|
| `hls.js` | 浏览器中的 HLS 自适应流 |
| `shaka-player` | 支持 DRM 的 DASH + HLS |
| `@bhengubv/aether-protocol` | 网状传输绑定 |

---

## 许可证

MIT
