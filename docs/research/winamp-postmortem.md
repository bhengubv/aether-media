# Winamp — Engineering Audit & Post-Mortem

> **Why this document exists.** AetherMedia is a newcomer taking on entrenched
> media players. The cheapest way to avoid flopping is to dissect the one that
> had everything — world-class tech, ~80 million users — and *still died*.
> Winamp teaches both halves at once: the engineering blueprint to copy, and
> the failure modes that kill you even when the tech is great.
>
> **Source audited:** `github.com/bhengubv/winamp` (fork chain →
> `alexfreud/winamp` → the official 2024 Winamp source release). ~275 MB,
> C/C++, branch `community`. Read-only audit; **no code was taken** — see the
> License section for why that line is firm.

---

## 1. What it actually is

Not a player — a **platform**. 74 subsystems under `Src/`. The breadth itself
is the first lesson: Winamp was effectively an operating system for media.

### Subsystem inventory (grouped)

| Group | Subsystems |
|---|---|
| **Audio decoders** | `aacdec`, `aacdec-mft`, `aacPlus`, `alac`, `mp3-mpg123`, `adpcm`, `pcm`, `vlb` |
| **Video decoders** | `h264`, `h264dec`, `f263` (H.263), `mpeg4dec`, `mp4v`, `theora`, `nsvdec_vp3/5/6`, `vp32`, `vp6`, `vp8x`, `libvp6`, `libvpShared` |
| **Containers / demux** | `nsv` (Nullsoft Streaming Video), `nsavi` (AVI), `nsmkv` (Matroska), `mp4v`, `playlist`, `plist`, `xspf` |
| **Image codecs** | `bmp`, `gif`, `jpeg`, `png` (album art + skins) |
| **Tagging / metadata** | `id3v2`, `apev2`, `tagz`, `tataki`, `albumart`, `gracenote` (CDDB), `xml` |
| **Audio DSP / analysis** | `Mastering`, `Elevator`, `ReplayGainAnalysis`, `ns-eel`, `ns-eel2` |
| **Database** | `nde` (Nullsoft Database Engine) |
| **Framework / runtime** | `Wasabi`, `Wasabi2`, `nswasabi`, `nu`, `pfc`, `nsutil`, `Agave`, `Components`, `WAT` |
| **Streaming / online** | `nsv`, `auth`, `omBrowser`, `replicant`, `nprt_plugin`, `ie_plugin` |
| **Devices / burning** | `burnlib` (CD), `devices`, `Portable` (in `Plugins`) |
| **Render** | `freetypewac` (FreeType fonts), `resources`, `wbm` |
| **Build / infra** | `installer`, `codesign`, `config`, `external_dependencies`, `filereader`, `timer`, `winampa`, `winampAll` |

---

## 2. The spine: the plugin ABI

**This is the single most important thing to learn.** Every capability is a
plugin with a name prefix implementing a known C ABI. The SDK examples make the
contract explicit:

| Example plugin | Prefix | Stage |
|---|---|---|
| `in_tone` | `in_` | **Input** — decode bytes → PCM |
| `out_null` | `out_` | **Output** — sink PCM → device / file / network |
| `dsp_test` | `dsp_` | **DSP** — transform PCM → PCM |
| `gen_classicart` | `gen_` | **General** — UI / feature |
| `ml_iso`, `ml_xmlex` | `ml_` | **Media Library** — catalogue source |
| (Milkdrop, AVS) | `vis_` | **Visualization** |
| (rip / transcode) | `enc_` | **Encoder** |

The signal flow is **`in_` → `dsp_` → `out_`**, with `ml_` / `enc_` / `gen_` /
`vis_` hanging off it, all behind a **frozen ABI**. The core orchestrates the
flow and implements none of the stages. *That* is why a single player handled
every format for 25 years: the core never changed when a codec did.

> **Take for AetherMedia:** the player core should be exactly this — an
> `in_ → dsp_ → out_` pipeline behind a stable contract, with library /
> encoder / visualization as plugin stages. It is the proven architecture for
> a media platform that has to absorb new formats and surfaces forever.

---

## 3. The engineering, layer by layer

### Decoder abstraction
One `in_` interface, ~20 implementations. A uniform decode contract is what
buys format breadth cheaply — add a codec, not a special case in the core.

