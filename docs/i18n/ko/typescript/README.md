# Aether Media — TypeScript 구현

[English](../../../../typescript/README.md) · [Français](../../fr/typescript/README.md) · [Español](../../es/typescript/README.md) · [العربية](../../ar/typescript/README.md) · [中文简体](../../zh-CN/typescript/README.md) · [日本語](../../ja/typescript/README.md) · [Deutsch](../../de/typescript/README.md) · [Português (BR)](../../pt-BR/typescript/README.md) · [Русский](../../ru/typescript/README.md) · [فارسی](../../fa/typescript/README.md) · [한국어](README.md)

Aether Media를 위한 TypeScript/Node.js 웹 플레이어입니다. 적응형 스트리밍을 위해 HLS.js와 Shaka Player를 통합하고, `@bhengubv/aether-protocol` TypeScript 바인딩을 통해 Aether 메시에 연결합니다. 브라우저 기반 미디어 플레이어, Electron 앱, Node.js 미디어 서버에 적합합니다.

---

## 요구 사항

- Node.js 20+
- npm 10+

---

## 설치

```bash
npm install @bhengubv/aether-media
```

또는 소스에서 빌드:

```bash
cd typescript
npm install
npm run build
```

---

## 테스트 실행

```bash
npm test
```

---

## 빠른 시작

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

## 모듈

| 모듈 | 내보내기 | 설명 |
|--------|--------|-------------|
| `player` | `AetherMeshMediaPlayer` | HLS.js + Shaka Player 적응형 재생 엔진 |
| `social` | `FeedClient`, `ReactionClient` | 피드 탐색 및 반응 전송 |
| `streaming` | `AetherMeshStreamClient` | 라이브 스트림 구독 및 세그먼트 버퍼링 |
| `content` | `ContentClient` | P2P 콘텐츠 청크 발견 및 다운로드 |
| `identity` | `ProfileClient` | AetherMeshTag 프로필 조회 |
| `models` | `MediaContent`, `MediaProfile`, `MediaFeedItem`, `MediaReaction` | 공유 도메인 타입 |

---

## 플레이어

`AetherMeshMediaPlayer`는 HLS.js(HTTP/HLS 소스용)와 Shaka Player(DASH 및 Aether 메시 스트림용)를 모두 래핑하여 최적의 엔진을 자동으로 선택합니다:

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

## 라이브 스트리밍

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

## 피드 및 반응

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

## 모델

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

## 프로젝트 구조

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

## 의존성

| 패키지 | 용도 |
|---------|---------|
| `hls.js` | 브라우저에서의 HLS 적응형 스트리밍 |
| `shaka-player` | DRM 지원이 포함된 DASH + HLS |
| `@bhengubv/aether-protocol` | 메시 전송 바인딩 |

---

## 라이선스

MIT
