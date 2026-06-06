# MediaManager — Engineering Audit & Learnings

> **Why this document exists.** Winamp teaches the *player / playback* end of
> the media stack. MediaManager teaches the **opposite end** — *acquire,
> organize, enrich, serve*. AetherMedia needs both, and MediaManager shows the
> library-automation half done in a modern, self-hosted shape.
>
> **Source audited:** `github.com/bhengubv/MediaManager` (fork of
> `maxdorninger/MediaManager`). ~26 MB. Python backend + Svelte frontend,
> Docker-first. **License: AGPL-3.0.** Read-only audit; **no code was taken** —
> see the License section.

---

## 1. What it actually is

An **\*arr-family tool** (in the lineage of Sonarr / Radarr): it doesn't *play*
media, it *manages* it. It auto-fetches metadata, watches for new episodes,
acquires them, organizes files, and notifies you.

- **Backend:** Python — FastAPI + SQLAlchemy + Alembic migrations, packaged
  with `uv`.
- **Frontend:** Svelte + TypeScript.
- **Deployment:** Docker-first (`docker-compose.yaml`, multi-service).

### Backend structure (`media_manager/`)

| Module | Responsibility |
|---|---|
| `metadataProvider` | TVDB / TMDB metadata, behind one abstraction, auto-refreshing |
| `indexer` | Prowlarr integration — search release indexers |
| `torrent` | Download-client integration (Transmission) + usenet |
| `movies`, `tv` | Domain logic for the two media types |
| `notification` | Notify on events (new episode acquired, etc.) |
| `auth` | OIDC + OAuth 2.0 |
| `database` | SQLAlchemy models + Alembic migrations |

A **separate** `metadata_relay/` service ships alongside (own Dockerfile,
`pyproject.toml`).

---

## 2. The gem: `metadata_relay`

This is the most directly valuable idea in the whole repo.

`metadata_relay` is a standalone service that sits **between the app and the
metadata providers** (TVDB / TMDB) so that individual self-hosters don't each
need their own API key — one relay fetches and serves many instances.

**Map that onto the mesh and it becomes obvious:**

> One node fetches a title's metadata from TMDB once; **every other node gets
> it over the mesh** — no per-node API key, no per-node internet round-trip.

That is the **Forge pattern** (`cache-once, serve-to-peers`) applied to media
metadata. The centralized relay becomes a **mesh metadata cache** — metadata
enrichment gossiped and cached P2P. This is the single most adoptable *concept*
from MediaManager, and it slots straight into AetherMesh's existing
`IContentService` / Forge model.

---

## 3. The automation loop

MediaManager's core loop:

```
indexer search  →  download (torrent/usenet)  →  organize files  →  notify
```

AetherMedia doesn't need torrents or usenet — **the mesh is the source.** But
the *shape* is identical, and worth adopting:

```
watch followed creators  →  auto-pull new content over the mesh  →  organize  →  notify
```

The mesh replaces the `indexer` + `torrent`/`usenet` layers entirely; the
"fully automatic, hands-off acquisition" behaviour is the part to keep.

---

## 4. The provider-abstraction pattern

`metadataProvider` swaps TVDB / TMDB behind a single interface with automatic
metadata refresh. AetherMedia's `IMetadataResolver` and `IMovieMetadataService`
are thinner versions of the same idea; MediaManager shows the mature shape —
pluggable providers, scheduled refresh, fallback ordering.

---

## 5. License reality — learn, do not copy

**AGPL-3.0.** Unlike Winamp's WCL, this is *legitimate* open source — but it is
strong **network-copyleft**: any network service built on AGPL code must
release its entire source under AGPL.

**Consequence:** we cannot pull AGPL code into AetherMedia (MIT) without forcing
all of AetherMedia to become AGPL. So the rule is the same as for Winamp:
**learn the architecture and concepts, write our own code.** (Softer flag than
the WCL — AGPL is real free software; it simply cannot flow one-way into an MIT
codebase.)

---

## 6. What AetherMedia takes (concept only)

1. **Turn the centralized `metadata_relay` into a mesh metadata cache** — the
   highest-value steal. One fetch, P2P distribution, zero per-node keys. Reuse
   the Forge / `IContentService` machinery.
2. **Adopt the automation loop** — hands-off "watch → acquire → organize →
   notify," with the mesh as the acquisition layer instead of torrent/usenet.
3. **Mature the provider abstraction** — pluggable metadata providers with
   scheduled refresh and fallback ordering, evolving `IMetadataResolver`.
4. **Note the self-hosted / Docker-first shape** — a deployment model for a
   headless AetherMedia node (relevant for a Go daemon or a server surface).