### DSP chain + ReplayGain
`ReplayGainAnalysis/gain_analysis.c` is a perceptual loudness-analysis engine:
it measures how loud a track *sounds* (not its peak sample) and normalizes
playback so volume doesn't jump between tracks. Combined with an orderable DSP
plugin chain (EQ, effects) sitting between decode and output. Real
audio-quality engineering that users *feel* even if they can't name it.

### ns-eel2 — a JIT compiler hiding in a media player
The files (`nseel-compiler.c`, `asm-nseel-x86-msvc.c`, `asm-nseel-x64-macho.o`,
`glue_x86_64.h`, lex/yacc tables) are a genuine just-in-time compiler: it
parses math expressions and emits **native x86 / x64 / PPC machine code at
runtime**. It powered Milkdrop and the AVS visualizers — users wrote
per-frame / per-pixel math that ran at native speed. The scriptable creative
layer is what made Winamp a *culture*, not just a tool. (The same engine,
EEL2, lives on in Cockos REAPER's JSFX.)

### nde — embedded media database
A lightweight database engine built to index libraries of 100k+ tracks with
fast query — Winamp's local-library backend.

### Streaming heritage — NSV + Shoutcast
Winamp engineered internet radio before "streaming" was a word: a full stack
of codec + container (NSV) + protocol (Shoutcast) + client buffering. The
direct ancestor of what AetherMedia does over the mesh. Note: this was
**broadcast** (server → many listeners), not on-demand — which becomes central
to the post-mortem below.

### Performance — Intel IPP
Built against Intel Integrated Performance Primitives (SIMD-accelerated signal
processing). They engineered for speed on weak hardware — relevant for
low-power surfaces like a set-top / HomeCinema box.

---

## 4. Why it flopped anyway — the post-mortem

The tech was world-class. It died regardless. The reasons are the real
curriculum:

1. **It betrayed "fast and light" with framework bloat.** Winamp 3 (2002) was a
   ground-up rewrite on **Wasabi** — heavy, slow, and it broke plugin
   compatibility. Users revolted; many left for foobar2000 and iTunes. Winamp 5
   was literally "2 + 3, because we learned," reverting to 2's engine plus 3's
   good parts. The `Wasabi` / `Wasabi2` directories in this repo are the scar
   tissue. **Over-engineering the framework nearly killed the product once.**
2. **AOL acquired it (1999) and starved it** — no product vision, ad-driven
   bloat, talent attrition.
3. **It missed mobile entirely.** No credible iOS / Android client while the
   entire audience migrated to phones.
4. **It missed on-demand streaming.** Built for "your files" + broadcast radio;
   never pivoted to the cloud-library model Spotify won with (2008+).
5. **The 2024 "open source" release shipped a hostile license** (see below),
   strangling the community revival that might have saved it.

---

## 5. License reality — learn, do not copy

The repo is under the **Winamp Collaborative License (WCL) v1.0**, which despite
calling itself "copyleft" is **not open source**:

- **No distribution of modified versions. No forking. Only official maintainers
  may distribute.** (Section 5.)
- Contributions assign all IP to Winamp. Governed by Belgian law.

**Consequence:** we cannot copy, adapt, or reuse a single line in AetherMedia
(MIT). The WCL is incompatible and forbids exactly the redistribution we'd
need. Ideas and architecture are not copyrightable — those we learn freely.
Code is — that stays untouched. (The fork chain that put this on GitHub already
breaches the WCL's own "no forking" clause, which is itself a reason to keep it
at arm's length.)

---

## 6. What AetherMedia takes (concept only)

1. **Adopt the spine.** Player core = `in_ → dsp_ → out_` pipeline behind a
   frozen contract; library / encoder / visualization as plugin stages.
2. **Guard "fast and light" with your life.** This is AetherMedia's *live* risk:
   it already carries 14 projects + `UI.Shared` + Wasabi-shaped layering.
   Winamp 3's grave says don't let the framework outweigh the experience.
3. **Build the audio depth users feel** — loudness normalization (ReplayGain),
   EQ + DSP chain, gapless / crossfade, format breadth, scriptable visuals.
   AetherMedia currently has **none** of these.
4. **The era-jump Winamp missed is our entire thesis** — mesh, offline, mobile.
   That is our moat *only if we ship the clients*. Winamp died from not
   following users into the new era; we risk the same by claiming platforms we
   haven't built (Swift client today ≈ 365 lines).
5. **License is the one fatal thing Winamp got wrong at the end, and we got
   right at the start.** MIT. Protect it — it's what lets the community we need
   actually form.
