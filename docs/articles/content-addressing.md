# Aether Media — Content Addressing and Distribution

Aether Media identifies every piece of media by its SHA-256 content hash, not by a URL or a server path. This means any device that holds the bytes can serve them — there is no origin server to go down, no CDN bill to pay, and no authority to censor a file. This document explains how content is hashed, chunked, distributed across the mesh, cached locally, and verified on receipt.

---

## SHA-256 as Primary Key

The primary key for all media in Aether Media is the SHA-256 hex digest of the raw encoded bytes. This is the `content_hash` field in both the C# `MediaContent` record and the JSON wire format:

```json
{
  "content_hash": "a3f9ee2c84b1d6f4230c5d987654e32a1b0c9d8e7f6a5b4c3d2e1f0a9b8c7d6",
  "title":        "Aether Launch Keynote",
  "size_bytes":   150000000
}
```

Because the hash is derived from the content itself, two independently computed copies of the same file always produce the same key. A device can verify that the bytes it received are correct simply by hashing them and comparing the result to the advertised key — no trusted third party is needed.

---

## Chunking via IContentService

Full media files are too large to transmit as a single unit over a mesh radio. `IContentService` (from `aether-protocol`) handles the split-and-reassemble process.

**Publishing.** When `MediaLibraryScanner` scans a local file it computes the SHA-256 hash of the entire file, then calls `IContentService.AnnounceAsync` with a `ContentDescriptor` that records:

- `root_hash` — SHA-256 of the full file
- `total_bytes` — encoded size
- `chunk_count` — number of fixed-size chunks
- `chunk_hashes` — SHA-256 hash of each individual chunk
- `content_type` — MIME type (e.g. `"video/mp4"`)

The descriptor is broadcast over the mesh. Any peer that receives it can verify which chunks it already holds and request only the missing ones.

**Requesting.** A receiving peer calls `IContentService.RequestChunksAsync` with the root hash and a list of chunk indices it needs. Chunks can be requested from multiple peers simultaneously (BitTorrent-style). Each received chunk is verified against its entry in `chunk_hashes` before being committed to local storage.

**Assembly.** Once all chunks are present, `IContentService` reassembles them in order, verifies the assembled file against `root_hash`, and delivers the complete file.

---

## Thumbnail as a Separately Addressed Chunk

Album art and video thumbnails are handled as independent content items. `MediaLibraryScanner` extracts embedded artwork (via `IThumbnailService`), hashes the raw image bytes, and stores the resulting SHA-256 as `thumbnail_hash` on the `MediaContent` record:

```json
{
  "content_hash":   "a3f9...",
  "thumbnail_hash": "b2e7c1d4a9f30e5812bc34da6701e98f2c5b4a3d1e2f0c9b8a7d6e5f4c3b2a1"
}
```

A client that needs only the thumbnail requests that single hash from the mesh without downloading the full media file. This is the standard pattern for feed thumbnails where bandwidth is limited.

---

## Large File Distribution via Watch Together

For large files (feature films, multi-hour recordings), `IWatchTogetherService.BroadcastTorrentAsync` distributes a BitTorrent-compatible metadata file to all participants in a watch party. The torrent metadata contains the same piece hashes that `IContentService` uses internally, so chunks fetched by the torrent engine are immediately reusable by the content layer and vice versa.

A watch party node that holds 60% of a film can seed those pieces to late-joining peers while simultaneously receiving the remaining 40% from other peers — without any central tracker. Piece availability is gossiped over the mesh so the swarm self-organises.

---

## DTN Delivery to Offline Peers

Not every content announcement reaches every interested peer immediately. The DTN (Delay-Tolerant Networking) layer handles the gap.

`IDtnService.CreateBundleAsync` stores a bundle locally when the destination UHID is currently unreachable. The bundle includes the content descriptor payload (or a follow intent, reaction, or profile update) and a 72-hour TTL. When a route to the destination opens — even via multiple relay hops — the DTN layer delivers the bundle automatically.

This means a creator can publish content while offline on a remote location. The moment any of their followers comes within radio range of any node that received the descriptor (directly or via relay), the follow relationship and the content announcement are both delivered.

---

## LRU Content Cache

`LruContentCache` in `AetherNet.Media.Content` provides an in-memory content cache with O(1) get, set, and eviction, backed by a doubly-linked list and a hash index.

**Default capacity.** 500 MiB. Configurable at construction time.

**Eviction policy.** When total stored bytes exceed the capacity limit, the least-recently-used entries are evicted from the tail of the linked list until the cache fits within the limit.

**Thread safety.** A `SemaphoreSlim(1,1)` guards all operations. The cache is safe to use from multiple threads concurrently.

**Keying.** Cache keys are content hashes (SHA-256 hex strings), compared case-insensitively. This means `"A3F9..."` and `"a3f9..."` refer to the same cache entry.

```csharp
// Store a chunk
cache.Set("a3f9ee2c...", chunkBytes);

// Retrieve it (promotes to MRU)
if (cache.TryGet("a3f9ee2c...", out var data))
    Process(data);

// Explicitly evict (e.g. after the user deletes the file)
cache.Evict("a3f9ee2c...");
```

Items larger than the total capacity are not cached at all (storing them would immediately evict everything else).

---

## Content Verification on Receipt

Every chunk is verified against its declared hash before being accepted. The root file is verified against `root_hash` after assembly. Because the hash is derived from the content itself, a tampered or corrupted chunk is detected immediately and re-requested from an alternative peer.

For profile data and social packets, the sender's Ed25519 identity key (available from `PeerInfo.PublicKey` after handshake) is used to verify the payload signature. A profile update that does not validate against the sending peer's known public key is silently dropped — it cannot be injected by a third party, even one that is relay-adjacent on the mesh.

This chain of verification — chunk hash, root hash, Ed25519 payload signature — means that content integrity is fully verifiable without trusting any server, relay, or certificate authority.

---

## Content Lifecycle Summary

```
Creator device
  └─ MediaLibraryScanner computes SHA-256 root hash
  └─ IThumbnailService extracts and hashes artwork
  └─ IContentService.AnnounceAsync broadcasts ContentDescriptor

Mesh relay nodes
  └─ Cache and re-broadcast ContentDescriptor (flood with dedup)

Viewer device
  └─ FeedAggregator receives ContentAnnounced event → adds to feed
  └─ User taps play → IContentService.RequestChunksAsync
  └─ Chunks received, verified per-chunk, assembled
  └─ LruContentCache stores assembled bytes for future requests
  └─ Root hash verified → playback begins
```

See `docs/articles/architecture.md` for how the content layer fits into the full system, and `docs/articles/wire-format.md` for the `MediaContent` JSON schema shared by all 8 language SDKs.
