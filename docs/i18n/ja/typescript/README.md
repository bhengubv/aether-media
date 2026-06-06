# Aether Media — TypeScript 実装

[English](../../../../typescript/README.md) · [Français](../../fr/typescript/README.md) · [Español](../../es/typescript/README.md) · [العربية](../../ar/typescript/README.md) · [中文简体](../../zh-CN/typescript/README.md) · [日本語](README.md) · [Deutsch](../../de/typescript/README.md) · [Português (BR)](../../pt-BR/typescript/README.md) · [Русский](../../ru/typescript/README.md) · [فارسی](../../fa/typescript/README.md) · [한국어](../../ko/typescript/README.md)

Aether Media 向けの TypeScript/Node.js ウェブプレイヤーです。アダプティブストリーミング用に HLS.js と Shaka Player を統合し、`@bhengubv/aether-protocol` TypeScript バインディングを介して Aether メッシュに接続します。ブラウザベースのメディアプレイヤー、Electron アプリ、Node.js メディアサーバーに適しています。

---

## 要件

- Node.js 20+
- npm 10+

---

## インストール

```bash
npm install @bhengubv/aether-media
```

またはソースからビルド:

```bash
cd typescript
npm install
npm run build
```

---

## テストの実行

```bash
npm test
```

---

## クイックスタート

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

## モジュール

| モジュール | エクスポート | 説明 |
|--------|--------|-------------|
| `player` | `AetherMeshMediaPlayer` | HLS.js + Shaka Player アダプティブ再生エンジン |
| `social` | `FeedClient`, `ReactionClient` | フィードの閲覧とリアクションの送信 |
| `streaming` | `AetherMeshStreamClient` | ライブストリーム購読とセグメントバッファリング |
| `content` | `ContentClient` | P2P コンテンツチャンクの探索とダウンロード |
| `identity` | `ProfileClient` | AetherMeshTag プロフィール解決 |
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` | 共有ドメイン型 |

---

## プレイヤー

`AetherMeshMediaPlayer` は HLS.js（HTTP/HLS ソース用）と Shaka Player（DASH および Aether メッシュストリーム用）の両方をラップし、最適なエンジンを自動的に選択します:

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

## ライブストリーミング

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

## フィードとリアクション

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

## モデル

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

## プロジェクト構成

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

## 依存関係

| パッケージ | 用途 |
|---------|---------|
| `hls.js` | ブラウザでの HLS アダプティブストリーミング |
| `shaka-player` | DRM サポート付き DASH + HLS |
| `@bhengubv/aether-protocol` | メッシュトランスポートバインディング |

---

## ライセンス

MIT
