# AetherNet Media — Audio Pipeline

This document describes how AetherNet Media's audio path is structured
and what each layer is responsible for. It is the architectural
counterpart to [`docs/research/winamp-postmortem.md`](../research/winamp-postmortem.md),
which explained *why* this depth is essential.

## The pipeline

```
┌───────────────────────────────────────────────────────────────┐
│  Decoder  (LibVLC / AVFoundation / media3 — platform-specific) │
└──────────────────────────┬────────────────────────────────────┘
                           │  32-bit float PCM, interleaved
                           ▼
┌───────────────────────────────────────────────────────────────┐
│  IDspChain    DSP effects in order                            │
│   ├─ IEqualizer        (BiquadEqualizer, 10-band)             │
│   ├─ (future compressor, limiter, room correction, …)         │
│   └─ ReplayGain applier (LoudnessMeasurement.GainToTarget × X) │
└──────────────────────────┬────────────────────────────────────┘
                           │
                           ▼
┌───────────────────────────────────────────────────────────────┐
│  ICrossfade   per-sample gain ramp on track boundaries        │
└──────────────────────────┬────────────────────────────────────┘
                           │
                           ▼
┌───────────────────────────────────────────────────────────────┐
│  Output device  (the OS audio stack)                          │
└───────────────────────────────────────────────────────────────┘
```

The contract is small: **everything that touches audio works on
interleaved 32-bit float PCM in the range `[−1.0, 1.0]`**. No format
conversions inside the chain, no platform-specific buffer types.
A future SIMD-accelerated path can replace `Span<float>` with
`Vector<float>` without changing any interface.

## How a track plays

1. Decoder hands the player a PCM buffer.
2. The player consults `ILoudnessStore.GetAsync(contentHash)`. If a
   measurement is cached, multiply the buffer by
   `LoudnessMeasurement.GainToTarget(LoudnessTargets.Spotify)` so the
   track sits at the target loudness. If there is no measurement, kick
   off `Itu1770Analyzer.MeasureAsync(...)` in the background — it will
   apply on subsequent plays.
3. Pass the (possibly normalized) buffer through `IDspChain.Process` —
   the EQ runs here, plus any other registered effects.
4. On the last few hundred milliseconds before the next track is
   queued, blend with `ICrossfade.ComputeGainRamp` and the next track's
   buffer.
5. Hand off to the output device.

This is exactly the Winamp `in_ → dsp_ → out_` pipeline, modernised:
the `in_` plugins are the platform decoders; the `dsp_` plugins are
`IDspEffect` implementations; the `out_` plugin is the OS audio stack.

## Reference targets

`LoudnessTargets` carries the actual values the major platforms use.
Hard-coding a single −14 LUFS would be lazy — broadcast is at −23,
Apple Music at −16, and quiet-listening users prefer −19. The targets
are constants, not enums, so callers can use any custom value too.

## Why pure C#, not LibVLC's filters?

Three reasons:

1. **Portability.** The audio depth runs in any AetherNet client —
   Avalonia Desktop, MAUI Mobile, Blazor Web, a future Go daemon for
   CircleOS HomeCinema. Native LibVLC filters bind us to the desktop.
2. **Mesh metadata.** The point of measuring a track's loudness on one
   node is to gossip the measurement to peers so they don't have to
   redo the work. Our own `LoudnessMeasurement` is a record we can
   serialise, hash, and ship over `IContentService`. LibVLC's internal
   filter state isn't.
3. **Verification.** The Petri-net work on `aether-protocol` proved
   properties of the protocol. The same approach applies to the player:
   we can prove `ICrossfade` produces unit-energy ramps, that
   `BiquadEqualizer` is flat for the `Flat` preset, that
   `Itu1770Analyzer` matches reference values. The tests in
   `tests/AetherNet.Media.Audio.Tests/` are the first floor of that
   discipline.

## Roadmap

- **Replay-gain tag I/O** — read `REPLAYGAIN_TRACK_GAIN` / `R128_TRACK_GAIN`
  from ID3v2 and Vorbis comments so we honour creator-supplied values
  when present.
- **SIMD path** — `Vector<float>` overloads of `Process` for net9/net10.
- **Compressor / limiter** — first-party `IDspEffect` implementations.
- **Room correction** — convolution against a measured impulse response.
- **Mesh metadata cache** — `ILoudnessStore` implementation backed by
  `AetherNet.Content` so a track's measurement gossips across the network.
- **Visualization feed** — `IVisualizationFeed` exposing FFT buckets +
  waveform samples for player UI.
- **Proofs** — a `formal/` directory like the protocol has, modelling
  the chain's properties (no clipping under any gain, EQ flat at 0 dB,
  crossfade unit-energy).
