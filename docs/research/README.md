# AetherMedia — Research & Learnings

> Studies of existing media systems, done so a newcomer can avoid the mistakes
> that sank far better-resourced products. **We learn from these systems; we do
> not copy their code.** Both are license-incompatible with AetherMedia's MIT
> (Winamp's WCL forbids reuse outright; MediaManager's AGPL would force all of
> AetherMedia to AGPL). Ideas and architecture are not copyrightable — those we
> take freely. Code is — that we never touch.

## Documents

| Doc | System | What it teaches |
|---|---|---|
| [`winamp-postmortem.md`](winamp-postmortem.md) | Winamp (C/C++, WCL) | The **player / playback** end — plugin-ABI architecture, DSP, loudness, JIT visuals — *and* why a product with the best tech and 80M users still died |
| [`mediamanager-learnings.md`](mediamanager-learnings.md) | MediaManager (Python/Svelte, AGPL) | The **library / automation** end — metadata pipeline, auto-acquisition, and the relay-cache pattern |

## The two poles, and where AetherMedia sits

```
   Winamp                      AetherMedia                  MediaManager
   ------                      -----------                  ------------
   PLAY / process              the synthesis                ACQUIRE / organize
   plugin pipeline             player core  +  automation   metadata pipeline
   DSP, loudness, visuals      loop, over the MESH          indexer → download → notify
   broadcast streaming         (P2P replaces both           centralized metadata relay
                                broadcast AND centralized
                                acquisition)
```

Winamp owned playback but never made the jump past desktop + broadcast.
MediaManager owns automation but is centralized and single-host. **AetherMedia's
opening is the synthesis neither attempted:** Winamp's player architecture +
MediaManager's automation loop, with the **mesh** replacing both the broadcast
layer *and* the centralized acquisition.

## The single most adoptable concept from each

- **From Winamp:** the **plugin-ABI spine** — `in_ → dsp_ → out_` behind a
  frozen contract, every capability a swappable stage. 25 years of format
  breadth from one stable core.
- **From MediaManager:** the **`metadata_relay` → mesh metadata cache** — one
  node fetches a title's metadata, every peer gets it over the mesh. The Forge
  pattern applied to enrichment.

## Consolidated "avoid flopping" principles

1. **Guard "fast and light" with your life.** Winamp 3's framework rewrite
   (Wasabi) was bloated and slow, broke compatibility, and nearly killed the
   product. AetherMedia already carries 14 projects + `UI.Shared` + Wasabi-shaped
   layering — this is a *live* risk, not a hypothetical.
2. **Follow users into the next era or die.** Winamp missed mobile and
   on-demand streaming. The era it missed (mesh + offline + mobile) is
   AetherMedia's entire thesis — but only a moat if we actually ship the
   clients. Claiming platforms we haven't built (Swift ≈ 365 lines today) is the
   same failure dressed differently.
3. **Build the depth users feel.** Loudness normalization, EQ / DSP chain,
   gapless / crossfade, scriptable visuals, format breadth — AetherMedia has
   none yet. These are what made Winamp loved beyond its UI.
4. **License is existential.** Winamp's fatal final mistake was a hostile
   license that strangled community revival. MIT is the one thing we got right
   at the start. Protect it — it's what lets the network and contributors form.
5. **A tiny stable core outlives everything bolted to it.** Winamp's core
   survived 25 years of codecs, devices, and OS changes because it orchestrated
   plugins rather than implementing features. Keep AetherMedia's core that thin.

## Method note

Both audits were performed read-only against public GitHub repos under
`bhengubv/` using the GitHub API (no clone, no code extraction). Findings are
architecture- and concept-level. No source from either system has been, or will
be, incorporated into AetherMedia.
